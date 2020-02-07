using System.Threading.Tasks;
using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;
using Couchbase.Core;

namespace PluginCouchbase.API.Factory
{
    public interface IClusterFactory
    {
        void Initialize(ClientConfiguration config, PasswordAuthenticator credentials);
        ICluster GetCluster();
        Task<IBucket> GetBucketAsync(string bucketName);
    }
}