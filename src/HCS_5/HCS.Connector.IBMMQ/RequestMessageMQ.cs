using HCS.Connector.Abstractions.Models;
using System;

namespace HCS.Connector.IBMMQ
{
    public class RequestMessageMQ : RequestMessage
    {
        /// <summary>
        /// Nombre de la cola de entrada desde donde se recibirá la respuesta
        /// </summary>
        public string InputQueue { get; set; }
        
        /// <summary>
        /// Nombre de la cola de salida donde se enviará el mensaje
        /// </summary>
        public string OutputQueue { get; set; }

        public RequestMessageMQ() { }
        
        public RequestMessageMQ(byte[] content, string inputQueue, string outputQueue, string correlationID = null, string appName = null)
            : base(content, correlationID, appName)
        {
            InputQueue = inputQueue ?? throw new ArgumentNullException(nameof(inputQueue));
            OutputQueue = outputQueue ?? throw new ArgumentNullException(nameof(outputQueue));
        }
    }
}
