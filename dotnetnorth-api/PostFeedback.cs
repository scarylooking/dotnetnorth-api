using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using DotNetNorthAPI.Models;
using System.Linq;
using System.Text;
using DotNetNorthAPI.Responses;
using Newtonsoft.Json;

namespace DotNetNorthAPI
{
    public static class PostFeedback
    {
        [FunctionName("feedback")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestMessage req,
            [Table("feedback", Connection = "dotnetnorth_storage")] ICollector<EventFeedbackModel> feedbackTable,
            [Table("tickets", Connection = "dotnetnorth_storage")] ICollector<TicketModel> ticketTable,
            TraceWriter log)
        {
            var eventId = req.GetQueryNameValuePairs().FirstOrDefault(q => q.Key.Equals("event")).Value;

            if (!IsEventIdValid(eventId, log))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var feedbackModel = await req.Content.ReadAsAsync<EventFeedbackRequestModel>();

            if (!IsRequestValid(feedbackModel, log))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var feedback = new EventFeedbackModel
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

            var ticket = string.Empty;

            if (!string.IsNullOrWhiteSpace(feedbackModel.PhoneNumber))
            {
                ticket = GenerateTicket(eventId, 6);

                var ticketModel = new TicketModel
                {
                    PartitionKey = eventId,
                    RowKey = ticket
                };
                SendText(feedbackModel.PhoneNumber, ticket);
                ticketTable.Add(ticketModel);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new TicketResponse { TicketId = ticket }), Encoding.UTF8, "application/json")
            };
        }



        private static async void SendText(string mobile, string ticket)
        {
            var client = new HttpClient();

            var apiKey = Environment.GetEnvironmentVariable("ClockworkSMSApiKey");
            var encodedMobile = WebUtility.UrlEncode(mobile);
            var encodedMessage = WebUtility.UrlEncode($"Thanks! Your raffle ticket number is {ticket}.");

            var x = await client.GetStringAsync($"https://api.clockworksms.com/http/send.aspx?key={apiKey}&to={encodedMobile}&content={encodedMessage}&from=DotNetNorth");
        }

        private static bool IsRequestValid(EventFeedbackRequestModel feedbackRequest, TraceWriter log)
        {
            if (feedbackRequest.SpeakerPresentationSkillScore < 1 || feedbackRequest.SpeakerPresentationSkillScore > 5) return false;
            if (feedbackRequest.SpeakerKnowledgeScore < 1 || feedbackRequest.SpeakerKnowledgeScore > 5) return false;
            if (feedbackRequest.VenueRefreshmentsScore < 1 || feedbackRequest.VenueRefreshmentsScore > 5) return false;
            if (feedbackRequest.VenueLocationScore < 1 || feedbackRequest.VenueLocationScore > 5) return false;
            if (feedbackRequest.VenueFacilitiesScore < 1 || feedbackRequest.VenueFacilitiesScore > 5) return false;

            return true;
        }

        private static bool IsEventIdValid(string eventId, TraceWriter log)
        {
            if (string.IsNullOrEmpty(eventId)) return false;
            if (eventId.Length < 5) return false;

            return true;
        }

        private static string GenerateTicket(string eventId, int length)
        {
            var random = new Random();
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var ticketNumber = new string(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());

            return ticketNumber;
        }
    }
}
