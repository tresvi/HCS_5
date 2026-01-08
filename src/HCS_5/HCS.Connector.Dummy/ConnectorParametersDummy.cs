using HCS.Connector.Abstractions.Interfaces;
using System;

namespace HCS.Connector.Dummy
{
    internal class ConnectorParametersDummy: IConnectorParameters
    {
        public TimeSpan ConnectionTimeout { get; set; }
        public TimeSpan CloseTimeout { get; set; }

        public int EchoPrefix { get; set; }
        public int NumberOfEchoes { get; set; }
        public TimeSpan DelayBetweenMessages { get; set; }
        public TimeSpan ResponseDelay { get; set; }

    }
}
