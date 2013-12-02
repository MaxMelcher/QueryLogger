using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SharePoint.Administration;

namespace MaxMelcher.QueryLogger.Monitor
{
    public class LogMonitor
    {
        public string LogFilePath { get; set; }
        public readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        public CancellationTokenSource _cts;

        readonly char[] _seperators = { '\r', '\n' };

        public Task LogMonitorTask;

        public Task Start()
        {
            Console.WriteLine("Starting LogMonitor");
            _cts = new CancellationTokenSource();
            LogMonitorTask = Task.Factory.StartNew(Watch,_cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            return _tcs.Task;
        }

        public void Watch()
        {
            try
            {
                using (var stream = new FileStream(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        reader.ReadToEnd();
                        string cache = String.Empty;
                        while (!_cts.IsCancellationRequested)
                        {
                            Thread.Sleep(1000);
                            if (reader.EndOfStream)
                                continue;
                            cache += reader.ReadToEnd();
                            string[] lines = cache.Split(_seperators, StringSplitOptions.RemoveEmptyEntries);
                            
                            //wait until we have a proper line
                            cache = cache.EndsWith("\n") ? String.Empty : lines.Last();
                            
                            int validLines = cache == String.Empty ? lines.Length : lines.Length - 1;

                            foreach (string line in lines.Take(validLines))
                            {
                                
                                LogEntry l = LogEntry.Parse(line);

                                Console.WriteLine("{0} {1} {2}", l.Timestamp, l.Process, l.Thread);
                            }
                        }
                        Console.WriteLine("LogMonitor stopped");
                    }
                }
            }
            catch (Exception ex)
            {
                _tcs.SetException(ex);
            }
        }

        public void Stop()
        {
            Console.WriteLine("Stopping LogMonitor");            
            _cts.Cancel();
        }
    }
}