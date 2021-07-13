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
        public static async Task<ReplicationMetadata> UpsertReplicationMetadata(IClusterFactory clusterFactory, string jobId, ReplicationMetadata metadata)
        {
            try
            {
                var bucket = await clusterFactory.GetBucketAsync(Constants.ReplicationMetadataBucket);

                var result = await bucket.UpsertAsync(jobId, metadata);
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