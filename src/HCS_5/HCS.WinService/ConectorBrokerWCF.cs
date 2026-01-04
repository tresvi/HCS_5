using BNA.FU.HCS;
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
    public class ConectorBrokerWCF : IConectorBrokerWCF
    {
        static int _openConnCounter = 0;
        static long _txsCounter = 0;
        static long _connCounterMax = 0;
        static readonly string _respuetaPing = $"ok - {Environment.MachineName}";
        const string UNKNOWN_COMMAND_RESPONSE = "Comando no reconocido";

        public BindingList<WCFMensaje> EnviarRecibir(WCFMensaje msgMensaje, string strDestino)
        {
            BindingList<WCFMensaje> responses = new BindingList<WCFMensaje>();
            IContextChannel channel = OperationContext.Current?.Channel;

            try
            {
                Interlocked.Increment(ref _openConnCounter);
                Interlocked.Increment(ref _txsCounter);
                if (_openConnCounter > _connCounterMax) Interlocked.Exchange(ref _connCounterMax, _openConnCounter);

                //Thread.Sleep(10000);
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
                    respuesta = $"ECO de {msgMensaje}";
                }

                // Crear cts fuera del using para que viva mientras los handlers puedan ejecutarse
                CancellationTokenSource cts = null;
                EventHandler closedHandler = null;
                EventHandler faultedHandler = null;
                
                try
                {
                    if (channel != null)
                    {
                        cts = new CancellationTokenSource();
                        
                        // Guardar referencias de los handlers para poder desasignarlos
                        closedHandler = (s, e) => 
                        {
                            try
                            {
                                Debug.WriteLine("******SE CANCELAAAAA - Closed******");
                                cts?.Cancel();
                            }
                            catch { }
                        };
                        
                        faultedHandler = (s, e) => 
                        {
                            try
                            {
                                Debug.WriteLine("******SE CANCELAAAAA - Faulted******");
                                cts?.Cancel();
                            }
                            catch { }
                        };
                        
                        // Asignar handlers
                        channel.Closed += closedHandler;
                        channel.Faulted += faultedHandler;
                    }

                    // Aquí iría tu lógica de procesamiento largo
                    // IMPORTANTE: Verificar periódicamente si el cliente se desconectó
                    // para evitar procesar trabajo innecesario
                    
                    // Ejemplo de procesamiento largo con verificación de cancelación:
                    
                    /*
                    for (int i = 0; i < 20; i++)
                    {
                        // Verificar si el cliente se desconectó antes de continuar
                        if (cts?.Token.IsCancellationRequested == true)
                        {
                            throw new OperationCanceledException("Cliente desconectado");
                        }
                        
                        // Verificar estado del canal (opcional, pero útil)
                        if (channel != null && channel.State != CommunicationState.Opened)
                        {
                            throw new OperationCanceledException($"Canal en estado: {channel.State}");
                        }
                        
                        // Tu lógica de procesamiento aquí
                        // ProcesarAlgoLargo();
                        
                        Thread.Sleep(1000);
                    }
                    */
                    byte[] respuestaBytes = Encoding.UTF8.GetBytes(respuesta);
                    WCFMensaje msje1 = new WCFMensaje() { ID = msgMensaje.ID, Contenido = respuestaBytes };
                    responses.Add(msje1);

                }
                finally
                {
                    // SIEMPRE desasignar los handlers antes de salir
                    if (channel != null)
                    {
                        if (closedHandler != null)
                        {
                            channel.Closed -= closedHandler;
                        }
                        if (faultedHandler != null)
                        {
                            channel.Faulted -= faultedHandler;
                        }
                    }
                    
                    // Dispose del cts solo después de desasignar handlers
                    cts?.Dispose();
                }
                
                /*
                IConector oCon = new ConectorBase();
                BNA.FU.HCS.Mensaje oMsg = new BNA.FU.HCS.Mensaje();
                oMsg.ID        = msgMensaje.ID;
                oMsg.Contenido = msgMensaje.Contenido;
                lmReturn = ConvertDataContract(oCon.EnviarRecibir(oMsg, strDestino));
                */
            }
            catch (OperationCanceledException)
            {
                // Cliente se desconectó durante el procesamiento
                Debug.WriteLine("Operación cancelada - cliente desconectado");
                throw new FaultException("Operación cancelada por desconexión del cliente");
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


    }
}
