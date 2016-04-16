using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.Scrapers.API.APIModels
{
    public class CloseVoteApiModel
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("dialog_title")]
        public string Title { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("sub_options")]
        public List<CloseVoteApiModel> SubOptions { get; set; }
    }
}
