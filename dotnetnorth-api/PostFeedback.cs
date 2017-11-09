using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage.Table;
using DotNetNorthAPI.Models;
using System.Linq;
using DotNetNorthAPI.Responses;

namespace DotNetNorthAPI
{
    public static class PostFeedback
    {
        [FunctionName("feedback")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequestMessage req,
            [Table("feedback", Connection = "dotnetnorth_storage")]ICollector<EventFeedbackModel> feedbackTable,
            [Table("tickets", Connection = "dotnetnorth_storage")]ICollector<TicketModel> ticketTable,
            TraceWriter log)
        {
            string eventId = req.GetQueryNameValuePairs().FirstOrDefault(q => q.Key.Equals("event")).Value;
            
            if (!IsEventIdValid(eventId, log))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Event ID is invalid");
            }

            var feedbackModel = await req.Content.ReadAsAsync<EventFeedbackModel>();

            if (!IsRequestValid(feedbackModel, log))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Feedback model is invalid");
            }

            var ticket = GenerateTicket(eventId, 6);

            var feedback = new EventFeedbackModel()
            {
                PartitionKey = eventId.ToLower(),
                RowKey = Guid.NewGuid().ToString(),
                SpeakerPresentationSkillScore = feedbackModel.SpeakerPresentationSkillScore,
                SpeakerKnowledgeScore = feedbackModel.SpeakerKnowledgeScore,
                VenueRefreshmentsScore = feedbackModel.VenueRefreshmentsScore,
                VenueLocationScore = feedbackModel.VenueLocationScore,
                VenueFacilitiesScore = feedbackModel.VenueFacilitiesScore,
                VenueComments = feedbackModel.VenueComments,
                SpeakerComments = feedbackModel.SpeakerComments
            };

            feedbackTable.Add(feedback);
            ticketTable.Add(ticket);

            return req.CreateResponse(HttpStatusCode.OK, new TicketResponse
            {
                TicketId = ticket.RowKey
            });
        }

        private static bool IsRequestValid(EventFeedbackModel feedback, TraceWriter log)
        {
            if (feedback.SpeakerPresentationSkillScore < 1 || feedback.SpeakerPresentationSkillScore > 5) return false;
            if (feedback.SpeakerKnowledgeScore < 1 || feedback.SpeakerKnowledgeScore > 5) return false;
            if (feedback.VenueRefreshmentsScore < 1 || feedback.VenueRefreshmentsScore > 5) return false;
            if (feedback.VenueLocationScore < 1 || feedback.VenueLocationScore > 5) return false;
            if (feedback.VenueFacilitiesScore < 1 || feedback.VenueFacilitiesScore > 5) return false;

            return true;
        }

        private static bool IsEventIdValid(string eventId, TraceWriter log)
        {
            if (string.IsNullOrEmpty(eventId)) return false;
            if (eventId.Length < 5) return false;

            return true;
        }

        private static TicketModel GenerateTicket(string eventId, int length)
        {
            var random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var ticketNumber = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());

            return new TicketModel
            {
                PartitionKey = eventId,
                RowKey = ticketNumber
            };
        }
    }
}
