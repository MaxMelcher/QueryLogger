﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using Microsoft.Win32;

namespace MaxMelcher.QueryLogger.Monitor
{
    class SPUtility
    {
        static IList<TraceSeverity> severities = new List<TraceSeverity>((IEnumerable<TraceSeverity>)Enum.GetValues(typeof(TraceSeverity)));

        public static SPVersion SPVersion
        {
            get
            {
                try
                {
                    // Check for SP2013
                    var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\15.0\WSS");
                    if (key != null)
                    {
                        key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\15.0");
                        return SPVersion.SP2013;
                    }

                }
                catch (SecurityException) { }
                return SPVersion.Unknown;
            }
        }

        public static bool IsWSSInstalled
        {
            get
            {
                try
                {
                    RegistryKey key = GetWSSRegistryKey();
                    if (key != null)
                    {
                        object val = key.GetValue("SharePoint");
                        if (val != null && val.Equals("Installed"))
                            return true;
                    }
                }
                catch (SecurityException) { }
                return false;
            }
        }

        public static bool IsMOSSInstalled
        {
            get
            {
                try
                {
                    using (RegistryKey key = GetMOSSRegistryKey())
                        if (key != null)
                        {
                            string versionStr = key.GetValue("BuildVersion") as string;
                            if (versionStr != null)
                            {
                                Version buildVersion = new Version(versionStr);
                                if (buildVersion.Major == 12 || buildVersion.Major == 14 || buildVersion.Major == 15)
                                    return true;
                            }
                        }
                }
                catch (SecurityException) { }
                return false;
            }
        }

        public static string LatestLogFile
        {
            get
            {
                string lastAccessedFile = null;
                if (IsWSSInstalled)
                    lastAccessedFile = GetLastAccessedFile(GetLogsLocation());

                return lastAccessedFile;
            }
        }
        public static string WSSInstallPath
        {
            get
            {
                string installPath = String.Empty;
                try
                {
                    using (RegistryKey key = GetWSSRegistryKey())
                        if (key != null)
                            installPath = key.GetValue("Location").ToString();
                }
                catch (SecurityException) { }
                return installPath;
            }
        }

        public static ICollection TraceSeverities
        {
            get
            {
                return new ReadOnlyCollection<TraceSeverity>(severities);
            }
        }

        public static string GetLogsLocation()
        {
            string logLocation = String.Empty;
            if (IsWSSInstalled)
            {
                logLocation = GetSPDiagnosticsLogLocation();
                if (logLocation == String.Empty)
                    logLocation = GetStandardLogLocation();
            }

            logLocation = Environment.ExpandEnvironmentVariables(logLocation);

            return logLocation;
        }

        public static int GetSeverity(string level)
        {
            try
            {
                var severity = (TraceSeverity)Enum.Parse(typeof(TraceSeverity), level, true);
                return (int)severity;
            }
            catch (ArgumentException)
            {
                return 0;
            }
        }

        public static string GetLastAccessedFile(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                var dirInfo = new DirectoryInfo(folderPath);
                var file = dirInfo.GetFiles().OrderByDescending(f => f.LastWriteTime).FirstOrDefault();
                if (file != null)
                    return file.FullName;
            }
            return null;
        }

        

        static RegistryKey GetMOSSRegistryKey()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office Server\12.0");
            if (key == null)
                key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office Server\14.0");
            else if (key == null)
                key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Office Server\15.0");
            return key;
        }

        static RegistryKey GetWSSRegistryKey()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\15.0");
            if (key == null)
                key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\14.0");
            else if (key == null)
                key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\12.0");
            return key;
        }

        private static string GetStandardLogLocation()
        {
            string logLocation = WSSInstallPath;
            if (logLocation != String.Empty)
                logLocation = Path.Combine(logLocation, "logs");

            return logLocation;
        }

        private static string GetSPDiagnosticsLogLocation()
        {
            string logLocation = String.Empty;
            Type diagSvcType = null;
            if (SPUtility.SPVersion == SPVersion.SP2013)
                diagSvcType = Type.GetType("Microsoft.SharePoint.Administration.SPDiagnosticsService, Microsoft.SharePoint, Version=15.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c");

            if (diagSvcType != null)
            {
                PropertyInfo propLocalDiagSvc = diagSvcType.GetProperty("Local", BindingFlags.Public | BindingFlags.Static);
                object localDiagSvc = propLocalDiagSvc.GetValue(null, null);
                PropertyInfo property = localDiagSvc.GetType().GetProperty("LogLocation");
                logLocation = (string)property.GetValue(localDiagSvc, null);
            }

            return logLocation;
        }
    }
}