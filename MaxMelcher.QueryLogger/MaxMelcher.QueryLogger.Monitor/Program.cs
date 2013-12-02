using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMelcher.QueryLogger.Monitor
{
    class Program
    {
        static void Main(string[] args)
        {
            LogMonitor log = new LogMonitor();
            Task t = log.Start();
            t.Wait();

        }
    }
}
