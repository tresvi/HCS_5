using HCS.Connector.Abstractions.Interfaces;
using HCS.Connector.Abstractions.Models;
using IBM.WMQ;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HCS.Connector.IBMMQ
{
    public class ConnectorMQ : IConnector, IDisposable
    {
        private const string NAME = "MQCore";
        private int _messagesSentCount;
        private int _errorCount;

        public string Name { get { return NAME; } }
        public int MessagesSentCount => _messagesSentCount;
        public int ErrorCount => _errorCount;
        public DateTime? ConnectedFrom { get; private set; }
        public ConnectionStateEnum State { get; private set; } = ConnectionStateEnum.Created;

        private readonly object _lock = new object();

        private MQQueueManager _queueManager;
        private ConnectorParametersMQ _parameters;
        private bool _disposed = false;

        public void Open(IConnectorParameters parameters)
        {
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            if (!(parameters is ConnectorParametersMQ mqParams))
                throw new ArgumentException("Parameters must be of type ConnectorParametersMQ", nameof(parameters));

            lock (_lock)
            {
                if (State == ConnectionStateEnum.Opened) return;

                State = ConnectionStateEnum.Opening;

                try
                {
                    _parameters = mqParams;
                    
                    // Crear propiedades de conexión
                    var connectionProperties = new Hashtable();
                    
                    connectionProperties.Add(MQC.HOST_NAME_PROPERTY, mqParams.ServerIp);
                    connectionProperties.Add(MQC.PORT_PROPERTY, mqParams.ServerPort);
                    connectionProperties.Add(MQC.CHANNEL_PROPERTY, mqParams.Channel);
                    //connectionProperties.Add(MQC.TRANSPORT_PROPERTY, MQC.TRANSPORT_MQSERIES_CLIENT);
                    
                    _queueManager = new MQQueueManager(mqParams.ManagerName, connectionProperties);
                    
                    ConnectedFrom = DateTime.UtcNow;
                    State = ConnectionStateEnum.Opened;
                }
                catch (MQException mqEx)
                {
                    Interlocked.Increment(ref _errorCount);
                    State = ConnectionStateEnum.Faulted;
                    throw new Exception($"Error al conectar con IBM MQ: Reason={mqEx.ReasonCode}, Message={mqEx.Message}", mqEx);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _errorCount);
                    State = ConnectionStateEnum.Faulted;
                    throw;
                }
            }
        }

        public void Close()
        {
            lock (_lock)
            {
                if (State == ConnectionStateEnum.Closed || State == ConnectionStateEnum.Created)
                    return;

                State = ConnectionStateEnum.Closing;

                try
                {
                    if (_queueManager != null && _queueManager.IsConnected)
                    {
                        _queueManager.Disconnect();
                        _queueManager = null;
                    }
                    State = ConnectionStateEnum.Closed;
                    ConnectedFrom = null;
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref _errorCount);
                    State = ConnectionStateEnum.Faulted;
                    throw;
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Close();
                _disposed = true;
            }
        }

        public bool CheckHealth()
        {
            try
            {
                if (_queueManager == null || !_queueManager.IsConnected) return false;

                // Intentar una operación simple para verificar conexión
                string qmName = _queueManager.Name;
                return !string.IsNullOrEmpty(qmName);
            }
            catch
            {
                return false;
            }
        }

        public TimeSpan Ping(string param = "")
        {
            if (State != ConnectionStateEnum.Opened)
                throw new InvalidOperationException("Connector is not open");

            var stopwatch = Stopwatch.StartNew();
            try
            {
                // Verificar conexión haciendo una consulta simple
                CheckHealth();
                stopwatch.Stop();
                return stopwatch.Elapsed;
            }
            catch
            {
                stopwatch.Stop();
                Interlocked.Increment(ref _errorCount);
                throw;
            }
        }

        public void Recycle()
        {
            lock (_lock)
            {
                Close();
                if (_parameters != null)
                {
                    Open(_parameters);
                }
            }
        }

        public void Send(RequestMessage request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!(request is RequestMessageMQ mqRequest))
                throw new ArgumentException("Request must be of type RequestMessageMQ", nameof(request));

            if (State != ConnectionStateEnum.Opened)
                throw new InvalidOperationException("Connector is not open");

            MQQueue outputQueue = null;
            try
            {
                if (string.IsNullOrEmpty(mqRequest.OutputQueue))
                    throw new ArgumentException("OutputQueue must be specified", nameof(request));

                // Abrir cola de salida
                int openOptions = MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING;
                outputQueue = _queueManager.AccessQueue(mqRequest.OutputQueue, openOptions);

                // Crear mensaje MQ
                var msg = new MQMessage
                {
                    Format = MQC.MQFMT_STRING,
                    CharacterSet = 1208,  // UTF-8
                    MessageId = MQC.MQMI_NONE,
                    CorrelationId = MQC.MQCI_NONE,
                    Persistence = MQC.MQPER_NOT_PERSISTENT
                };

                string content = Encoding.UTF8.GetString(request.Content);
                msg.WriteString(content);

                var pmo = new MQPutMessageOptions
                {
                    Options = MQC.MQPMO_NO_SYNCPOINT | MQC.MQPMO_NEW_MSG_ID | MQC.MQPMO_NO_CONTEXT
                };
                
                outputQueue.Put(msg, pmo);

                request.SentAt = DateTime.UtcNow;
                Interlocked.Increment(ref _messagesSentCount);
            }
            catch (MQException mqEx)
            {
                Interlocked.Increment(ref _errorCount);
                throw new Exception($"Error MQ al enviar mensaje: Reason={mqEx.ReasonCode}, Message={mqEx.Message}", mqEx);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _errorCount);
                throw;
            }
            finally
            {
                outputQueue?.Close();
            }
        }

        public ResponseMessage SendAndReceive(RequestMessage request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // Inicializar mediciones de tiempo
            var totalStopwatch = Stopwatch.StartNew();
            var openQueuesStopwatch = new Stopwatch();
            var preparePutStopwatch = new Stopwatch();
            var putStopwatch = new Stopwatch();
            var prepareGetStopwatch = new Stopwatch();
            var getStopwatch = new Stopwatch();
            var processResponseStopwatch = new Stopwatch();
            var closeQueuesStopwatch = new Stopwatch();

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!(request is RequestMessageMQ mqRequest))
                throw new ArgumentException("Request must be of type RequestMessageMQ", nameof(request));

            if (State != ConnectionStateEnum.Opened)
                throw new InvalidOperationException("Connector is not open");

            MQQueue queueOut = null;
            MQQueue queueIn = null;
            ResponseMessage response = null;
            
            try
            {
                if (string.IsNullOrEmpty(mqRequest.OutputQueue))
                    throw new ArgumentException("OutputQueue must be specified", nameof(request));
                if (string.IsNullOrEmpty(mqRequest.InputQueue))
                    throw new ArgumentException("InputQueue must be specified", nameof(request));

                // Medir tiempo de apertura de colas
                openQueuesStopwatch.Start();
                int openOutOptions = MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING;
                int openInOptions = MQC.MQOO_INPUT_AS_Q_DEF | MQC.MQOO_FAIL_IF_QUIESCING;

                queueOut = _queueManager.AccessQueue(mqRequest.OutputQueue, openOutOptions);
                queueIn = _queueManager.AccessQueue(mqRequest.InputQueue, openInOptions);
                openQueuesStopwatch.Stop();

                // Medir tiempo de preparación del mensaje PUT
                preparePutStopwatch.Start();
                var msgPut = new MQMessage
                {
                    Format = MQC.MQFMT_STRING,
                    CharacterSet = 1208,  // UTF-8
                    MessageId = MQC.MQMI_NONE,
                    CorrelationId = MQC.MQCI_NONE,
                    Persistence = MQC.MQPER_NOT_PERSISTENT
                };

                string content = Encoding.UTF8.GetString(request.Content);
                msgPut.WriteString(content);

                var pmo = new MQPutMessageOptions
                {
                    Options = MQC.MQPMO_NO_SYNCPOINT | MQC.MQPMO_NEW_MSG_ID //| MQC.MQPMO_NO_CONTEXT
                };
                preparePutStopwatch.Stop();

                // Medir tiempo de PUT
                putStopwatch.Start();
                queueOut.Put(msgPut, pmo);
                putStopwatch.Stop();
                
                request.SentAt = DateTime.UtcNow;
                Interlocked.Increment(ref _messagesSentCount);

                // Obtener CorrelationId del mensaje enviado (lo usa MQ para el match)
                byte[] correlationId = new byte[24];
                Array.Copy(msgPut.MessageId, correlationId, 24);

                // Medir tiempo de preparación del mensaje GET
                prepareGetStopwatch.Start();
                var msgGet = new MQMessage
                {
                    CharacterSet = 1208,  // UTF-8
                    Format = MQC.MQFMT_STRING,
                    MessageId = MQC.MQMI_NONE,
                    CorrelationId = correlationId
                };

                // Opciones de GET con espera y match por CorrelationId
                int waitInterval = (int)timeout.TotalMilliseconds;
                if (waitInterval <= 0) waitInterval = 5000; // Default 5 segundos
                if (waitInterval > 300000) waitInterval = 300000; // Max 5 minutos

                var gmo = new MQGetMessageOptions
                {
                    Options = MQC.MQGMO_WAIT | MQC.MQGMO_CONVERT | MQC.MQGMO_NO_SYNCPOINT | MQC.MQGMO_FAIL_IF_QUIESCING,
                    WaitInterval = 6500,//waitInterval,
                  //  MatchOptions = MQC.MQMO_MATCH_CORREL_ID
                };
                prepareGetStopwatch.Stop();

                // Medir tiempo de GET
                getStopwatch.Start();
                queueIn.Get(msgGet, gmo);
                getStopwatch.Stop();

                // Medir tiempo de procesamiento de respuesta
                processResponseStopwatch.Start();
                string responseContent = msgGet.ReadString(msgGet.MessageLength);
                byte[] responseBytes = Encoding.UTF8.GetBytes(responseContent);
                processResponseStopwatch.Stop();

                response = new ResponseMessage(responseBytes, request.CorrelationID);
                response.ReceivedAt = DateTime.UtcNow;

                // Agrego metadata adicional
                if (response.Metadata != null)
                {
                    response.Metadata["MessageId"] = Convert.ToBase64String(msgGet.MessageId);
                    response.Metadata["PutDateTime"] = msgGet.PutDateTime;
                }

                Interlocked.Increment(ref _messagesSentCount);
                
                return response;
            }
            catch (MQException mqEx) when (mqEx.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
            {
                Interlocked.Increment(ref _errorCount);
                throw new TimeoutException($"No se recibió respuesta en el timeout especificado: {timeout}", mqEx);
            }
            catch (MQException mqEx)
            {
                Interlocked.Increment(ref _errorCount);
                throw new Exception($"Error MQ en SendAndReceive: Reason={mqEx.ReasonCode}, Message={mqEx.Message}", mqEx);
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _errorCount);
                throw;
            }
            finally
            {
                // Medir tiempo de cierre de colas
                closeQueuesStopwatch.Start();
                queueOut?.Close();
                queueIn?.Close();
                closeQueuesStopwatch.Stop();
                
                // Detener el cronómetro total
                totalStopwatch.Stop();
                
                // Imprimir todas las mediciones
                Debug.WriteLine($"[ConnectorMQ.SendAndReceive] Mediciones de tiempo - CorrelationID: {request?.CorrelationID ?? "N/A"}");
                Debug.WriteLine($"  Total: {totalStopwatch.ElapsedMilliseconds} ms");
                Debug.WriteLine($"  Apertura de colas (AccessQueue): {openQueuesStopwatch.ElapsedMilliseconds} ms");
                Debug.WriteLine($"  Preparación mensaje PUT: {preparePutStopwatch.ElapsedMilliseconds} ms");
                Debug.WriteLine($"  PUT (queueOut.Put): {putStopwatch.ElapsedMilliseconds} ms");
                Debug.WriteLine($"  Preparación mensaje GET: {prepareGetStopwatch.ElapsedMilliseconds} ms");
                Debug.WriteLine($"  GET (queueIn.Get): {getStopwatch.ElapsedMilliseconds} ms");
                Debug.WriteLine($"  Procesamiento respuesta: {processResponseStopwatch.ElapsedMilliseconds} ms");
                Debug.WriteLine($"  Cierre de colas (Close): {closeQueuesStopwatch.ElapsedMilliseconds} ms");
                
                if (mqRequest != null)
                {
                    Debug.WriteLine($"  OutputQueue: {mqRequest.OutputQueue}, InputQueue: {mqRequest.InputQueue}");
                }
            }
                
        }

        // Métodos Async
        public async Task OpenAsync(IConnectorParameters parameters)
        {
            await Task.Run(() => Open(parameters));
        }

        public async Task CloseAsync()
        {
            await Task.Run(() => Close());
        }

        public async Task<bool> CheckHealthAsync()
        {
            return await Task.Run(() => CheckHealth());
        }

        public async Task<TimeSpan> PingAsync(string param = "")
        {
            return await Task.Run(() => Ping(param));
        }

        public async Task RecycleAsync()
        {
            await Task.Run(() => Recycle());
        }

        public async Task SendAsync(RequestMessage request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            await Task.Run(() => Send(request, timeout, cancellationToken), cancellationToken);
        }

        public async Task<ResponseMessage> SendAndReceiveAsync(RequestMessage request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            return await Task.Run(() => SendAndReceive(request, timeout, cancellationToken), cancellationToken);
        }
    }
}
