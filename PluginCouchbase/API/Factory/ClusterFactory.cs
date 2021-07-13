using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Couchbase;
using Couchbase.Authentication;
using Couchbase.Configuration.Client;
using Couchbase.Core;
using Naveego.Sdk.Logging;
using Newtonsoft.Json;
using PluginCouchbase.DataContracts;
using PluginCouchbase.Helper;

namespace PluginCouchbase.API.Factory
{
    public class ClusterFactory : IClusterFactory
    {
        private PasswordAuthenticator _credentials;
        private ClientConfiguration _clientConfiguration;

        public void Initialize(ClientConfiguration config, PasswordAuthenticator credentials)
        {
            _credentials = credentials;
            _clientConfiguration = config;
            ClusterHelper.Initialize(config, credentials);
        }

        public ICluster GetCluster()
        {
            return ClusterHelper.Get();
        }

        public async Task<IBucket> GetBucketAsync(string bucketName)
        {
            await EnsureBucketAsync(bucketName);
            await CreateIndex(bucketName);
            return await ClusterHelper.GetBucketAsync(bucketName);
        }

        public async Task EnsureBucketAsync(string bucketName)
        {
            // prepare to create bucket
            var requestUrl = $"{_clientConfiguration.Servers.First().ToString().TrimEnd('/')}/default/buckets";

            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("name", bucketName),
                new KeyValuePair<string, string>("ramQuotaMB", "100"),
            };
            var body = new FormUrlEncodedContent(formData);
            
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_credentials.Username}:{_credentials.Password}")));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // create bucket
            Logger.Info($"Attempting to create bucket {bucketName}");
            var response = await client.PostAsync(requestUrl, body);
            if (response.IsSuccessStatusCode)
            {
                Logger.Info($"Bucket {bucketName} created successfully");
                Logger.Info($"Waiting for {bucketName} to be ready");
                // wait until bucket is ready
                var retries = 5;
                while (true)
                {
                    try
                    {
                        if (retries == 0)
                        {
                            // error making bucket
                            throw new Exception($"Error ensuring bucket {bucketName}");
                        }
                        
                        // try to get bucket
                        await GetBucketAsync(bucketName);
                        break;
                    }
                    catch (Exception e)
                    {
                        // wait for a moment then retry
                        Logger.Info($"Bucket {bucketName} not ready yet. Retries remaining {retries}");
                        retries -= 1;
                        Thread.Sleep(1000);
                    }
                }
                Logger.Info($"Bucket {bucketName} is ready");
                return;
            }

            var content = JsonConvert.DeserializeObject<EnsureBucketResponse>(await response.Content.ReadAsStringAsync());
            if (content.Errors.ContainsKey("name"))
            {
                // bucket already exists
                var nameError = content.Errors["name"];
                if (nameError.Equals("Bucket with given name already exists"))
                {
                    return;
                }
            }

            // error making bucket
            var ex = new Exception($"Error ensuring bucket {bucketName}");
            Logger.Error(ex, $"Error ensuring bucket: {JsonConvert.SerializeObject(content.Errors, Formatting.Indented)}");
            throw ex;
        }
        
        public async Task DeleteBucketAsync(string bucketName)
        {
            var requestUrl = $"{_clientConfiguration.Servers.First().ToString().TrimEnd('/')}/default/buckets/{bucketName}";
            
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_credentials.Username}:{_credentials.Password}")));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.DeleteAsync(requestUrl);
            response.EnsureSuccessStatusCode();
        }

        public async Task CreateIndex(string bucketName)
        {
            var bucket = await ClusterHelper.GetBucketAsync(bucketName);
            var result =
                await bucket.QueryAsync<dynamic>(
                    $"CREATE PRIMARY INDEX `{bucketName}-index` ON `{bucketName}` USING GSI;");

            if (!result.Success)
            {
                if (result.Errors.First().Message.Contains("already exists"))
                {
                    return;    
                }
                
                throw new Exception("Error creating index for bucket");
            }
        }
        
        public bool Initialized()
        {
            return ClusterHelper.Initialized;
        }
    }
}