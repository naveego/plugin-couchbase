using System;
using System.Threading.Tasks;
using Couchbase;
using Naveego.Sdk.Logging;
using PluginCouchbase.API.Factory;
using PluginCouchbase.API.Utility;
using PluginCouchbase.DataContracts;
using PluginCouchbase.Helper;

namespace PluginCouchbase.API.Replication
{
    public static partial class Replication
    {
        public static async Task<ReplicationMetadata> GetPreviousReplicationMetadata(IClusterFactory clusterFactory,
            string jobId)
        {
            try
            {
                var bucket = await clusterFactory.GetBucketAsync(Constants.ReplicationMetadataBucket);

                // check if metadata exists
                if (!await bucket.ExistsAsync(jobId))
                {
                    // no metadata
                    return null;
                }

                // metadata exists
                var result = await bucket.GetAsync<ReplicationMetadata>(jobId);
                result.EnsureSuccess();

                return result.Value;
            }
            catch (Exception e)
            {
                Logger.Error(e, e.Message);
                throw;
            }
        }
    }
}