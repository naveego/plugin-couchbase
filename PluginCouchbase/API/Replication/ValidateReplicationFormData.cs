using System.Collections.Generic;
using PluginCouchbase.DataContracts;

namespace PluginCouchbase.API.Replication
{
    public static partial class Replication
    {
        public static List<string> ValidateReplicationFormData(ConfigureReplicationFormData data)
        {
            var errors = new List<string>();
            
            if (string.IsNullOrWhiteSpace(data.GoldenBucketName))
            {
                errors.Add("Golden Record bucket name is empty.");
            }
            else
            {
                if (data.GoldenBucketName.Length > 100)
                {
                    errors.Add("Golden Record bucket name is too long.");
                }
            }
            if (string.IsNullOrWhiteSpace(data.VersionBucketName))
            {
                errors.Add("Version Record bucket name is empty.");
            }
            else
            {
                if (data.VersionBucketName.Length > 100)
                {
                    errors.Add("Version Record bucket name is too long.");
                }
            }

            return errors;
        }
    }
}