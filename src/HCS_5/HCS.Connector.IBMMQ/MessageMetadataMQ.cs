using HCS.Connector.Abstractions.Interfaces;
using System.Collections.Generic;

namespace HCS.Connector.IBMMQ
{
    internal class MessageMetadataMQ: IMessageMetadata
    {
        string IMessageMetadata.CorrelationID { get; set; }
        string IMessageMetadata.AppName { get; set; }
        Dictionary<string, string> IMessageMetadata.ExtraData { get; set; }

        public string InputQueue { get; set; }
        public string OutputQueue { get; set; }

        public MessageMetadataMQ() { }
        
    }
}
