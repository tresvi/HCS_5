using HCS.Connector.Abstractions.Interfaces;
using HCS.Connector.Abstractions.Models;
using IBM.WMQ;
using System;
using System.Collections;
using System.Collections.Generic;
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
        private long _totalSentMessages;
        private long _totalReceivedMessages;
        private long _totalSentMessageErrors;
        private long _totalReceivedMessageErrors;

        public string Name { get { return NAME; } }
        public long TotalSentMessages => _totalSentMessages;
        public long TotalReceivedMessages => _totalReceivedMessages;
        public long TotalSentMessageErrors => _totalSentMessageErrors;
        public long TotalReceivedMessageErrors => _totalReceivedMessageErrors;

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
                    State = ConnectionStateEnum.Faulted;
                    throw new Exception($"Error al conectar con IBM MQ: Reason={mqEx.ReasonCode}, Message={mqEx.Message}", mqEx);
                }
                catch (Exception ex)
                {
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
            if (!(request is RequestMessageMQ mqRequest))
                throw new ArgumentException("Request must be of type RequestMessageMQ", nameof(request));

            (byte[] correlationId, _) = Send(mqRequest);
            //TODO: Deberia inyecar el Correlation ID ? Probablemente si
        }


        MQQueue _queueOut = null;
        MQQueue _queueIn = null;

        /// <summary>
        /// Envía un mensaje a la cola de salida y retorna el CorrelationId para recibir la respuesta
        /// </summary>
        private (byte[] correlationId, DateTime putDateTime) Send(RequestMessageMQ mqRequest)
        {
            if (string.IsNullOrEmpty(mqRequest.OutputQueue))
                throw new ArgumentException("OutputQueue must be specified", nameof(mqRequest));

            try
            {
                // Abrir cola de salida si es necesario
                if (_queueOut == null)
                {
                    int openOutOptions = MQC.MQOO_OUTPUT | MQC.MQOO_FAIL_IF_QUIESCING;
                    _queueOut = _queueManager.AccessQueue(mqRequest.OutputQueue, openOutOptions);
                }

                // Preparar mensaje PUT
                var msgPut = new MQMessage
                {
                    Format = MQC.MQFMT_STRING,
                    CharacterSet = 1208,  // UTF-8
                    MessageId = MQC.MQMI_NONE,
                    CorrelationId = MQC.MQCI_NONE,
                    Persistence = MQC.MQPER_NOT_PERSISTENT
                };

                string content = Encoding.UTF8.GetString(mqRequest.Content);
                msgPut.WriteString(content);

                var pmo = new MQPutMessageOptions
                {
                    Options = MQC.MQPMO_NO_SYNCPOINT | MQC.MQPMO_NEW_MSG_ID //| MQC.MQPMO_NO_CONTEXT
                };

                _queueOut.Put(msgPut, pmo);

                mqRequest.SentAt = DateTime.UtcNow;
                Interlocked.Increment(ref _totalSentMessages);

                // Obtener CorrelationId del mensaje enviado (lo usa MQ para el match)
                byte[] correlationId = new byte[24];
                Array.Copy(msgPut.MessageId, correlationId, 24);

                return (correlationId, msgPut.PutDateTime);
            }
            catch (MQException mqEx)
            {
                Interlocked.Increment(ref _totalSentMessageErrors);
                throw;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalSentMessageErrors);
                throw;
            }
        }

        /// <summary>
        /// Recibe un mensaje de la cola de entrada usando el CorrelationId y retorna la respuesta
        /// </summary>
        private (ResponseMessage response, DateTime putDateTime) Receive(RequestMessageMQ mqRequest, byte[] correlationId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(mqRequest.InputQueue))
                throw new ArgumentException("InputQueue must be specified", nameof(mqRequest));
            List <byte[]> mqResponses = new List <byte[]>();

            try
            {
                // Abrir cola de entrada si es necesario
                if (_queueIn == null)
                {
                    int openInOptions = MQC.MQOO_INPUT_AS_Q_DEF | MQC.MQOO_FAIL_IF_QUIESCING;
                    _queueIn = _queueManager.AccessQueue(mqRequest.InputQueue, openInOptions);
                }

                // Preparar mensaje GET
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
                    WaitInterval = 6500, //waitInterval,
                    MatchOptions = MQC.MQMO_MATCH_CORREL_ID
                };

                // GET
                bool goOn = true;
                while (goOn)
                { 
                    _queueIn.Get(msgGet, gmo);
                    string responseContent = msgGet.ReadString(msgGet.MessageLength);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(responseContent);
                    mqResponses.Add(responseBytes);

                    //Analizar para ver si es el mensaje final.
                    //El bit de último mensaje en el grupo está encendido.
                    if (msgGet.Feedback == MQC.MQFB_APPL_LAST) 
                        break;
                }

                var response = new ResponseMessage(mqResponses, mqRequest.CorrelationID);
                response.ReceivedAt = DateTime.UtcNow;
                //response.
                // Agrego metadata adicional
                /*if (response.Metadata != null)
                {
                    response.Metadata["MessageId"] = Convert.ToBase64String(msgGet.MessageId);
                    response.Metadata["PutDateTime"] = msgGet.PutDateTime;
                }
                */
                Interlocked.Increment(ref _totalReceivedMessages);

                return (response, msgGet.PutDateTime);
            }
            catch (MQException mqEx)
            {
                Interlocked.Increment(ref _totalReceivedMessageErrors);
                throw;
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref _totalReceivedMessageErrors);
                throw;
            }
        }


        public ResponseMessage SendAndReceive(RequestMessage request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!(request is RequestMessageMQ mqRequest))
                throw new ArgumentException("Request must be of type RequestMessageMQ", nameof(request));

            if (State != ConnectionStateEnum.Opened)
                throw new InvalidOperationException("Connector is not open");

            try
            {
                DateTime firstPutDateTime, LastPutDateTime;
                byte[] correlationId;
                ResponseMessage response;

                // Enviar mensaje y obtener CorrelationId
                (correlationId, firstPutDateTime) = Send(mqRequest);

                // Recibir respuesta usando el CorrelationId
                (response, LastPutDateTime) = Receive(mqRequest, correlationId, timeout, cancellationToken);

                return response;
            }
            catch (MQException mqEx) when (mqEx.ReasonCode == MQC.MQRC_NO_MSG_AVAILABLE)
            {
                // Timeout en Receive - ya contado en Receive() como _totalReceivedMessageErrors
                throw new TimeoutException($"No se recibió respuesta en el timeout especificado: {timeout}", mqEx);
            }
            catch (MQException mqEx)
            {
                throw new Exception($"Error MQ en SendAndReceive: Reason={mqEx.ReasonCode}, Message={mqEx.Message}", mqEx);
            }
            catch (Exception ex)
            {
                throw;
            }
            finally
            {
                // _queueOut?.Close();
                // _queueIn?.Close();
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
