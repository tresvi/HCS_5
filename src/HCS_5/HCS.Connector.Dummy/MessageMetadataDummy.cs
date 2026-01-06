using HCS.Connector.Abstractions.Models;
using System;

namespace HCS.Connector.Dummy
{
    internal class MessageMetadataDummy: MessageMetadata
    {
        public int EchoPrefix { get; set; }
        public int NumberOfEchoes { get; set; }
        public TimeSpan DelayBetweenMessages { get; set; }
        public TimeSpan ResponseDelay { get; set; }
    }
}
