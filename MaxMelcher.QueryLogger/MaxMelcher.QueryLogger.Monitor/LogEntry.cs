using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxMelcher.QueryLogger.Monitor
{
    /// <copyright>
    /// This class is copied from the SharePoint LogViewer: http://sharepointlogviewer.codeplex.com/SourceControl/latest#SharePointLogViewer/SPUtility.cs
    /// Copyright (c) 2010 Overroot Inc.
    /// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
    /// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
    /// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
    /// </copyright>
    public class LogEntry
    {
        public string Timestamp { get; set; }
        public string Process { get; set; }
        public string Thread { get; set; }
        public string Area { get; set; }
        public string Category { get; set; }
        public string EventID { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Correlation { get; set; }

        public static LogEntry Parse(string line)
        {
            string[] fields = line.Split('\t');
            if (fields.Length > 8)
            {
                string messageOneLine = fields[7].Trim();
                string messageMultiLine = messageOneLine.Replace("    at ", "\r\n    at ");

                var entry = new LogEntry()
                {
                    Timestamp = fields[0].Trim(),
                    Process = fields[1].Trim(),
                    Thread = fields[2].Trim(),
                    Area = fields[3].Trim(),
                    Category = fields[4].Trim(),
                    EventID = fields[5].Trim(),
                    Level = fields[6].Trim(),
                    Message = messageMultiLine,
                    Correlation = fields[8].Trim()
                };
                return entry;
            }
            else
                return null;
        }
    }
}
