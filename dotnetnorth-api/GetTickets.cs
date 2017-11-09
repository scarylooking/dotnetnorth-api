using System.Linq;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using DotNetNorthAPI.Models;

namespace DotNetNorthAPI
{
    public static class GetTickets
    {
        [FunctionName("Ticket")]
        public static HttpResponseMessage Run(
            [HttpTrigger(AuthorizationLevel.Function, "get")]HttpRequestMessage req, 
            [Table("tickets", Connection = "dotnetnorth_storage")]IQueryable<TicketModel> ticketTable, 
            TraceWriter log)
        {
            string eventId = req.GetQueryNameValuePairs().FirstOrDefault(q => q.Key.Equals("event")).Value;

            if (!IsEventIdValid(eventId, log))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, "Event ID is invalid");
            }

            var tickets = ticketTable
                .Where(t => t.PartitionKey.Equals(eventId, System.StringComparison.InvariantCultureIgnoreCase))
                .Select(t => t.RowKey);

            return req.CreateResponse(HttpStatusCode.OK, tickets.ToArray());
        }

        private static bool IsEventIdValid(string eventId, TraceWriter log)
        {
            if (string.IsNullOrEmpty(eventId)) return false;
            if (eventId.Length < 5) return false;

            return true;
        }
    }
}
