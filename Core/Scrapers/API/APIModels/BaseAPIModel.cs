using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.Scrapers.API.APIModels
{
    public class BaseApiModel<T>
    {
        [JsonProperty("items")]
        public IList<T> Items { get; set; }
        [JsonProperty("quota_remaining")]
        public int QuotaRemaining { get; set; }
    }
}
