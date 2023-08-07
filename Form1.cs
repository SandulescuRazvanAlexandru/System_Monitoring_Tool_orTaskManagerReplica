using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml;
using Newtonsoft.Json;
using System.Diagnostics.Eventing.Reader;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Globalization;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace TestProcese
{
    public partial class Form1 : Form
    {
        Timer timer;
        List<ProcessData> processes;
        long totalMemory;
        Dictionary<int, ulong> previousReadCounters;
        Dictionary<int, ulong> previousWriteCounters;
        int indexAC;
        bool exist;

        public class ProcessData
        {
            public DateTime Timestamp { get; set; }
            public Process Process { get; set; }
            public double CpuUsage { get; set; }
            public double RamUsage { get; set; }
            public ulong TotalDiskRead { get; set; }
            public ulong ReadDiskRate { get; set; }
            public ulong TotalDiskWrite { get; set; }
            public ulong WriteDiskRate { get; set; }
            public ulong TotalReadOperationCount { get; set; }
            public ulong TotalWriteOperationCount { get; set; }
            public double ReadOperationRate { get; set; }
            public double WriteOperationRate { get; set; }
            public double ProcessDuration { get; set; } 
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [DllImport("kernel32.dll")]
        private static extern bool GetProcessIoCounters(IntPtr ProcessHandle, out IO_COUNTERS IoCounters);

        public Form1()
        {
            InitializeComponent();

            processes = new List<ProcessData>();
            previousReadCounters = new Dictionary<int, ulong>();
            previousWriteCounters = new Dictionary<int, ulong>();
            indexAC = 0;
            exist = true; 

            timer = new Timer();
            timer.Interval = 5000;
            timer.Tick += new EventHandler(Timer_Tick);
        }

        private async void Timer_Tick(object sender, EventArgs e)
        {
            indexAC++;
            tbMes.Text += "\r\nSnapshot[" + indexAC.ToString() + "] Started Successfully";

            var currentProcesses = Process.GetProcesses();
            var cpuTimes = new Dictionary<int, TimeSpan>();
            var ioCounters = new Dictionary<int, IO_COUNTERS>();
            var initialReadCounters = new Dictionary<int, ulong>();
            var initialWriteCounters = new Dictionary<int, ulong>();

            using (var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem"))
            {
                foreach (var computerSystem in searcher.Get())
                {
                    totalMemory = Convert.ToInt64(computerSystem["TotalPhysicalMemory"]);
                }
            }

            foreach (var process in currentProcesses)
            {
                try
                {
                    cpuTimes[process.Id] = process.TotalProcessorTime;

                    if (GetProcessIoCounters(process.Handle, out var ioCounter))
                    {
                        ioCounters[process.Id] = ioCounter;
                        initialReadCounters[process.Id] = previousReadCounters.ContainsKey(process.Id) ? previousReadCounters[process.Id] : ioCounter.ReadTransferCount;
                        initialWriteCounters[process.Id] = previousWriteCounters.ContainsKey(process.Id) ? previousWriteCounters[process.Id] : ioCounter.WriteTransferCount;
                    }
                }
                catch (Exception)
                {
                    cpuTimes[process.Id] = TimeSpan.Zero;
                    ioCounters[process.Id] = new IO_COUNTERS();
                }
            }

            await Task.Delay(1000);

            foreach (var process in currentProcesses)
            {
                try
                {
                    var initialCpuTime = cpuTimes[process.Id];
                    var currentCpuTime = process.TotalProcessorTime;
                    var cpuUsage = (currentCpuTime.TotalMilliseconds - initialCpuTime.TotalMilliseconds) * 100 / (1000.0 * Environment.ProcessorCount);
                    var ramUsage = (double)process.WorkingSet64 * 100 / totalMemory;

                    if (GetProcessIoCounters(process.Handle, out var ioCounter))
                    {
                        var totalRead = ioCounter.ReadTransferCount;
                        var readRate = totalRead - initialReadCounters[process.Id];
                        var totalWrite = ioCounter.WriteTransferCount;
                        var writeRate = totalWrite - initialWriteCounters[process.Id];
                        var readOperationCount = ioCounter.ReadOperationCount;
                        var writeOperationCount = ioCounter.WriteOperationCount;
                        var readOperationRate = readOperationCount > 0 ? totalRead / (double)readOperationCount : 0;
                        var writeOperationRate = writeOperationCount > 0 ? totalWrite / (double)writeOperationCount : 0;

                        previousReadCounters[process.Id] = totalRead;
                        previousWriteCounters[process.Id] = totalWrite;

                        var ProcessDuration = (DateTime.Now - process.StartTime).TotalSeconds;

                        var processData = new ProcessData
                        {
                            Timestamp = DateTime.Now,
                            Process = process,
                            CpuUsage = cpuUsage,
                            RamUsage = ramUsage,
                            TotalDiskRead = totalRead,
                            ReadDiskRate = readRate,
                            TotalDiskWrite = totalWrite,
                            WriteDiskRate = writeRate,
                            TotalReadOperationCount = readOperationCount,
                            TotalWriteOperationCount = writeOperationCount,
                            ReadOperationRate = readOperationRate,
                            WriteOperationRate = writeOperationRate,
                            ProcessDuration = ProcessDuration
                        };
                        processes.Add(processData);
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }

            //===create process.txt===//
            using (StreamWriter writer = new StreamWriter("process.txt"))
            {
                foreach (var processData in processes)
                {
                    writer.WriteLine($"Time: {processData.Timestamp}, PID: {processData.Process.Id}, Name: {processData.Process.ProcessName}, CPU Usage: {processData.CpuUsage:F5}%, RAM Usage: {processData.RamUsage:F5}%, Total Disk Read: {processData.TotalDiskRead} bytes, Disk Read Rate: {processData.ReadDiskRate} bytes/sec, Total Read Operations: {processData.TotalReadOperationCount}, Read Operations Rate: {processData.ReadOperationRate:F5} bytes/operation, Total Disk Write: {processData.TotalDiskWrite} bytes, Disk Write Rate: {processData.WriteDiskRate} bytes/sec, Total Write Operations: {processData.TotalWriteOperationCount}, Write Operations Rate: {processData.WriteOperationRate:F5} bytes/operation, Process Duration: {processData.ProcessDuration:F5} seconds");
                }
            }
            processes.Clear();

            //===Get ip info===//
            tbMes.Text += "\r\n\tGet ip info started successfully";

            var startInfo = new ProcessStartInfo
            {
                FileName = "netstat",
                Arguments = "-ano", 
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var processGetipinfo = new Process { StartInfo = startInfo };
            processGetipinfo.Start();

            string output = processGetipinfo.StandardOutput.ReadToEnd();
            processGetipinfo.WaitForExit();

            // Write the output to a file
            File.WriteAllText("ActiveConnections[Snapshot " + indexAC.ToString() + "].txt", output);

            tbMes.Text += "\r\n\tGet ip info stopped successfully";

            //===Merge ip info with PID===//
            tbMes.Text += "\r\n\tMerge ip info with PID started successfully";

            string processesFilePath = "process.txt";
            string activeConnectionsFilePath = "ActiveConnections[Snapshot " + indexAC.ToString() + "].txt";

            // Read active connections into a dictionary for quick lookup
            var activeConnections = new Dictionary<int, List<string>>();
            var activeConnectionsLines = File.ReadLines(activeConnectionsFilePath).Skip(4);
            foreach (var line in activeConnectionsLines)
            {
                var parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    string protocol = parts[0];
                    string localAddress = parts[1];
                    string foreignAddress = parts[2];
                    string state = parts.Length >= 5 ? parts[3] : "N/A"; // Use "N/A" for state in UDP connections
                    int pid = int.Parse(parts[parts.Length - 1]); // PID is always the last part

                    var connectionInfo = $"[Connection: Protocol: {protocol}, Local Address: {localAddress}, Foreign Address: {foreignAddress}, State: {state}";
                    if (!activeConnections.ContainsKey(pid))
                    {
                        activeConnections[pid] = new List<string> { connectionInfo };
                    }
                    else
                    {
                        if (!activeConnections[pid].Contains(connectionInfo))
                        {
                            activeConnections[pid].Add(connectionInfo);
                        }
                    }
                }
            }

            // Write the contents of activeConnections to a temporary file
            var activeConnectionsStr = JsonConvert.SerializeObject(activeConnections, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText("tempActiveConnections[Snapshot " + indexAC.ToString() + "].txt", activeConnectionsStr);
            
            // Read processes file, modify lines, and write them back to the file
            var lines = new List<string>(File.ReadAllLines(processesFilePath));
            var writtenConnections = new Dictionary<int, bool>();
            for (int i = 0; i < lines.Count; i++)
            {
                var parts = lines[i].Split(new string[] { ", " }, StringSplitOptions.None);
                if (parts.Length >= 3 && parts[1].StartsWith("PID:") && int.TryParse(parts[1].Substring(4), out int pid))
                {
                    if (!writtenConnections.ContainsKey(pid) && activeConnections.TryGetValue(pid, out List<string> connectionInfoList))
                    {
                        var formattedConnectionsList = connectionInfoList
                            .Select((connection, index) => $"[Connection {index + 1}: {connection}]");
                        string formattedConnections = $"[Active Connections: {string.Join(", ", formattedConnectionsList)}]";
                        lines[i] += $", {formattedConnections}";
                        writtenConnections[pid] = true;
                    }
                    else
                    {
                        lines[i] += ", no ip info available";
                    }
                }
            }

            // Write the modified lines back to the processes file
            File.WriteAllLines(processesFilePath, lines);

            tbMes.Text += "\r\n\tMerge ip info with PID stopped successfully";

            //===Get GPU Usage Info===//
            var gpuInfoStartInfo = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-compute-apps=pid,used_gpu_memory --format=csv,noheader,nounits",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var gpuInfoProcess = new Process { StartInfo = gpuInfoStartInfo };
            gpuInfoProcess.Start();

            string gpuInfoOutput = gpuInfoProcess.StandardOutput.ReadToEnd();
            gpuInfoProcess.WaitForExit();

            // Parse GPU usage info and store it in a dictionary
            var gpuUsages = new Dictionary<int, double>();
            var gpuInfoLines = gpuInfoOutput.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in gpuInfoLines)
            {
                var parts = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && int.TryParse(parts[0], out int pid) && double.TryParse(parts[1], out double gpuUsage))
                {
                    gpuUsages[pid] = gpuUsage;
                }
            }

            // Merge GPU usage info with process info
            var processLines = File.ReadAllLines("process.txt");
            using (var writer = new StreamWriter("process.txt"))
            {
                foreach (var line in processLines)
                {
                    var parts = line.Split(new string[] { ", " }, StringSplitOptions.None);
                    if (parts.Length >= 3 && parts[1].StartsWith("PID:") && int.TryParse(parts[1].Substring(4), out int pid))
                    {
                        if (gpuUsages.TryGetValue(pid, out var gpuUsage))
                        {
                            writer.WriteLine(line + $", GPU Usage: {gpuUsage} MiB");
                        }
                        else
                        {
                            writer.WriteLine(line + ", GPU Usage: GPU is not used");
                        }
                    }
                    else
                    {
                        writer.WriteLine(line);
                    }
                }
            }

            //===Insert ip country===//
            tbMes.Text += "\r\n\tip country started successfully";

            string[] liness = File.ReadAllLines("process.txt");
            StringBuilder outputt = new StringBuilder();

            foreach (string line in liness)
            {
                string newLine = line;

                // Handle Local Address 
                int localAddressIndex = newLine.IndexOf("Local Address: ");
                while (localAddressIndex != -1)
                {
                    int start = localAddressIndex + "Local Address: ".Length;
                    int end = newLine.IndexOf(",", start);
                    if (end == -1)
                    {
                        end = newLine.IndexOf("]", start);
                    }
                    string localAddress = newLine.Substring(start, end - start).Split(':')[0].Trim();
                    string addressType = CategorizeLocalAddress(localAddress);

                    // Insert the address type information into the line
                    newLine = newLine.Insert(end, " - " + addressType);

                    localAddressIndex = newLine.IndexOf("Local Address: ", end);
                }

                // Handle Foreign Address
                int foreignAddressIndex = newLine.IndexOf("Foreign Address: ");
                while (foreignAddressIndex != -1)
                {
                    int start = foreignAddressIndex + "Foreign Address: ".Length;
                    int end = newLine.IndexOf(",", start);
                    if (end == -1)
                    {
                        end = newLine.IndexOf("]", start);
                    }
                    string foreignAddress = newLine.Substring(start, end - start);
                    string[] addressParts = foreignAddress.Split(':');

                    if (addressParts.Length == 2 && !string.IsNullOrWhiteSpace(addressParts[0]) && addressParts[0] != "[::]" && addressParts[0] != "0.0.0.0" && addressParts[0] != "*")
                    {
                        string country = GetLocation(addressParts[0].Trim());
                        // Insert the country information into the line
                        newLine = newLine.Insert(end, " - Country: " + country);
                    }

                    foreignAddressIndex = newLine.IndexOf("Foreign Address: ", end);
                }

                //// Replacing [::1]:any_number - Unknown Address Type with [::1]:any_number - Loopback Address
                //var loopbackRegex = new Regex(@"Local Address: \[\:\:1\]\:\d+ - Unknown Address Type");
                //newLine = loopbackRegex.Replace(newLine, m => m.Value.Replace("Unknown Address Type", "Loopback Address"));

                //// Replacing [::]:any_number - Unknown Address Type with [::]:any_number - Unspecified/Wildcard Address
                //var unspecifiedRegex = new Regex(@"Local Address: \[\:\:\]\:\d+ - Unknown Address Type");
                //newLine = unspecifiedRegex.Replace(newLine, m => m.Value.Replace("Unknown Address Type", "Unspecified/Wildcard Address"));

                //// Replacing [any_characters::any_characters:any_characters:any_characters:any_characters%any_number]:any_number - Unknown Address Type
                //// with [any_characters::any_characters:any_characters:any_characters:any_characters%any_number]:any_number - Link-Local Address 
                //var linkLocalRegex = new Regex(@"Local Address: \[[a-fA-F0-9\:]+%?\d*\]\:\d+ - Unknown Address Type");
                //newLine = linkLocalRegex.Replace(newLine, m => m.Value.Replace("Unknown Address Type", "Link-Local Address"));

                outputt.AppendLine(newLine);
            }

            // Write the modified lines to the file
            File.WriteAllText("process.txt", outputt.ToString());
            tbMes.Text += "\r\n\tip country stopped successfully";

            //===processes.txt===//
            if (File.Exists("processes.txt") && exist)
            {
                File.Delete("processes.txt");
                exist = false;
            }
            File.AppendAllText("processes.txt", File.ReadAllText("process.txt"));

            tbMes.Text += "\r\nSnapshot[" + indexAC.ToString() + "] Stopped Successfully";
        }

        public static string CategorizeLocalAddress(string ip)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(ip, out ipAddress))
            {
                if (IPAddress.IsLoopback(ipAddress))
                {
                    return "Loopback Address";
                }
                else if (IsLocal(ipAddress))
                {
                    return "Simple Local Address";
                }
                else if (ipAddress.IsIPv6LinkLocal)
                {
                    return "Link-Local Address";
                }
                else if (ipAddress.Equals(IPAddress.IPv6Any) || ipAddress.Equals(IPAddress.Any))
                {
                    return "Unspecified/Wildcard Address";
                }
            }
            return "Unknown Address Type";
        }


        public static bool IsLocal(IPAddress ipAddress)
        {
            // Check if the IP address is in the private IP address range
            byte[] bytes = ipAddress.GetAddressBytes();
            switch (bytes[0])
            {
                case 10:
                    return true;
                case 172:
                    return bytes[1] < 32 && bytes[1] >= 16;
                case 192:
                    return bytes[1] == 168;
                default:
                    return false;
            }
        }

        private void btnStartSnapshot_Click(object sender, EventArgs e)
        {
            timer.Start();
            Timer_Tick(sender, e);
        }

        private void btnStopSnapshot_Click(object sender, EventArgs e)
        {
            timer.Stop();
        }

        private void btnGPU_Click(object sender, EventArgs e)
        {
            // Get GPU usage info
            var gpuInfoStartInfo = new ProcessStartInfo
            {
                FileName = "nvidia-smi",
                Arguments = "--query-compute-apps=pid,used_gpu_memory --format=csv,noheader,nounits",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            var gpuInfoProcess = new Process { StartInfo = gpuInfoStartInfo };
            gpuInfoProcess.Start();

            string gpuInfoOutput = gpuInfoProcess.StandardOutput.ReadToEnd();
            gpuInfoProcess.WaitForExit();

            // Parse GPU usage info and store it in a dictionary
            var gpuUsages = new Dictionary<int, double>();
            var gpuInfoLines = gpuInfoOutput.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in gpuInfoLines)
            {
                var parts = line.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && int.TryParse(parts[0], out int pid) && double.TryParse(parts[1], out double gpuUsage))
                {
                    gpuUsages[pid] = gpuUsage;
                }
            }

            var sb = new StringBuilder();
            foreach (var kvp in gpuUsages)
            {
                sb.AppendLine($"PID: {kvp.Key}, GPU Usage: {kvp.Value} MiB");
            }
            File.WriteAllText("GPUusage.txt", sb.ToString());
        }

        public static string GetLocation(string ip)
        {
            IPAddress ipAddress;
            if (IPAddress.TryParse(ip, out ipAddress))
            {
                if (IPAddress.IsLoopback(ipAddress))
                {
                    return "Loopback address";
                }
                else if (IsLocal(ipAddress))
                {
                    return "Local address";
                }
            }

            string url = "https://api.ipgeolocation.io/ipgeo?apiKey=1a9eeb5c9db44e60ade9bf3ba8a6b069&ip=" + ip;
      
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Timeout = 3500; // Timeout after 3.5 seconds
                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                string responseText = reader.ReadToEnd();

                JObject json = JObject.Parse(responseText);

                return json["country_name"].ToString();
            }
            catch (Exception ex)
            {
                File.WriteAllText("eroareeee.txt", ex.Message);
            }
            return null;
        }

        private void btnIPcountry_Click(object sender, EventArgs e)
        {
            string[] lines = File.ReadAllLines("processes.txt");
            StringBuilder output = new StringBuilder();

            foreach (string line in lines)
            {
                string newLine = line;
                int foreignAddressIndex = newLine.IndexOf("Foreign Address: ");
                while (foreignAddressIndex != -1)
                {
                    int start = foreignAddressIndex + "Foreign Address: ".Length;
                    int end = newLine.IndexOf(",", start);
                    if (end == -1)
                    {
                        end = newLine.IndexOf("]", start);
                    }
                    string foreignAddress = newLine.Substring(start, end - start);
                    string[] addressParts = foreignAddress.Split(':');

                    if (addressParts.Length == 2 && !string.IsNullOrWhiteSpace(addressParts[0]) && addressParts[0] != "[::]" && addressParts[0] != "0.0.0.0" && addressParts[0] != "*")
                    {
                        string country = GetLocation(addressParts[0].Trim());
                        // Insert the country information into the line
                        newLine = newLine.Insert(end, " - Country: " + country);
                    }

                    foreignAddressIndex = newLine.IndexOf("Foreign Address: ", end);
                }
                output.AppendLine(newLine);
            }

            // Write the modified lines to the file
            File.WriteAllText("processes.txt", output.ToString());
        }

        private void btnLookForRansomware_Click(object sender, EventArgs e)
        {
            tbMes.Text += "\r\nLooking for Ransomware Started Successfully";
            FilterProcesses();
            AnalyzeAndWriteDurationsAndTimeIntervals();
            tbMes.Text += "\r\nLooking for Ransomware Stopped Successfully";
        }

        public void FilterProcesses()
        {
            string sourceFilePath = "processes.txt";  
            string targetFilePath = "ransomware.txt"; 
            string line;
            string targetProcessName = "Name: Ransomware";
            //string targetProcessName = "Name: IGCCTray";

            using (StreamReader sr = new StreamReader(sourceFilePath))
            {
                using (StreamWriter sw = new StreamWriter(targetFilePath))
                {
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains(targetProcessName))
                        {
                            sw.WriteLine(line);
                        }
                    }
                }
            }
        }

        public void AnalyzeAndWriteDurationsAndTimeIntervals()
        {
            string filePath = "ransomware.txt";   // Replace with your actual path
            string[] lines = File.ReadAllLines(filePath);

            List<int> timeIntervals = new List<int>();
            List<int> durationIntervals = new List<int>();
            List<int> diskUsageLevels = new List<int>();
            List<int> indexLevels = new List<int>();
            List<int> ipLevels = new List<int>();  

            foreach (string line in lines)
            {
                // Analyze timestamp and get the interval/level
                string timeString = line.Split(',')[0].Replace("Time: ", "").Trim();  // Extract time string

                try
                {
                    DateTime timestamp = DateTime.ParseExact(timeString, "dd-MMM-yy h:mm:ss tt", CultureInfo.InvariantCulture);
                    int hour = timestamp.Hour;
                    int timeLevel = GetTimeIntervalLevel(hour);
                    timeIntervals.Add(timeLevel);
                }
                catch (FormatException ex)
                {
                    File.WriteAllText("alta_eroare.txt", ex.Message);
                }

                // Analyze Process Duration and get the interval/level
                
                try
                {
                    string durationValueString = "";
                    string durationLabel = "Process Duration: ";
                    int durationStart = line.IndexOf(durationLabel);

                    if (durationStart >= 0)
                    {
                        durationStart += durationLabel.Length;
                        int durationEnd = line.IndexOf(" ", durationStart);
                        durationValueString = line.Substring(durationStart, durationEnd - durationStart);
                    }

                    double durationInSeconds = double.Parse(durationValueString);
                    int durationLevel = GetDurationIntervalLevel(durationInSeconds);
                    durationIntervals.Add(durationLevel);

                }
                catch (FormatException ex)
                {
                    File.WriteAllText("alta_eroare_parse.txt", ex.Message);
                }

                // Analyze Total Disk Read and Total Disk Write and get the level
                string readLabel = "Total Disk Read: ";
                int readStart = line.IndexOf(readLabel);
                string readValueString = "";
                if (readStart >= 0)
                {
                    readStart += readLabel.Length;
                    int readEnd = line.IndexOf(" ", readStart);
                    readValueString = line.Substring(readStart, readEnd - readStart);
                }

                string writeLabel = "Total Disk Write: ";
                int writeStart = line.IndexOf(writeLabel);
                string writeValueString = "";
                if (writeStart >= 0)
                {
                    writeStart += writeLabel.Length;
                    int writeEnd = line.IndexOf(" ", writeStart);
                    writeValueString = line.Substring(writeStart, writeEnd - writeStart);
                }

                // Convert values from bytes to gigabytes
                double totalDiskReadInGB = double.Parse(readValueString) / (1024 * 1024 * 1024);
                double totalDiskWriteInGB = double.Parse(writeValueString) / (1024 * 1024 * 1024);

                // Compute the total disk usage and its level
                double totalDiskUsageInGB = totalDiskReadInGB + totalDiskWriteInGB;
                int diskUsageLevel = GetDiskUsageLevel(totalDiskUsageInGB);

                diskUsageLevels.Add(diskUsageLevel);

                // Extract CPU Usage
                string cpuLabel = "CPU Usage: ";
                int cpuStart = line.IndexOf(cpuLabel);
                string cpuValueString = "";
                if (cpuStart >= 0)
                {
                    cpuStart += cpuLabel.Length;
                    int cpuEnd = line.IndexOf("%", cpuStart);
                    cpuValueString = line.Substring(cpuStart, cpuEnd - cpuStart);
                }

                // Extract RAM Usage
                string ramLabel = "RAM Usage: ";
                int ramStart = line.IndexOf(ramLabel);
                string ramValueString = "";
                if (ramStart >= 0)
                {
                    ramStart += ramLabel.Length;
                    int ramEnd = line.IndexOf("%", ramStart);
                    ramValueString = line.Substring(ramStart, ramEnd - ramStart);
                }

                // Parse the CPU and RAM usage percentages
                double cpuUsage = double.Parse(cpuValueString);
                double ramUsage = double.Parse(ramValueString);

                // Analyze Disk Read Rate and Disk Write Rate and get the level
                string readRateLabel = "Disk Read Rate: ";
                int readRateStart = line.IndexOf(readRateLabel);
                string readRateValueString = "";
                if (readRateStart >= 0)
                {
                    readRateStart += readRateLabel.Length;
                    int readRateEnd = line.IndexOf(" ", readRateStart);
                    readRateValueString = line.Substring(readRateStart, readRateEnd - readRateStart);
                }

                string writeRateLabel = "Disk Write Rate: ";
                int writeRateStart = line.IndexOf(writeRateLabel);
                string writeRateValueString = "";
                if (writeRateStart >= 0)
                {
                    writeRateStart += writeRateLabel.Length;
                    int writeRateEnd = line.IndexOf(" ", writeRateStart);
                    writeRateValueString = line.Substring(writeRateStart, writeRateEnd - writeRateStart);
                }

                double diskReadRateInMB = double.Parse(readRateValueString);
                double diskWriteRateInMB = double.Parse(writeRateValueString);

                double totalDiskUsageRateInMB = diskReadRateInMB + diskWriteRateInMB;
                double diskUsageScaled = ScaleValue(totalDiskUsageRateInMB, 0, 500, 0, 100);

                double index = 0.3 * cpuUsage + 0.1 * ramUsage + 0.3 * diskUsageScaled + 0.15 * diskUsageScaled + 0.15 * cpuUsage;
                int indexLevel = GetUsageLevel(index);
                indexLevels.Add(indexLevel);

                // ip part
                int ipLevel = 0;
                if (line.Contains("Country"))
                {
                    ipLevel = 1;
                }
                else if (line.Contains("Simple Local Address") || line.Contains("Link-Local Address"))
                {
                    ipLevel = 2;
                }
                else if (line.Contains("Loopback Address") || line.Contains("Unspecified/Wildcard Address"))
                {
                    ipLevel = 3;
                }
                else if (line.Contains("no ip info available"))
                {
                    ipLevel = 4;
                }
                ipLevels.Add(ipLevel);
            }

            using (StreamWriter sw = new StreamWriter(filePath, false)) 
            {
                foreach (string line in lines)
                {
                    sw.WriteLine(line);
                }

                for (int i = 0; i < timeIntervals.Count; i++)
                {
                    sw.WriteLine("-1" + " 1:" + timeIntervals[i] + " 2:" + durationIntervals[i] + " 3:" + diskUsageLevels[i] + " 4:" + indexLevels[i] + " 5:" + ipLevels[i]);  
                }
            }
        }

        public int GetTimeIntervalLevel(int hour)
        {
            if (hour >= 2 && hour < 6)
                return 1;
            else if (hour >= 0 && hour < 2)
                return 2;
            else if (hour >= 22)
                return 3;
            else if (hour >= 6 && hour < 8)
                return 4;
            else if (hour >= 20 && hour < 22)
                return 5;
            else 
                return 6;
        }

        public int GetDurationIntervalLevel(double durationInSeconds)
        {
            // Convert duration to minutes for easy comparison
            double durationInMinutes = durationInSeconds / 60;

            if ((durationInMinutes >= 0 && durationInMinutes <= 2) || (durationInMinutes > 480))
                return 1;
            else if ((durationInMinutes > 2 && durationInMinutes <= 10) || (durationInMinutes > 120 && durationInMinutes <= 480))
                return 2;
            else // 10 minutes - 2 hours
                return 3;
        }

        public int GetDiskUsageLevel(double totalDiskUsageInGB)
        {
            if (totalDiskUsageInGB > 100)
                return 1;
            else if (totalDiskUsageInGB > 80)
                return 2;
            else if (totalDiskUsageInGB > 50)
                return 3;
            else if (totalDiskUsageInGB > 10)
                return 4;
            else if (totalDiskUsageInGB > 1)
                return 5;
            else if (totalDiskUsageInGB > 0.1) 
                return 6;
            else
                return 7;
        }

        public int GetUsageLevel(double usage)
        {
            if (usage >= 90)
                return 1;
            else if (usage >= 70)
                return 2;
            else if (usage >= 50)
                return 3;
            else if (usage >= 25)
                return 4;
            else if (usage >= 15)
                return 5;
            else if (usage >= 10)
                return 6;
            else
                return 7;
        }

        public double ScaleValue(double value, double originalMin, double originalMax, double targetMin, double targetMax)
        {
            double scale = (targetMax - targetMin) / (originalMax - originalMin);
            return targetMin + (value - originalMin) * scale;
        }
    }
}

