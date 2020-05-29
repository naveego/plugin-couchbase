using System;
using Naveego.Sdk.Plugins;


namespace PluginCouchbase.DataContracts
{
    public class ReplicationMetadata
    {
        public PrepareWriteRequest Request { get; set; }
        public DateTime Timestamp { get; set; }
        public string ReplicatedShapeId { get; set; }
        public string ReplicatedShapeName { get; set; }
    }
}