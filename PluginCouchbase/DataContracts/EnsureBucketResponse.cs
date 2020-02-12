using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginCouchbase.DataContracts
{
    public class EnsureBucketResponse
    {
        [JsonProperty("errors")]
        public Dictionary<string, string> Errors { get; set; }
    }
}