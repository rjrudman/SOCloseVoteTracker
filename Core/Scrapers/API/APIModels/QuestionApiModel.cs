using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.Scrapers.API.APIModels
{
    public class QuestionApiModel
    {
        [JsonProperty("question_id")]
        public int QuestionId { get; set; }

        [JsonProperty("creation_date")]
        public long CreateDateInt { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("closed_date")]
        public long? ClosedDateInt { get; set; }

        [JsonProperty("close_vote_count")]
        public int CloseVotes { get; set; }

        [JsonProperty("reopen_vote_count")]
        public int ReopenVotes { get; set; }

        [JsonProperty("delete_vote_count")]
        public int DeleteVotes { get; set; }
        
        [JsonProperty("tags")]
        public IList<string> Tags { get; set; }

        [JsonProperty("closed_details")]
        public ClosedDetails ClosedDetails { get; set; } 
    }

    public class ClosedDetails
    {
        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("original_questions")]
        public IList<OriginalQuestion> OriginalQuestions { get; set; } 
    }

    public class OriginalQuestion
    {
        [JsonProperty("question_id")]
        public int QuestionId { get; set; }
    }

}
