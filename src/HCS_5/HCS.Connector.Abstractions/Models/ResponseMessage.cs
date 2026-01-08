using System;
using System.Collections.Generic;
using System.Text;

namespace HCS.Connector.Abstractions.Models
{
    public class ResponseMessage
    {
        public byte[] Content { get; set; }
        public string CorrelationID { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Metadata adicional específico del conector (opcional)
        /// </summary>
        public Dictionary<string, object> ExtendedMetadata { get; set; }

        public ResponseMessage()
        {
            ExtendedMetadata = new Dictionary<string, object>();
        }

        public ResponseMessage(byte[] content, string correlationID)
            : this()
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            CorrelationID = correlationID;
        }

    }
}
