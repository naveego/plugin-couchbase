using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PluginCouchbase.API.Factory;
using PluginCouchbase.DataContracts;
using Pub;

namespace PluginCouchbase.API.Replication
{
    public static partial class Replication
    {
        private const string GoldenNameChange = "Golden record name changed";
        private const string VersionNameChange = "Version name changed";
        private const string JobDataVersionChange = "Job data version changed";
        private const string ShapeDataVersionChange = "Shape data version changed";

        public static async Task ReconcileReplicationJob(IClusterFactory clusterFactory, PrepareWriteRequest request)
        {
            // get request settings 
            var replicationSettings =
                JsonConvert.DeserializeObject<ConfigureReplicationFormData>(request.Replication.SettingsJson);
            var safeGoldenBucketName =
                string.Concat(replicationSettings.GoldenBucketName.Where(c => !char.IsWhiteSpace(c)));
            var safeVersionBucketName =
                string.Concat(replicationSettings.VersionBucketName.Where(c => !char.IsWhiteSpace(c)));

            // get previous metadata
            var previousMetadata = await GetPreviousReplicationMetadata(clusterFactory, request.DataVersions.JobId);

            // create current metadata
            var metadata = new ReplicationMetadata
            {
                ReplicatedShapeId = request.Schema.Id,
                ReplicatedShapeName = request.Schema.Name,
                Timestamp = DateTime.Now,
                Request = request
            };

            // setup for reconciliation
            var cluster = clusterFactory.GetCluster();

            // check if changes are needed
            if (previousMetadata == null)
            {
                var goldenBucket = await cluster.OpenBucketAsync(safeGoldenBucketName);
                var versionBucket = await cluster.OpenBucketAsync(safeVersionBucketName);
            }
            else
            {
                var dropGoldenReason = "";
                var dropVersionReason = "";
                var previousReplicationSettings =
                    JsonConvert.DeserializeObject<ConfigureReplicationFormData>(previousMetadata.Request.Replication
                        .SettingsJson);

                // check if golden bucket name changed
                if (previousReplicationSettings.GoldenBucketName != replicationSettings.GoldenBucketName)
                {
                    dropGoldenReason = GoldenNameChange;
                }

                // check if version bucket name changed
                if (previousReplicationSettings.VersionBucketName != replicationSettings.VersionBucketName)
                {
                    dropVersionReason = VersionNameChange;
                }

                // check if job data version changed
                if (metadata.Request.DataVersions.JobDataVersion > previousMetadata.Request.DataVersions.JobDataVersion)
                {
                    dropGoldenReason = JobDataVersionChange;
                    dropVersionReason = JobDataVersionChange;
                }

                // check if shape data version changed
                if (metadata.Request.DataVersions.ShapeDataVersion >
                    previousMetadata.Request.DataVersions.ShapeDataVersion)
                {
                    dropGoldenReason = ShapeDataVersionChange;
                    dropVersionReason = ShapeDataVersionChange;
                }

                if (dropGoldenReason != "")
                {
                    var safePreviousGoldenBucketName =
                        string.Concat(previousReplicationSettings.GoldenBucketName.Where(c => !char.IsWhiteSpace(c)));
                    var previousGoldenBucket =
                        await cluster.OpenBucketAsync(safePreviousGoldenBucketName);
                    cluster.CloseBucket(previousGoldenBucket);

                    var goldenBucket = await cluster.OpenBucketAsync(safeGoldenBucketName);
                }

                if (dropVersionReason != "")
                {
                    var safePreviousVersionBucketName =
                        string.Concat(previousReplicationSettings.VersionBucketName.Where(c => !char.IsWhiteSpace(c)));
                    var previousVersionBucket =
                        await cluster.OpenBucketAsync(safePreviousVersionBucketName);
                    cluster.CloseBucket(previousVersionBucket);

                    var versionBucket = await cluster.OpenBucketAsync(safeVersionBucketName);
                }
            }

            // save new metadata
            metadata = await UpsertReplicationMetadata(clusterFactory, request.DataVersions.JobId, metadata);
        }
    }
}