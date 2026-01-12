using BNA.FU.HCS;
using HCS.Connector.Abstractions.Interfaces;
using HCS.Connector.Abstractions.Models;
using HCS.Connector.Dummy;
using HCS.Connector.IBMMQ;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace HCS.WinService
{
    //Esta configuración eleva el throughput notablemente, de 80K en 30seg a 116K
    //Resultado de test de carga con la consulta a "$PING$" en "??" con 1000 hilos en 30 seg.

    [ServiceBehavior(
        InstanceContextMode = InstanceContextMode.PerCall,
        ConcurrencyMode = ConcurrencyMode.Multiple,
        UseSynchronizationContext = false
    )]
    public class HCSService : IConectorBrokerWCF
    {

        private static IConnector _connectorMQ;

        static int _openConnCounter = 0;
        static long _txsCounter = 0;
        static long _connCounterMax = 0;
        static long _canceledByClientConnCounter = 0;
        private static readonly object _connectorLock = new object();
        static readonly string _respuetaPing = $"ok - {Environment.MachineName}";
        const string UNKNOWN_COMMAND_RESPONSE = "Comando no reconocido";


        public BindingList<WCFMensaje> EnviarRecibir(WCFMensaje msgMensaje, string strDestino)
        {
            Stopwatch swTotal = new Stopwatch();
            Stopwatch swConvert = new Stopwatch();
            Stopwatch swVerify = new Stopwatch();
            Stopwatch swSendAndReceive = new Stopwatch();
            
            swTotal.Start();
            BindingList<WCFMensaje> responses = new BindingList<WCFMensaje>();
            IContextChannel channel = OperationContext.Current?.Channel;

            swVerify.Start();
            VerifyConnector();
            swVerify.Stop();

            try
            {
                Interlocked.Increment(ref _openConnCounter);
                Interlocked.Increment(ref _txsCounter);
                if (_openConnCounter > _connCounterMax) Interlocked.Exchange(ref _connCounterMax, _openConnCounter);

                string request = Encoding.ASCII.GetString(msgMensaje.Contenido);

                //Console.WriteLine($"strDestino: {strDestino}");
                //Console.WriteLine($"msgMensaje: {System.Text.Encoding.ASCII.GetString(msgMensaje.Contenido)}");

                string respuesta = "";

                if (strDestino.Trim() == "??")
                {
                    if (request.Trim() == "$PING$")
                        respuesta = _respuetaPing;
                    else if (request.Trim() == "$STATUS$")
                        respuesta = $"#Conexiones activas: {_openConnCounter}, #MsjesEnviados: {_txsCounter}, MaxConnCounter: {_connCounterMax}, Canceled by Client: {_canceledByClientConnCounter}";
                    else
                        respuesta = UNKNOWN_COMMAND_RESPONSE;

                    byte[] respuestaBytes = Encoding.UTF8.GetBytes(respuesta);
                    WCFMensaje msje1 = new WCFMensaje() { ID = msgMensaje.ID, Contenido = respuestaBytes };
                    responses.Add(msje1);
                    return responses;
                }

                //respuesta = $"ECO de {request}";

                /*
                IConnector connector = new ConnectorDummy();
                IConnectorParameters parameters = new IConnectorParameters() { };
                connector.Open(parameters);
                byte[] receivedBytes = connector.SendAndReceive(msgMensaje.Contenido, TimeSpan.FromSeconds(10), null);
                respuesta += "ECO de " + Encoding.ASCII.GetString(receivedBytes);
                */

                //RequestMessageMQ requestMQ  = new RequestMessageMQ() { InputQueue = "BNA.CU1.RESPUESTA", OutputQueue = "BNA.CU1.PEDIDO", SendTimeout = TimeSpan.FromSeconds(2) };
                RequestMessageMQ requestMQ = new RequestMessageMQ() { InputQueue = "BNA.TU5.RESPUESTA", OutputQueue = "BNA.TU5.PEDIDO", SendTimeout = TimeSpan.FromSeconds(2) };
                requestMQ.Content = msgMensaje.Contenido;

                swSendAndReceive.Start();
                ResponseMessage response = _connectorMQ.SendAndReceive(requestMQ, TimeSpan.FromSeconds(10), new CancellationToken());
                swSendAndReceive.Stop();

                //_connectorMQ.Send(requestMQ, TimeSpan.FromSeconds(10), new CancellationToken());
                //response.Content = new byte[] { 97, 98, 99, 0 };

                //Thread.Sleep(10000);
                //   for (int i = 0; i < 20; i++)
                //   {
                //      Debug.WriteLine($"Channel State: {channel.State.ToString()}");
                if (channel.State == CommunicationState.Closed)
                {
                    Console.WriteLine("FINALIZOOOOOOOOO DE GOLPE");
                    //throw new Exception("Conexion cerrada por el cliente");
                    Interlocked.Increment(ref _canceledByClientConnCounter);
                    return null;
                }

                swConvert.Start();
                BindingList<WCFMensaje> wcfResponse = ConvertResponseToWCFMessage(in response, msgMensaje.ID);
                swConvert.Stop();

                return wcfResponse;

                //    Thread.Sleep(1000);
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine($"Excepcion: {e.Message}");
                throw new FaultException(e.Message);
            }
            finally
            {
                //No hay forma de que no pase por aca
                Interlocked.Decrement(ref _openConnCounter);
                swTotal.Stop();
                Debug.WriteLine($"Total: {swTotal.ElapsedMilliseconds} = Verify:{swVerify.ElapsedMilliseconds}  +  S&R: {swSendAndReceive.ElapsedMilliseconds} + Conv: {swConvert.ElapsedMilliseconds} ");
            }
        }

        void Cancel(CancellationTokenSource cts)
        {
            try
            {
                Debug.WriteLine("******SE CANCELAAAAA******");
                cts.Cancel();
            }
            catch { }
        }


        private BindingList<WCFMensaje> ConvertResponseToWCFMessage(in ResponseMessage response, string correlationID)
        {
            if (response?.Content == null) return null;

            BindingList<WCFMensaje> responses = new BindingList<WCFMensaje>();

            foreach (byte[] contentBytes in response.Content)
            {
                WCFMensaje wcfMessage = new WCFMensaje()
                {
                    ID = correlationID,
                    Contenido = contentBytes
                };
                responses.Add(wcfMessage);
            }
            
            return responses;
        }


        IConnector VerifyConnector()
        { 
            lock (_connectorLock)
            {
                if (_connectorMQ != null && _connectorMQ.State == ConnectionStateEnum.Opened)
                    return _connectorMQ;

                if (_connectorMQ != null)
                {
                    try { _connectorMQ.Close(); } catch { }
                    _connectorMQ = null;
                }
                
                _connectorMQ = new ConnectorMQ();
                IConnectorParameters parameters = new ConnectorParametersMQ()
                {
                    Channel = "CHANNEL1",
                    ManagerName = "MQGD",
                    ServerIp = "10.6.248.10",
                    ServerPort = 1414
                };
                
                _connectorMQ.Open(parameters);
                return _connectorMQ;
            }
        }


    }
}
