using System;
using System.Collections.Generic;

namespace HCS.Connector.Abstractions.Models
{
    public class RequestMessage
    {
        public byte[] Content { get; set; }
        public string CorrelationID { get; set; }
        public string AppName { get; set; }

        /// <summary>
        /// Timestamp de cuando se creó el mensaje
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Metadata adicional específico del conector (opcional)
        /// </summary>
        public Dictionary<string, object> ExtendedMetadata { get; set; }

        public RequestMessage()
        {
            ExtendedMetadata = new Dictionary<string, object>();
        }

        public RequestMessage(byte[] content, string correlationID = null, string appName = null)
            : this()
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            CorrelationID = correlationID ?? Guid.NewGuid().ToString();
            AppName = appName;
        }
    }
}
