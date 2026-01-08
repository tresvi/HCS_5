using System.Collections.Generic;

namespace HCS.Connector.Abstractions.Interfaces
{
    public interface IMessageMetadata
    {
        string CorrelationID { get; set; }
        string AppName { get; set; }
        Dictionary<string, string> ExtraData { get; set; }
    }
}
