using SignService.HttpClients;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SignService
{
    public class ErrorLog 
    {       
        public static void LogErrorToFile(Exception ex,string error="")
        {

           // IPAddress[] a = Dns.GetHostByName(Dns.GetHostName()).AddressList;
            string hostName = Dns.GetHostName();

            // Get the list of IP addresses associated with the hostname
            IPAddress[] a = Dns.GetHostEntry(hostName).AddressList;

            //string filePath = System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + @"\ErrorLog.txt";// Path to the error log file
            //string filePath = "ErrorLog.txt"; // Path to the error log file
            string errorMessage = $"****************************************************************************************************************\n ";
            string ip = a[0].ToString();
            errorMessage += "IP Address:-"+ ip;
            errorMessage += "\n Operating System: " + Environment.OSVersion;
            errorMessage += "\n 64-bit OS: " + Environment.Is64BitOperatingSystem;
            errorMessage += "\n Machine Name: " + Environment.MachineName;
            errorMessage += "\n System Directory: " + Environment.SystemDirectory;
            errorMessage += "\n User Name: " + Environment.UserName;
            if(ex!=null)
            errorMessage += $"[{DateTime.Now}] \n Exception: {ex.Message}\n Stack Trace: {ex.StackTrace}";
            else
            errorMessage += $"[{DateTime.Now}] \n Error: {error}";
            errorMessage += "\n*********************************************************************************************************************\n";
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string Appfolder = System.IO.Path.Combine(path, "DGIS");
                Directory.CreateDirectory(Appfolder);
                string filePath = System.IO.Path.Combine(Appfolder, "ErrorLog.txt");
                // Append the error message to the file
                File.AppendAllText(filePath, errorMessage);
            }
            catch (Exception fileEx)
            {
                Console.WriteLine($"Failed to write to log file: {fileEx.Message}");
            }
            // ---- 2) Send to API (best-effort) ----
            try
            {
                SendLogToApi(ip, ex.Message, ex.StackTrace, error);
            }
            catch
            {
                // swallow errors (do not crash)
            }
        }

        private const int TimeoutSeconds = 5;

        /// <summary>
        /// Non-async callers can call this. It will not crash your app.
        /// </summary>
        public static void SendLogToApi(string ip, string errorMessage, string stackTrace, string extra)
        {
            _ = Task.Run(() => SendLogToApiAsync(ip, errorMessage, stackTrace, extra, CancellationToken.None));
        }

        /// <summary>
        /// Preferred async method.
        /// </summary>
        public static async Task SendLogToApiAsync(
            string ip,
            string errorMessage,
            string stackTrace,
            string extra,
            CancellationToken ct)
        {
            try
            {
                
                // Build payload (same fields as your JSON builder)
                var payload = BuildPayload(ip, errorMessage, stackTrace, extra);

                // ✅ JWT auto-attached + auto-refresh inside DeviceJwtHttpClient
                await new ApiClient().PostRequestAsync<string>(
                    "api/ClientLogs/SaveClientLogs",
                    payload
                );
            }
            catch (Exception ex)
            {
                // Never throw from logging; log locally only
                ErrorLog.LogErrorToFile(ex);
            }
        }

        private static object BuildPayload(string ip, string errMsg, string stackTrace, string extra)
        {
            string appName = "HastaksharSewa";
            string appVersion = GetAppVersion();

            return new
            {
                ipAddress = ip,
                machineName = Environment.MachineName,
                userName = Environment.UserName,
                operatingSystem = Environment.OSVersion.ToString(),
                is64Bit = Environment.Is64BitOperatingSystem,
                systemDirectory = Environment.SystemDirectory,
                appName,
                appVersion,
                errorMessage = errMsg,
                stackTrace,   // can be null
                extra = string.IsNullOrWhiteSpace(extra) ? null : extra
            };
        }

        private static string GetAppVersion()
        {
            try
            {
                var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                var v = asm.GetName().Version;
                return v != null ? v.ToString() : "NA";
            }
            catch { return "NA"; }
        }

    }
}