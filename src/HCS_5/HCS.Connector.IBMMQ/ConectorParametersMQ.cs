using HCS.Connector.Abstractions.Interfaces;
using System;

namespace HCS.Connector.IBMMQ
{
    public class ConectorParametersMQ : IConnectorParameters
    {
        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan CloseTimeout { get; set; }
    }
}
