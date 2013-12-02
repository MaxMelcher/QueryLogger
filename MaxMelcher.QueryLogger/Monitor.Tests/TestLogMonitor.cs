using System.IO;
using System.Threading;
using MaxMelcher.QueryLogger.Monitor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Monitor.Tests
{
    [TestClass]
    public class TestLogMonitor
    {
        [TestMethod]
        public void AfterStartTheLogMonitorWatchesForNewFiles()
        {
            LogMonitor log = new LogMonitor();
            log.Start();

            log.MonitorTask.Wait();

            Assert.IsFalse(string.IsNullOrEmpty(log.LogFilePath));
        }

        [TestMethod]
        public void TheLogFilePathChangesAfterANewFileIsCreated()
        {
            LogMonitor log = new LogMonitor();
            log.Start();

            log.MonitorTask.Wait();
            Assert.IsFalse(string.IsNullOrEmpty(log.LogFilePath));

            FileInfo file = new FileInfo(log.LogFilePath);

            string testFile = Path.Combine(file.Directory.FullName, "test.log");

            //delete existing, old files
            if (File.Exists(testFile))
            {
                File.Delete(testFile);                
            }

            File.CreateText(testFile);

            //wait for the file event to trigger
            Thread.Sleep(500);

            Assert.AreEqual(testFile, log.LogFilePath);
        }
    }
}
