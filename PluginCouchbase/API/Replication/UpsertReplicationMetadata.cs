using System.Threading.Tasks;
using Couchbase;
using PluginCouchbase.API.Factory;
using PluginCouchbase.API.Utility;
using PluginCouchbase.DataContracts;

namespace PluginCouchbase.API.Replication
{
    public static partial class Replication
    {
        public static async Task<ReplicationMetadata> UpsertReplicationMetadata(IClusterFactory clusterFactory, string jobId, ReplicationMetadata metadata)
        {
            var bucket = await clusterFactory.GetBucketAsync(Constants.ReplicationMetadataBucket);

            var result = await bucket.UpsertAsync(jobId, metadata);
            result.EnsureSuccess();

            return result.Value;
        }
    }
}