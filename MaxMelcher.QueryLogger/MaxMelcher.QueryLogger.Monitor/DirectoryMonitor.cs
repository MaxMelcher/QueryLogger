using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


//TODO switch to log4net for logging
//TODO parse the last line
//comment

namespace MaxMelcher.QueryLogger.Monitor
{
    public class DirectoryMonitor
    {
        /// <summary>
        /// This is the inner LogMonitorTask that handles the actual monitoring
        /// </summary>
        public Task MonitorTask;

        readonly FileSystemWatcher _watcherFolder = new FileSystemWatcher();
        readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();
        private LogMonitor _logMonitor;
        private Task taskLogMonitor;

        public string LogFilePath;


        /// <summary>
        /// Starts watching for folder changes
        /// </summary>
        /// <returns></returns>
        public Task Start()
        {
            MonitorTask = Task.Factory.StartNew(MonitorFolder);
            return _tcs.Task;
        }

        private void MonitorFolder()
        {
            try
            {
                Console.WriteLine("IsWSSInstalled installed:" + SPUtility.IsWSSInstalled);
                Console.WriteLine("IsMOSSInstalled installed:" + SPUtility.IsMOSSInstalled);
                string logsLocation = SPUtility.GetLogsLocation();
                _watcherFolder.Path = logsLocation;
                _watcherFolder.Filter = "*.log";

                Console.WriteLine("Monitoring Folder {0}", logsLocation);

                var dirInfo = new DirectoryInfo(logsLocation);
                var file = dirInfo.GetFiles().OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

                if (file == null)
                {
                    Console.WriteLine("No log file found in {0}", logsLocation);
                    throw new Exception(string.Format("No log file found {0}", logsLocation));
                }

                LogFilePath = file.FullName;

                //attach a LogMonitor to the file
                _logMonitor = new LogMonitor(LogFilePath);
                taskLogMonitor = _logMonitor.Start();

                _watcherFolder.Created += (sender, args) =>
                {
                    Console.WriteLine("File {0} created", args.FullPath);
                    LogFilePath = args.FullPath;

                    //stop the task
                    _logMonitor.Stop();

                    //wait for the stop
                    taskLogMonitor.Wait();
                    
                    //start a new task
                    _logMonitor = new LogMonitor(LogFilePath);
                    LogFilePath = args.FullPath;
                    taskLogMonitor = _logMonitor.Start();
                };

                _watcherFolder.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                _tcs.SetException(ex);
            }
        }
        /// <summary>
        /// Stops the watching
        /// </summary>
        public void Stop()
        {
            _watcherFolder.EnableRaisingEvents = false;
            _tcs.TrySetCanceled();
        }
    }
}