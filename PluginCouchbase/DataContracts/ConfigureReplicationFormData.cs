namespace PluginCouchbase.DataContracts
{
    public class ConfigureReplicationFormData
    {
        public string GoldenBucketName { get; set; }
        public string VersionBucketName { get; set; }
    }
}