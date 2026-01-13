using HCS.Connector.Abstractions.Interfaces;
using HCS.Connector.Abstractions.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HCS.Connector.Dummy
{
    public class ConnectorDummy : IConnector, IDisposable
    {
        private const string NAME = "Dummy";

        private long _totalSentMessages;
        private long _totalReceivedMessages;
        private long _totalSentMessageErrors;
        private long _totalReceivedMessageErrors;
        private ConnectorParametersDummy _parameters;
        private readonly object _lock = new object();
        private bool _disposed = false;

        public string Name { get { return NAME; } }
        public long TotalSentMessages => _totalSentMessages;
        public long TotalReceivedMessages => _totalReceivedMessages;
        public long TotalSentMessageErrors => _totalSentMessageErrors;
        public long TotalReceivedMessageErrors => _totalReceivedMessageErrors;
        public DateTime? ConnectedFrom { get; private set; }
        public ConnectionStateEnum State { get; private set; } = ConnectionStateEnum.Created;

        public bool CheckHealth()
        {
            return true; // Siempre OK
        }

        public void Close()
        {
            lock (_lock)
            {
                if (State == ConnectionStateEnum.Closed || State == ConnectionStateEnum.Created)
                    return;

                State = ConnectionStateEnum.Closing;
                // Simular cierre
                State = ConnectionStateEnum.Closed;
                ConnectedFrom = null;
            }
        }

        public void Open(IConnectorParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (!(parameters is ConnectorParametersDummy dummyParams))
                throw new ArgumentException("Parameters must be of type ConnectorParametersDummy", nameof(parameters));

            lock (_lock)
            {
                if (State == ConnectionStateEnum.Opened)
                    return;

                State = ConnectionStateEnum.Opening;
                _parameters = dummyParams;
                // Simular apertura
                State = ConnectionStateEnum.Opened;
                ConnectedFrom = DateTime.UtcNow;
            }
        }

        public TimeSpan Ping(string param = "")
        {
            // Siempre OK, retorna tiempo cero
            return TimeSpan.Zero;
        }

        public void Recycle()
        {
            lock (_lock)
            {
                if (State == ConnectionStateEnum.Opened)
                {
                    Close();
                    Open(_parameters);
                }
            }
        }

        public void Send(RequestMessage request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (State != ConnectionStateEnum.Opened)
                throw new InvalidOperationException("Connector is not open");

            try
            {
                Interlocked.Increment(ref _totalSentMessages);
                // En modo Send, no hacemos nada, solo contamos
            }
            catch (Exception)
            {
                Interlocked.Increment(ref _totalSentMessageErrors);
                throw;
            }
        }

        public ResponseMessage SendAndReceive(RequestMessage request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (State != ConnectionStateEnum.Opened)
                throw new InvalidOperationException("Connector is not open");

            try
            {
                Interlocked.Increment(ref _totalSentMessages);

                // Obtener parámetros con valores por defecto
                int numberOfEchoes = _parameters?.NumberOfEchoes ?? 1;
                TimeSpan delayBetweenMessages = _parameters?.DelayBetweenMessages ?? TimeSpan.Zero;
                TimeSpan responseDelay = _parameters?.ResponseDelay ?? TimeSpan.Zero;

                // Aplicar ResponseDelay (simula tiempo de encolado/procesamiento inicial)
                if (responseDelay > TimeSpan.Zero)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Thread.Sleep(responseDelay);
                }

                // Convertir el contenido del mensaje a string
                string originalMessage = Encoding.UTF8.GetString(request.Content ?? new byte[0]);

                // Generar los ecos
                List<byte[]> echoMessages = new List<byte[]>();
                for (int i = 1; i <= numberOfEchoes; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // Aplicar DelayBetweenMessages (excepto antes del primer mensaje)
                    if (i > 1 && delayBetweenMessages > TimeSpan.Zero)
                    {
                        Thread.Sleep(delayBetweenMessages);
                    }

                    // Generar el mensaje de eco
                    string echoMessage = $"Eco Nro{i} de {originalMessage}";
                    byte[] echoBytes = Encoding.UTF8.GetBytes(echoMessage);
                    echoMessages.Add(echoBytes);

                    Interlocked.Increment(ref _totalReceivedMessages);
                }

                var response = new ResponseMessage(echoMessages, request.CorrelationID);
                response.ReceivedAt = DateTime.UtcNow;

                return response;
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref _totalSentMessageErrors);
                throw;
            }
            catch (Exception)
            {
                Interlocked.Increment(ref _totalSentMessageErrors);
                Interlocked.Increment(ref _totalReceivedMessageErrors);
                throw;
            }
        }

        public async Task<bool> CheckHealthAsync()
        {
            await Task.CompletedTask;
            return true; // Siempre OK
        }

        public async Task CloseAsync()
        {
            await Task.Run(() => Close());
        }

        public async Task OpenAsync(IConnectorParameters parameters)
        {
            await Task.Run(() => Open(parameters));
        }

        public async Task<TimeSpan> PingAsync(string param = "")
        {
            await Task.CompletedTask;
            return TimeSpan.Zero; // Siempre OK
        }

        public async Task RecycleAsync()
        {
            await Task.Run(() => Recycle());
        }

        public async Task SendAsync(RequestMessage request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            await Task.Run(() => Send(request, timeout, cancellationToken));
        }

        public async Task<ResponseMessage> SendAndReceiveAsync(RequestMessage request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return await Task.Run(() => SendAndReceive(request, timeout, cancellationToken));
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Close();
                _disposed = true;
            }
        }
    }
}
