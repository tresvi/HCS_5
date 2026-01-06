using System;

namespace HCS.Connector.Abstractions.Models
{
    public class ConnectorParameters
    {
        TimeSpan ConnectionTimeout { get; set; }
        int ConnectionRetry { get; set; }
    }
}
