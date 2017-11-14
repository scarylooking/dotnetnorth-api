using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using DotNetNorthAPI.Models;
using Newtonsoft.Json;

namespace DotNetNorthAPI
{
    public static class GetTickets
    {
        [FunctionName("ticket")]
        public static HttpResponseMessage Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")]HttpRequestMessage req, 
            [Table("tickets", Connection = "dotnetnorth_storage")]IQueryable<TicketModel> ticketTable, 
            TraceWriter log)
        {
            var eventId = req.GetQueryNameValuePairs().FirstOrDefault(q => q.Key.Equals("event")).Value;

            if (!IsEventIdValid(eventId, log))
            {
                return new HttpResponseMessage(HttpStatusCode.BadRequest);
            }

            var tickets = ticketTable
                .Where(t => t.PartitionKey.Equals(eventId, System.StringComparison.InvariantCultureIgnoreCase))
                .Select(t => t.RowKey);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(tickets.ToArray()), Encoding.UTF8, "application/json")
            };
        }

        private static bool IsEventIdValid(string eventId, TraceWriter log)
        {
            if (string.IsNullOrEmpty(eventId)) return false;
            if (eventId.Length < 5) return false;

            return true;
        }
    }
}
