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
            DirectoryMonitor directory = new DirectoryMonitor();
            Task t = directory.Start();
            t.Wait();

        }
    }
}
