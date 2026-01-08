using HCS.Connector.Abstractions.Interfaces;
using HCS.Connector.Abstractions.Models;
using System;
using System.Threading.Tasks;

namespace HCS.Connector.IBMMQ
{
    public class ConnectorMQ : IConnector
    {
        private const string NAME = "MQCore";

        public string Name { get { return NAME; } }
        public int MessagesSentCount { get; private set; }
        public int ErrorCount { get; private set; }
        public DateTime? ConnectedFrom { get; private set; }
        public ConnectionStateEnum State { get; private set; }


        public bool CheckHealth()
        {
            throw new NotImplementedException();
        }

        public void Close()
        {
            throw new NotImplementedException();
        }

        public void Open(IConnectorParameters parameters)
        {
            throw new NotImplementedException();
        }

        public TimeSpan Ping(string param = "")
        {
            throw new NotImplementedException();
        }

        public void Recycle()
        {
            throw new NotImplementedException();
        }

        public void Send(byte[] message, TimeSpan timeout, IMessageMetadata metadata)
        {
            if (!(metadata is MessageMetadataMQ mqMetadata))
                throw new ArgumentException("Solo se acepta MessageMetadataMQ en ConnectorMQ.", nameof(metadata));

            throw new NotImplementedException();
        }

        public byte[] SendAndReceive(byte[] message, TimeSpan timeout, IMessageMetadata metadata)
        {
            if (!(metadata is MessageMetadataMQ mqMetadata))
                throw new ArgumentException("Solo se acepta MessageMetadataMQ en ConnectorMQ.", nameof(metadata));

            throw new NotImplementedException();
        }

        public Task<bool> CheckHealthAsync() => throw new NotImplementedException();
        public Task CloseAsync() => throw new NotImplementedException();
        public Task OpenAsync(IConnectorParameters parameters) => throw new NotImplementedException();
        public Task<TimeSpan> PingAsync(string param = "") => throw new NotImplementedException();
        public Task RecycleAsync() => throw new NotImplementedException();
        public Task SendAsync(byte[] message, TimeSpan timeout, IMessageMetadata metadata) => throw new NotImplementedException();
        public Task<byte[]> SendAndReceiveAsync(byte[] message, TimeSpan timeout, IMessageMetadata metadata) => throw new NotImplementedException();
    }
}
