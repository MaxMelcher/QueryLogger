using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace MaxMelcher.QueryLogger.Utils
{
    public class UlsHub : Hub
    {
        public void Notify(LogEntry logEntry)
        {
            Console.WriteLine("Message: {0}", logEntry);
            Clients.All.addMessage(logEntry);

            //additionally, if its a search message, send a second message
            if (Regex.IsMatch(logEntry.Area, "Search", RegexOptions.IgnoreCase))
            {
                NotifySearchQuery(logEntry);
            }
        }

        public void NotifySearchQuery(LogEntry logEntry)
        {
            Console.WriteLine("Search: {0}", logEntry);
            Clients.All.addSearchQuery(logEntry);
        }
    }
}
