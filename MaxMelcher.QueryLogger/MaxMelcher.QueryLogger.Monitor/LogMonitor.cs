using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


//TODO switch to log4net for logging
//TODO parse the last line
//comment

namespace MaxMelcher.QueryLogger.Monitor
{
    public class LogMonitor
    {
        /// <summary>
        /// This is the inner task that handles the actual monitoring
        /// </summary>
        public Task MonitorTask;

        readonly FileSystemWatcher _watcherFolder = new FileSystemWatcher();
        readonly FileSystemWatcher _watcherFile = new FileSystemWatcher();
        readonly TaskCompletionSource<object> _tcs = new TaskCompletionSource<object>();

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
                string logsLocation = SPUtility.GetLogsLocation();
                _watcherFolder.Path = logsLocation;
                _watcherFolder.Filter = "*.log";

                Console.WriteLine("Monitoring Folder {0}", logsLocation);

                string logFile = SPUtility.GetLastAccessedFile(logsLocation);
                _watcherFile.Path = logsLocation;

                var dirInfo = new DirectoryInfo(logsLocation);
                var file = dirInfo.GetFiles().OrderByDescending(f => f.LastWriteTime).FirstOrDefault();

                if (file == null)
                {
                    Console.WriteLine("No log file found in {0}", logsLocation);
                    throw new Exception(string.Format("No log file found {0}", logsLocation));
                }

                _watcherFile.Filter = file.Name;
                _watcherFile.NotifyFilter = NotifyFilters.LastWrite;
                LogFilePath = file.FullName;

                Console.WriteLine("Monitoring Logfile for write changes: {0} ", logFile);

                _watcherFolder.Created += (sender, args) =>
                {
                    Console.WriteLine("File {0} created", args.FullPath);
                    _watcherFile.Filter = args.Name;
                    LogFilePath = args.FullPath;
                };

                _watcherFile.Changed += logfileChanged;

                _watcherFile.EnableRaisingEvents = true;
                _watcherFolder.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                _tcs.SetException(ex);
            }
        }

        void logfileChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine("Logfile {0} changed", e.FullPath);
        }

        public void Stop()
        {
            _watcherFile.EnableRaisingEvents = false;
            _watcherFolder.EnableRaisingEvents = false;
            _tcs.TrySetCanceled();
        }
    }
}