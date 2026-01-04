using BNA.FU.HCS;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace HCS.WinService
{
    public class ConectorBrokerWCF: IConectorBrokerWCF
    {

        public BindingList<WCFMensaje> EnviarRecibir(WCFMensaje msgMensaje, string strDestino)
        {
            BindingList<WCFMensaje> lmReturn = new BindingList<WCFMensaje>();
            try
            {
                Console.WriteLine($"strDestino: {strDestino}");
                Console.WriteLine($"msgMensaje: {System.Text.Encoding.ASCII.GetString(msgMensaje.Contenido)}");
                string respuesta = $"ECO de {msgMensaje}";

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
            return lmReturn;
        }

        /*
        private BindingList<WCFMensaje> ConvertDataContract(List<BNA.FU.HCS.Mensaje> lmReturn)
        {
            BindingList<WCFMensaje> lmsgRet = new BindingList<WCFMensaje>();
            foreach (BNA.FU.HCS.Mensaje oMsg in lmReturn)
            {
                WCFMensaje oMsgWCF = new WCFMensaje();
                oMsgWCF.ID = oMsg.ID;
                oMsgWCF.Contenido = oMsg.Contenido;
                lmsgRet.Add(oMsgWCF);
            }
            return lmsgRet;
        }
        */
    }
}
