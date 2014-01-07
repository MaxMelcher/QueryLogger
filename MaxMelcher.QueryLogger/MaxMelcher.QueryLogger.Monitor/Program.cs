using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using MaxMelcher.QueryLogger.Monitor.Properties;
using MaxMelcher.QueryLogger.Utils;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Hosting;
using Owin;

namespace MaxMelcher.QueryLogger.Monitor
{
    class Program
    {
        static void Main(string[] args)
        {

            bool startServer = Settings.Default.StartServer;
            
            if (startServer)
            {
               Task.Factory.StartNew(StartServer);
            }


            DirectoryMonitor directory = new DirectoryMonitor();
            Task t = directory.Start();

            t.Wait();
        }

        private static void StartServer()
        {
            string serverUrl = Settings.Default.ServerUrl;

            //load the assembly so that signalr detects the hub.
            AppDomain.CurrentDomain.Load(typeof(UlsHub).Assembly.FullName);
            
            WebApp.Start<Startup>(serverUrl);
            Console.WriteLine("Server running on {0}", serverUrl);
        }

        class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                app.UseCors(CorsOptions.AllowAll);
                app.MapSignalR();
            }
        }

        
    }
}
