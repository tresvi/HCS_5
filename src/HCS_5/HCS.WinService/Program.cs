using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace HCS.WinService
{
    internal static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        static void Main()
        {
#if DEBUG
            // Modo consola para debug
            RunAsConsole();
#else
            // Modo servicio normal
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new HCSWinService()
            };
            ServiceBase.Run(ServicesToRun);
#endif
        }

#if DEBUG
        /// <summary>
        /// Ejecuta el servicio en modo consola para facilitar el debug
        /// </summary>
        static void RunAsConsole()
        {
            Console.WriteLine("Ejecutando servicio en modo consola (Debug)");
            Console.WriteLine("Presione 'Q' para detener el servicio...\n");

            var service = new HCSWinService();
            string[] args = new string[] { };
            service.Start(args);

            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);
            } while (key.KeyChar != 'q' && key.KeyChar != 'Q');

            Console.WriteLine("\nDeteniendo servicio...");
            service.Stop();
            Console.WriteLine("Servicio detenido.");
        }
#endif
    }
}


/*
1- Desde la carpeta de la solución, ejecutar el comando:

dotnet build HCS_5.sln --configuration Debug

2- Ejecutar el programa
.\HCS.WinService\bin\Debug\HCS.WinService.exe
*/