using Microsoft.WindowsAzure.Storage.Table;

namespace DotNetNorthAPI.Models
{
    public class EventFeedbackModel : TableEntity
    {
        public int SpeakerPresentationSkillScore { get; set; }
        public int SpeakerKnowledgeScore { get; set; }
        public int VenueRefreshmentsScore { get; set; }
        public int VenueLocationScore { get; set; }
        public int VenueFacilitiesScore { get; set; }
        public string VenueComments { get; set; }
        public string SpeakerComments { get; set; }
    }
}
