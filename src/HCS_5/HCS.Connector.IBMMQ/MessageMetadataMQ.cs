using HCS.Connector.Abstractions.Models;

namespace HCS.Connector.IBMMQ
{
    internal class MessageMetadataMQ: MessageMetadata
    {
        public string InputQueue { get; set; }
        public string OutputQueue { get; set; }

        public MessageMetadataMQ() { }
        
    }
}
