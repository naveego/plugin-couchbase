using System;
using System.Linq;
using System.Threading.Tasks;
using Naveego.Sdk.Plugins;
using Newtonsoft.Json;
using PluginCouchbase.API.Factory;
using PluginCouchbase.DataContracts;
using PluginCouchbase.Helper;


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

            Logger.Info(
                $"Golden Bucket: {safeGoldenBucketName} Version Bucket: {safeVersionBucketName} job: {request.DataVersions.JobId}");

            // get previous metadata
            Logger.Info($"Getting previous metadata job: {request.DataVersions.JobId}");
            var previousMetadata = await GetPreviousReplicationMetadata(clusterFactory, request.DataVersions.JobId);
            Logger.Info($"Got previous metadata job: {request.DataVersions.JobId}");

            // create current metadata
            Logger.Info($"Generating current metadata job: {request.DataVersions.JobId}");
            var metadata = new ReplicationMetadata
            {
                ReplicatedShapeId = request.Schema.Id,
                ReplicatedShapeName = request.Schema.Name,
                Timestamp = DateTime.Now,
                Request = request
            };
            Logger.Info($"Generated current metadata job: {request.DataVersions.JobId}");

            // check if changes are needed
            if (previousMetadata == null)
            {
                Logger.Info($"No Previous metadata creating buckets job: {request.DataVersions.JobId}");
                await clusterFactory.EnsureBucketAsync(safeGoldenBucketName);
                await clusterFactory.EnsureBucketAsync(safeVersionBucketName);
                Logger.Info($"Created buckets job: {request.DataVersions.JobId}");
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

                // drop previous golden bucket
                if (dropGoldenReason != "")
                {
                    var safePreviousGoldenBucketName =
                        string.Concat(previousReplicationSettings.GoldenBucketName.Where(c => !char.IsWhiteSpace(c)));

                    await clusterFactory.DeleteBucketAsync(safePreviousGoldenBucketName);

                    await clusterFactory.EnsureBucketAsync(safeGoldenBucketName);
                }

                // drop previous version bucket
                if (dropVersionReason != "")
                {
                    var safePreviousVersionBucketName =
                        string.Concat(previousReplicationSettings.VersionBucketName.Where(c => !char.IsWhiteSpace(c)));

                    await clusterFactory.DeleteBucketAsync(safePreviousVersionBucketName);

                    await clusterFactory.EnsureBucketAsync(safeVersionBucketName);
                }
            }

            // save new metadata
            Logger.Info($"Updating metadata job: {request.DataVersions.JobId}");
            await UpsertReplicationMetadata(clusterFactory, request.DataVersions.JobId, metadata);
            Logger.Info($"Updated metadata job: {request.DataVersions.JobId}");
        }
    }
}