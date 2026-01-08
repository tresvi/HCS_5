using HCS.Connector.Abstractions.Interfaces;
using System;
using System.Collections.Generic;

namespace HCS.Connector.Dummy
{
    internal class MessageMetadataDummy: IMessageMetadata
    {
        public string CorrelationID { get; set; }
        public string AppName { get; set; }
        public Dictionary<string, string> ExtraData { get; set; }

        public int EchoPrefix { get; set; }
        public int NumberOfEchoes { get; set; }
        public TimeSpan DelayBetweenMessages { get; set; }
        public TimeSpan ResponseDelay { get; set; }
    }
}
