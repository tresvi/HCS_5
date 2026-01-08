using HCS.Connector.Abstractions.Interfaces;
using HCS.Connector.Abstractions.Models;
using System;
using System.Collections.Generic;

namespace HCS.Connector.Dummy
{
    internal class RequestMessageDummy: RequestMessage
    {
        public int EchoPrefix { get; set; }
        public int NumberOfEchoes { get; set; }
        public TimeSpan DelayBetweenMessages { get; set; }
        public TimeSpan ResponseDelay { get; set; }
    }
}
