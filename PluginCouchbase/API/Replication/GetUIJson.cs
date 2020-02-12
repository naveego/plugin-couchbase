using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginCouchbase.API.Replication
{
    public static partial class Replication
    {
        public static string GetUIJson()
        {
            var uiJsonObj = new Dictionary<string, object>
            {
                {"ui:order", new []
                {
                    "GoldenBucketName",
                    "VersionBucketName"
                }}
            };

//            var uiJsonObj = new Dictionary<string, object>();

            return JsonConvert.SerializeObject(uiJsonObj);
        }
    }
}