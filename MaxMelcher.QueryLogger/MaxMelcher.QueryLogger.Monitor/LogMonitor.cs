using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MaxMelcher.QueryLogger.Monitor.Properties;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Client;
using MaxMelcher.QueryLogger.Utils;

namespace MaxMelcher.QueryLogger.Monitor
{
    public class LogMonitor
    {
        public string LogFilePath { get; set; }
        public readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        public CancellationTokenSource Cts;
        


        readonly char[] _seperators = { '\r', '\n' };

        public Task LogMonitorTask;
        IHubProxy hub;

        public LogMonitor(string logFilePath)
        {
            LogFilePath = logFilePath;
        }
        /// <summary>
        /// Starts the watching of a log file
        /// </summary>
        /// <returns></returns>
        public Task Start()
        {
            Console.WriteLine("Starting LogMonitor for file: {0}", LogFilePath);
            Cts = new CancellationTokenSource();

            var hubConnection = new HubConnection(Settings.Default.ServerUrl);

            hub = hubConnection.CreateHubProxy("UlsHub");
            hubConnection.Start().Wait();

            LogMonitorTask = Task.Factory.StartNew(Watch,Cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

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
                        while (!Cts.IsCancellationRequested)
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
                                LogEntry logentry = LogEntry.Parse(line);
                                Console.WriteLine("{0} {1} {2}", logentry.Timestamp, logentry.Process, logentry.Message);
                                hub.Invoke("Notify", logentry);
                            }
                        }
                        Console.WriteLine("LogMonitor stopped");
                        _tcs.SetResult(null);
                    }
                }
            }
            catch (Exception ex)
            {
                _tcs.SetException(ex);
            }
        }
        /// <summary>
        /// Stops the watching of a file
        /// </summary>
        public void Stop()
        {
            Console.WriteLine("Stopping LogMonitor");            
            Cts.Cancel();
        }
    }
}