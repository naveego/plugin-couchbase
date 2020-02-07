using System.Collections.Generic;
using Newtonsoft.Json;

namespace PluginCouchbase.API.Replication
{
    public static partial class Replication
    {
        public static string GetSchemaJson()
        {
            var schemaJsonObj = new Dictionary<string, object>
            {
                {"type", "object"},
                {"properties", new Dictionary<string, object>
                {
                    {"GoldenBucketName", new Dictionary<string, string>
                    {
                        {"type", "string"},
                        {"title", "Golden Record Bucket Name"},
                        {"description", "Name for your golden record bucket in Couchbase"},
                    }},
                    {"VersionBucketName", new Dictionary<string, string>
                    {
                        {"type", "string"},
                        {"title", "Version Record Bucket Name"},
                        {"description", "Name for your version record bucket in Couchbase"},
                    }},
                }},
                {"required", new []
                {
                    "GoldenBucketName",
                    "VersionBucketName"
                }}
            };

//            var schemaJsonObj = new Dictionary<string, object>();

            return JsonConvert.SerializeObject(schemaJsonObj);
        }
    }
}