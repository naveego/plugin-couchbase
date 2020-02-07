using System.Threading.Tasks;
using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;
using Couchbase.Core;

namespace PluginCouchbase.API.Factory
{
    public class ClusterFactory : IClusterFactory
    {
        public void Initialize(ClientConfiguration config, PasswordAuthenticator credentials)
        {
            ClusterHelper.Initialize(config, credentials);
        }

        public ICluster GetCluster()
        {
            return ClusterHelper.Get();
        }

        public async Task<IBucket> GetBucketAsync(string bucketName)
        {
            return await ClusterHelper.GetBucketAsync(bucketName);
        }
    }
}