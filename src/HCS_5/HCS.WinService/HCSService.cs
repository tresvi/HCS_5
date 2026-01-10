using BNA.FU.HCS;
using HCS.Connector.Abstractions.Interfaces;
using HCS.Connector.Abstractions.Models;
using HCS.Connector.Dummy;
using HCS.Connector.IBMMQ;
using System;
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
        static readonly string _respuetaPing = $"ok - {Environment.MachineName}";
        const string UNKNOWN_COMMAND_RESPONSE = "Comando no reconocido";

        public BindingList<WCFMensaje> EnviarRecibir(WCFMensaje msgMensaje, string strDestino)
        {
            BindingList<WCFMensaje> responses = new BindingList<WCFMensaje>();
            IContextChannel channel = OperationContext.Current?.Channel;


            if (_connectorMQ == null)
            {
                _connectorMQ = new ConnectorMQ();
                IConnectorParameters parameters = new ConnectorParametersMQ() { Channel = "CHANNEL1", ManagerName = "MQGD", ServerIp = "10.6.248.10", ServerPort = 1414 };
                _connectorMQ.Open(parameters);
            }

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
                        respuesta = $"#Conexiones activas: {_openConnCounter}, #MsjesEnviados: {_txsCounter}, MaxConnCounter: {_connCounterMax}";
                    else
                        respuesta = UNKNOWN_COMMAND_RESPONSE;

                }
                else
                { 
                    //respuesta = $"ECO de {request}";

                    //Thread.Sleep(10000);
                    //   for (int i = 0; i < 20; i++)
                    //   {
                    //      Debug.WriteLine($"Channel State: {channel.State.ToString()}");
                        if (channel.State == CommunicationState.Closed)
                        {
                            Console.WriteLine("FINALIZOOOOOOOOO DE GOLPE");
                            throw new Exception("Conexion cerrada por el cliente");
                        }

                    //    Thread.Sleep(1000);
                    //}
                    /*
                    IConnector connector = new ConnectorDummy();
                    IConnectorParameters parameters = new IConnectorParameters() { };
                    connector.Open(parameters);
                    byte[] receivedBytes = connector.SendAndReceive(msgMensaje.Contenido, TimeSpan.FromSeconds(10), null);
                    respuesta += "ECO de " + Encoding.ASCII.GetString(receivedBytes);
                    */


                    RequestMessageMQ requestMQ  = new RequestMessageMQ() { InputQueue = "BNA.CU1.RESPUESTA", OutputQueue = "BNA.CU1.PEDIDO", SendTimeout = TimeSpan.FromSeconds(2) };
                    requestMQ.Content = msgMensaje.Contenido;
                    
                    ResponseMessage response = _connectorMQ.SendAndReceive(requestMQ, TimeSpan.FromSeconds(10), new CancellationToken());
                    //_connectorMQ.Send(requestMQ, TimeSpan.FromSeconds(10), new CancellationToken());
                    //response.Content = new byte[] { 97, 98, 99, 0 };
                    respuesta += "ECO de " + Encoding.ASCII.GetString(requestMQ.Content);
                }

                byte[] respuestaBytes = Encoding.UTF8.GetBytes(respuesta);
                WCFMensaje msje1 = new WCFMensaje() { ID = msgMensaje.ID, Contenido = respuestaBytes };
                responses.Add(msje1);

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
            }

            return responses;
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

    }
}
