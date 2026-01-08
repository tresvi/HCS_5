using HCS.Connector.Abstractions.Models;

namespace HCS.Connector.IBMMQ
{
    internal class RequestMessageMQ: RequestMessage
    {
      /*  
        string CorrelationID { get; set; }
        string AppName { get; set; }
        Dictionary<string, string> Metadata { get; set; }
      */

        public string InputQueue { get; set; }
        public string OutputQueue { get; set; }

        public RequestMessageMQ() { }
        
    }
}
