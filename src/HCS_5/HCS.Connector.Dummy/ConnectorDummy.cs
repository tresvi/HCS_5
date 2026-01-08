using HCS.Connector.Abstractions.Interfaces;
using HCS.Connector.Abstractions.Models;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace HCS.Connector.Dummy
{
    public class ConnectorDummy : IConnector, IDisposable
    {
        private const string NAME = "Dummy";

        public string Name { get { return NAME; } }
        public int MessagesSentCount { get; private set; }
        public int ErrorCount { get; private set; }
        public DateTime? ConnectedFrom { get; private set; }
        public ConnectionStateEnum State { get; private set; } = ConnectionStateEnum.Created;


        public bool CheckHealth()
        {
            return true;
        }

        public void Close()
        {
        }

        public void Open(IConnectorParameters parameters)
        {
            State = ConnectionStateEnum.Opened;
        }

        public TimeSpan Ping(string param = "")
        {
            return TimeSpan.Zero;
        }

        public void Recycle()
        {
        }

        public void Send(RequestMessage request, TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public ResponseMessage SendAndReceive(RequestMessage request, TimeSpan timeout)
        {
            return new ResponseMessage();
        }

        public Task<bool> CheckHealthAsync() => throw new NotImplementedException();
        public Task CloseAsync() => throw new NotImplementedException();
        public Task OpenAsync(IConnectorParameters parameters) => throw new NotImplementedException();
        public Task<TimeSpan> PingAsync(string param = "") => throw new NotImplementedException();
        public Task RecycleAsync() => throw new NotImplementedException();
        public Task SendAsync(RequestMessage request, TimeSpan timeout) => throw new NotImplementedException();
        public Task<ResponseMessage> SendAndReceiveAsync(RequestMessage request, TimeSpan timeout) => throw new NotImplementedException();

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
