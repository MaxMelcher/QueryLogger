using System;
using MaxMelcher.QueryLogger.Utils;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Owin;
using Microsoft.Owin.Cors;


namespace MaxMelcher.QueryLogger.SignalrConsoleHost
{
    class Program
    {
        static void Main(string[] args)
        {
            string url = "http://sharepoint2013:8080";
            using (WebApp.Start<Startup>(url))
            {
                Console.WriteLine("Server running on {0}", url);
                Console.ReadLine();
            }
        }
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
