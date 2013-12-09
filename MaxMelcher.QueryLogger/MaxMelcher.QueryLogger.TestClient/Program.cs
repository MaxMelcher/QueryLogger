using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MaxMelcher.QueryLogger.Utils;
using Microsoft.AspNet.SignalR.Client;

namespace MaxMelcher.QueryLogger.TestClient
{
    class Program
    {
        static IHubProxy hub;


        static void Main(string[] args)
        {

            var hubConnection = new HubConnection("http://sharepoint2013:8080");

            hub = hubConnection.CreateHubProxy("UlsHub");
            hubConnection.TraceLevel = TraceLevels.All;
//            hubConnection.TraceWriter = Console.Out;

            hubConnection.Start().Wait();

            hub.On<LogEntry>("addMessage", s => Console.WriteLine(s.Message));

            Console.ReadLine();
        }
    }
}
