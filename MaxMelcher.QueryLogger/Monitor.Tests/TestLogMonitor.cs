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
            DirectoryMonitor directory = new DirectoryMonitor();
            directory.Start();

            directory.MonitorTask.Wait();

            Assert.IsFalse(string.IsNullOrEmpty(directory.LogFilePath));
        }

        [TestMethod]
        public void TheLogFilePathChangesAfterANewFileIsCreated()
        {
            DirectoryMonitor directory = new DirectoryMonitor();
            directory.Start();

            directory.MonitorTask.Wait();
            Assert.IsFalse(string.IsNullOrEmpty(directory.LogFilePath));

            FileInfo file = new FileInfo(directory.LogFilePath);

            string testFile = Path.Combine(file.Directory.FullName, "test.log");

            //delete existing, old files
            if (File.Exists(testFile))
            {
                File.Delete(testFile);                
            }

            File.CreateText(testFile);

            //wait for the file event to trigger
            Thread.Sleep(1200);

            Assert.AreEqual(testFile, directory.LogFilePath);
        }
    }
}
