using HCS.Connector.Abstractions.Interfaces;
using System;

namespace HCS.Connector.IBMMQ
{
    public class ConnectorParametersMQ : IConnectorParameters
    {
        public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(10);
        
        public string ServerIp { get; set; } = "";
        public int ServerPort { get; set; }
        public string Channel { get; set; } = "";
        public string ManagerName { get; set; } = "";
    }
}
