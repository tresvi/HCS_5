using System;
using System.Collections.Generic;

namespace HCS.Connector.Abstractions.Models
{
    public class ResponseMessage
    {
        public byte[] Content { get; set; }
        public string CorrelationID { get; set; }

        /// <summary>
        /// Timestamp de cuando se envió el mensaje
        /// </summary>
        public DateTime? ReceivedAt { get; private set; }

        /// <summary>
        /// Metadata adicional específico del conector (opcional)
        /// </summary>
        public Dictionary<string, object> ExtendedMetadata { get; set; } = new Dictionary<string, object>();

        public ResponseMessage() { }

        public ResponseMessage(byte[] content, string correlationID)
            : this()
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            CorrelationID = correlationID;
        }

    }
}
