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
        public static void LogErrorToFile(Exception ex, string error = "", bool isLocalError = false)
        { 
            string hostName = Dns.GetHostName();
             
            IPAddress[] a = Dns.GetHostEntry(hostName).AddressList;
             
            string errorMessage = $"****************************************************************************************************************\n ";
            string ip = a[0].ToString();
            errorMessage += "IP Address:-" + ip;
            errorMessage += "\n Operating System: " + Environment.OSVersion;
            errorMessage += "\n 64-bit OS: " + Environment.Is64BitOperatingSystem;
            errorMessage += "\n Machine Name: " + Environment.MachineName;
            errorMessage += "\n System Directory: " + Environment.SystemDirectory;
            errorMessage += "\n User Name: " + Environment.UserName;
            if (ex != null)
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
               
                File.AppendAllText(filePath, errorMessage);
            }
            catch (Exception fileEx)
            {
                Console.WriteLine($"Failed to write to log file: {fileEx.Message}");
            }
          
            try
            {
                if (!isLocalError && ex !=null)
                    SendLogToApi(ip, ex.Message, ex.StackTrace, error);
            }
            catch
            {
                
            }
        }

        private const int TimeoutSeconds = 5;

      
        public static void SendLogToApi(string ip, string errorMessage, string stackTrace, string extra)
        {
            _ = Task.Run(() => SendLogToApiAsync(ip, errorMessage, stackTrace, extra, CancellationToken.None));
        }

        
        public static async Task SendLogToApiAsync(
            string ip,
            string errorMessage,
            string stackTrace,
            string extra,
            CancellationToken ct)
        {
            try
            { 
                var payload = BuildPayload(ip, errorMessage, stackTrace, extra); 
                await new ApiClient().PostRequestAsync<string>(
                    "api/ClientLogs/SaveClientLogs",
                    payload
                );
            }
            catch (Exception ex)
            { 
                LogErrorToFile(ex, "Excepction During the SaveClientLogs", true);
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
                stackTrace,    
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