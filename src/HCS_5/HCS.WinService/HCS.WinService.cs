using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace HCS.WinService
{
    public partial class HCSWinService : ServiceBase
    {
        const string SERVICE_NAME = "BNA - Host Communications Service 5.0";

        public HCSWinService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Start(args);
        }

        protected override void OnStop()
        {
            Stop();
        }

        public void Start(string[] args)
        {
            // Aquí va la lógica de inicio del servicio
        }

        public void Stop()
        {
            // Aquí va la lógica de detención del servicio
        }

        void HCSServiceHost_Error(object sender, EventArgs e)
        {
            if (!EventLog.SourceExists(SERVICE_NAME))
            {
                EventLog.CreateEventSource(SERVICE_NAME, "Application");
            }
            EventLog.WriteEntry(SERVICE_NAME, "Fallo en el servicio WCF.");
        }
        void HCSServiceHost_Opened(object sender, EventArgs e)
        {
            if (!EventLog.SourceExists(SERVICE_NAME))
            {
                EventLog.CreateEventSource(SERVICE_NAME, "Application");
            }
            EventLog.WriteEntry(SERVICE_NAME, "Los servicios WCF están en modo Abierto.");
        }
        void HCSServiceHost_Closed(object sender, EventArgs e)
        {
            if (!EventLog.SourceExists(SERVICE_NAME))
            {
                EventLog.CreateEventSource(SERVICE_NAME, "Application");
            }
            EventLog.WriteEntry(SERVICE_NAME, "Los servicios WCF están en modo Cerrado.");
        }
    }
}
