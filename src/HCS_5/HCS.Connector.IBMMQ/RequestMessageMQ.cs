using HCS.Connector.Abstractions.Models;
using System.Collections.Generic;

namespace HCS.Connector.IBMMQ
{
    internal class RequestMessageMQ: RequestMessage
    {
        string CorrelationID { get; set; }
        string AppName { get; set; }
        Dictionary<string, string> ExtraData { get; set; }

        public string InputQueue { get; set; }
        public string OutputQueue { get; set; }

        public RequestMessageMQ() { }
        
    }
}
