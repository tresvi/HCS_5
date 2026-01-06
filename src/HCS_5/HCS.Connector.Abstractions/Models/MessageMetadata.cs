using System.Collections.Generic;

namespace HCS.Connector.Abstractions.Models
{
    public class MessageMetadata
    {
        public string CorrelationID { get; set; }
        public string AppName { get; set; }
        public Dictionary<string, string> ExtraData { get; set; }
    }
}
