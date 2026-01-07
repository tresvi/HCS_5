using System;

namespace HCS.Connector.Dummy
{
    internal class ConnectorDummyConfiguration
    {
        public int EchoPrefix { get; set; }
        public int NumberOfEchoes { get; set; }
        public TimeSpan DelayBetweenMessages { get; set; }
        public TimeSpan ResponseDelay { get; set; }
    }
}
