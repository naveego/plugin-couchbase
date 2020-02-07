using System.Threading.Tasks;
using Couchbase;
using PluginCouchbase.API.Factory;
using PluginCouchbase.API.Utility;
using PluginCouchbase.DataContracts;

namespace PluginCouchbase.API.Replication
{
    public static partial class Replication
    {
        public static async Task<ReplicationMetadata> GetPreviousReplicationMetadata(IClusterFactory clusterFactory,
            string jobId)
        {
            var cluster = clusterFactory.GetCluster();
            var bucket = await cluster.OpenBucketAsync(Constants.ReplicationMetadataBucket);

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
    }
}