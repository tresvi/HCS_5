using HCS.Connector.Abstractions.Models;
using System;
using System.Threading.Tasks;

namespace HCS.Connector.Abstractions.Interfaces
{
    public interface IConnector
    {
        string Name { get; }
        int MessagesSentCount { get; }
        int ErrorCount { get; }
        DateTime? ConnectedFrom { get; }
        ConnectionStateEnum State { get; }

        void Open(IConnectorParameters parameters);
        void Close();
        void Recycle();
        bool CheckHealth();
        TimeSpan Ping(string param = "");
        void Send(byte[] message, TimeSpan timeout, IMessageMetadata metadata);
        byte[] SendAndReceive(byte[] message, TimeSpan timeout, IMessageMetadata metadata);

        Task OpenAsync(IConnectorParameters parameters);
        Task CloseAsync();
        Task RecycleAsync();
        Task<bool> CheckHealthAsync();
        Task<TimeSpan> PingAsync(string param = "");
        Task SendAsync(byte[] message, TimeSpan timeout, IMessageMetadata metadata);
        Task<byte[]> SendAndReceiveAsync(byte[] message, TimeSpan timeout, IMessageMetadata metadata);
    }
}
