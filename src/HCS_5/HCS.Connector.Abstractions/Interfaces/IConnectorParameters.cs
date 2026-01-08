using System;

namespace HCS.Connector.Abstractions.Interfaces
{
    public interface IConnectorParameters
    {
        TimeSpan ConnectionTimeout { get; set; }
        int ConnectionRetry { get; set; }
    }
}
