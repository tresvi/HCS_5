using System;
using System.Collections.Generic;
using System.Text;
using System.ServiceModel;
using System.ComponentModel;
using System.Runtime.Serialization;

//!!! Conservar el Namespace para evitar prpoblemas de compatibilidad con las verisones viejas
namespace BNA.FU.HCS
{
    //!!! Para variar, esta mal nombrada. Deveria llamarse IServiceBrokerWCF o similar. 
    [ServiceContract()]   
    internal interface IConectorBrokerWCF
    {
        /// <summary>
        /// Interface WCF para el uso del servicio HCS
        /// </summary>
        /// <param name="msgMensaje"></param>
        ///     Objeto de tipo Mensaje que contiene el mensaje de Input en formato Byte array
        ///     a ser transmitido al destinatario 
        /// <param name="strDestino"></param>
        ///     String que indica el nombre de la transacción destino que va a atender el 
        ///     Mensaje, valor que va 
        /// <returns name="MensajeRetorno"></returns>
        ///     Objeto de tipo List conteniendo el o los objetos de tipo Mensaje que son resultado
        ///     de ejecutar la transacción indicada en strDestino
        [OperationContract]
        BindingList<WCFMensaje> EnviarRecibir(WCFMensaje msgMensaje, string strDestino);
    }


    /// <summary>
    ///  Objeto Mensaje expuesto via el mecanismo de WCF
    /// </summary>
    [DataContract]
    public class WCFMensaje
    {
        private string m_ID;
        private byte[] m_Contenido;
        /// <summary>
        /// Propiedad ID
        /// Tipo    : string 
        /// Accesso : read/write
        /// Objetivo: permite el uso de una identificación del mensaje
        /// 
        /// </summary>
        [DataMember(Name = "ID")]
        public string ID
        {
            get
            {
                return m_ID;
            }

            set
            {
                m_ID = value;
            }
        }

        /// <summary>
        /// Contendido del mensaje.
        /// Tipo    : Byte Array
        /// Accesso : read/write
        /// Objetivo: Contener un Byte Array con el contenido del mensaje
        /// </summary>
        [DataMember(Name = "Contenido")]
        public byte[] Contenido
        {
            get
            {
                return m_Contenido;
            }
            set
            {
                m_Contenido = value;
            }
        }

    }
}
