using HCS.Connector.Abstractions.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HCS.Connector.Abstractions.Interfaces
{
    public interface IConnector: IDisposable
    {
        string Name { get; }
        long TotalSentMessages { get; }
        long TotalReceivedMessages { get; }
        long TotalSentMessageErrors { get; }
        long TotalReceivedMessageErrors { get; }
        DateTime? ConnectedFrom { get; }
        ConnectionStateEnum State { get; }

        void Open(IConnectorParameters parameters);
        void Close();
        void Recycle();
        bool CheckHealth();
        TimeSpan Ping(string param = "");
        void Send(RequestMessage request, TimeSpan sendTimeout, CancellationToken cancellationToken);
        ResponseMessage SendAndReceive(RequestMessage request, TimeSpan timeout, CancellationToken cancellationToken);

        Task OpenAsync(IConnectorParameters parameters);
        Task CloseAsync();
        Task RecycleAsync();
        Task<bool> CheckHealthAsync();
        Task<TimeSpan> PingAsync(string param = "");
        Task SendAsync(RequestMessage request, TimeSpan sendTimeout, CancellationToken cancellationToken);
        Task<ResponseMessage> SendAndReceiveAsync(RequestMessage request, TimeSpan timeout, CancellationToken cancellationToken);
    }
}
