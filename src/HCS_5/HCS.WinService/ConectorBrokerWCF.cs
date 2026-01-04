using BNA.FU.HCS;
using System;
using System.ComponentModel;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace HCS.WinService
{
    public class ConectorBrokerWCF: IConectorBrokerWCF
    {
        static int _connectionCounter = 0;
        static long _txCounter = 0;

        public BindingList<WCFMensaje> EnviarRecibir(WCFMensaje msgMensaje, string strDestino)
        {
            BindingList<WCFMensaje> lmReturn = new BindingList<WCFMensaje>();
            try
            {
                Interlocked.Increment(ref _connectionCounter);
                Interlocked.Increment(ref _txCounter);

                //Console.WriteLine($"strDestino: {strDestino}");
                //Console.WriteLine($"msgMensaje: {System.Text.Encoding.ASCII.GetString(msgMensaje.Contenido)}");

                string respuesta;

                if (strDestino == "??")
                    respuesta = $"#Conexiones: {_connectionCounter},  #MsjesEnviados: {_txCounter}";
                else
                    respuesta = $"ECO de {msgMensaje}";

                byte[] respuestaBytes = Encoding.UTF8.GetBytes(respuesta);
                WCFMensaje msje1 = new WCFMensaje() { Contenido = respuestaBytes };
                lmReturn.Add(msje1);

                /*
                IConector oCon = new ConectorBase();
                BNA.FU.HCS.Mensaje oMsg = new BNA.FU.HCS.Mensaje();
                oMsg.ID        = msgMensaje.ID;
                oMsg.Contenido = msgMensaje.Contenido;
                lmReturn = ConvertDataContract(oCon.EnviarRecibir(oMsg, strDestino));
                */
            }
            catch (Exception e)
            {
                throw new FaultException(e.Message);
            }
            finally
            {
                //No hay forma de que no pase por aca
                Interlocked.Decrement(ref _connectionCounter);
             //   Console.WriteLine(_connectionCounter);
            }
            return lmReturn;
        }

    }
}
