using System.ComponentModel;
using System.Runtime.Serialization;
using System.ServiceModel;

//!!! Conservar el Namespace para evitar problemas de compatibilidad con las verisones viejas
namespace BNA.FU.HCS
{
    //!!! Para variar, esta mal nombrada. Deveria llamarse IServiceBrokerWCF o similar. 
    [ServiceContract()]   
    internal interface IConectorBrokerWCF
    {
        [OperationContract]
        BindingList<WCFMensaje> EnviarRecibir(WCFMensaje msgMensaje, string strDestino);
    }

    [DataContract]
    public class WCFMensaje
    {
        [DataMember(Name = "ID")]
        public string ID { get; set; }

        [DataMember(Name = "Contenido")]
        public byte[] Contenido { get; set; }
    }
}
