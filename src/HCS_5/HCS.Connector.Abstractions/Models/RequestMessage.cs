using System;
using System.Collections.Generic;

namespace HCS.Connector.Abstractions.Models
{
    public abstract class RequestMessage
    {
        public byte[] Content { get; set; }
        public string CorrelationID { get; set; }
        public TimeSpan SendTimeout { get; set; }

        /// <summary>
        /// Nombre de la aplicacion que envia el msje
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// Timestamp de cuando se envió el mensaje
        /// </summary>
        public DateTime? SentAt { get; set; }

        /// <summary>
        /// Metadata adicional específico del conector (opcional)
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        public RequestMessage() { }

        public RequestMessage(byte[] content, string correlationID = null, string appName = null)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            CorrelationID = correlationID ?? Guid.NewGuid().ToString();
            AppName = appName;
        }
    }
}
