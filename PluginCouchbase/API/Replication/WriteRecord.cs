using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Couchbase;
using Newtonsoft.Json;
using PluginCouchbase.API.Factory;
using PluginCouchbase.API.Utility;
using PluginCouchbase.DataContracts;
using PluginCouchbase.Helper;
using Pub;

namespace PluginCouchbase.API.Replication
{
    public static partial class Replication
    {
        /// <summary>
        /// Adds and removes records to local replication db
        /// Adds and updates available shapes
        /// </summary>
        /// <param name="clusterFactory"></param>
        /// <param name="schema"></param>
        /// <param name="record"></param>
        /// <param name="config"></param>
        /// <returns>Error message string</returns>
        public static async Task<string> WriteRecord(IClusterFactory clusterFactory, Schema schema, Record record,
            ConfigureReplicationFormData config)
        {
            try
            {
                // setup
                var safeShapeName = schema.Name;
                var safeGoldenBucketName =
                    string.Concat(config.GoldenBucketName.Where(c => !char.IsWhiteSpace(c)));
                var safeVersionBucketName =
                    string.Concat(config.VersionBucketName.Where(c => !char.IsWhiteSpace(c)));

                var cluster = clusterFactory.GetCluster();
                var goldenBucket = await cluster.OpenBucketAsync(safeGoldenBucketName);
                var versionBucket = await cluster.OpenBucketAsync(safeVersionBucketName);

                // transform data
                var recordVersionIds = record.Versions.Select(v => v.RecordId).ToList();
                var recordData = GetNamedRecordData(schema, record);
                recordData[Constants.NaveegoVersionIds] = recordVersionIds;

                // get previous golden record
                List<string> previousRecordVersionIds;
                if (await goldenBucket.ExistsAsync(record.RecordId))
                {
                    var result = await goldenBucket.LookupIn<Dictionary<string, object>>(record.RecordId)
                        .Get(Constants.NaveegoVersionIds)
                        .ExecuteAsync();
                    previousRecordVersionIds = result.Content<List<string>>(0);
                }
                else
                {
                    previousRecordVersionIds = recordVersionIds;
                }

                // write data
                if (recordData.Count == 0)
                {
                    // delete everything for this record
                    Logger.Info($"shapeId: {safeShapeName} | recordId: {record.RecordId} - DELETE");
                    var result = await goldenBucket.RemoveAsync(record.RecordId);
                    result.EnsureSuccess();

                    foreach (var versionId in previousRecordVersionIds)
                    {
                        Logger.Info($"shapeId: {safeShapeName} | recordId: {record.RecordId} | versionId: {versionId} - DELETE");
                        result = await versionBucket.RemoveAsync(versionId);
                        result.EnsureSuccess();
                    }
                }
                else
                {
                    // update record and remove/add versions
                    Logger.Info($"shapeId: {safeShapeName} | recordId: {record.RecordId} - UPSERT");
                    var result = await goldenBucket.UpsertAsync(record.RecordId, recordData);
                    result.EnsureSuccess();
                        
                    // delete missing versions
                    var missingVersions = previousRecordVersionIds.Except(recordVersionIds);
                    foreach (var versionId in missingVersions)
                    {
                        Logger.Info($"shapeId: {safeShapeName} | recordId: {record.RecordId} | versionId: {versionId} - DELETE");
                        var versionDeleteResult = await versionBucket.RemoveAsync(versionId);
                        versionDeleteResult.EnsureSuccess();
                    }
                        
                    // upsert other versions
                    foreach (var version in record.Versions)
                    {
                        Logger.Info($"shapeId: {safeShapeName} | recordId: {record.RecordId} | versionId: {version.RecordId} - UPSERT");
                        var versionData = JsonConvert.DeserializeObject<Dictionary<string, object>>(version.DataJson);
                        var versionUpsertResult = await versionBucket.UpsertAsync(version.RecordId, versionData);
                        versionUpsertResult.EnsureSuccess();
                    }
                }

                return "";
            }
            catch (Exception e)
            {
                Logger.Error(e.Message);
                throw;
            }
        }

        /// <summary>
        /// Converts data object with ids to friendly names
        /// </summary>
        /// <param name="schema"></param>
        /// <param name="record"></param>
        /// <returns>Data object with friendly name keys</returns>
        private static Dictionary<string, object> GetNamedRecordData(Schema schema, Record record)
        {
            var namedData = new Dictionary<string, object>();
            var recordData = JsonConvert.DeserializeObject<Dictionary<string, object>>(record.DataJson);

            foreach (var property in schema.Properties)
            {
                var key = property.Id;
                if (recordData.ContainsKey(key))
                {
                    if (recordData[key] == null)
                    {
                        continue;
                    }

                    namedData.Add(property.Name, recordData[key]);
                }
            }

            return namedData;
        }
    }
}