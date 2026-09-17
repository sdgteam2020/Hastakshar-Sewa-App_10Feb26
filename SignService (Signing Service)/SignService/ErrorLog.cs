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
            string ip = Service1.GetClientIpAddressSafe();
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
                string Appfolder = Path.Combine(path, "DGIS");
                Directory.CreateDirectory(Appfolder);
                string filePath = Path.Combine(Appfolder, "ErrorLog.txt");

                File.AppendAllText(filePath, errorMessage);
            }
            catch (Exception fileEx)
            {
                Console.WriteLine($"Failed to write to log file: {fileEx.Message}");
            }

            try
            {
                if (!isLocalError && ex != null)
                    SendLogToApi(ip, ex, error);
            }
            catch
            {

            }
        }

        private const int TimeoutSeconds = 5;


        public static void SendLogToApi(string ip, dynamic ex, string extra)
        {
            _ = Task.Run(() => SendLogToApiAsync(ip, ex, extra, CancellationToken.None));
        }


        public static async Task SendLogToApiAsync(
            string ip,
            dynamic ex1,
            string extra,
            CancellationToken ct)
        {
            try
            {
                var payload = BuildPayload(ip, ex1, extra);
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

        private static object BuildPayload(string ip, dynamic ex, string extra)
        {
            string appName = "HastaksharSewa";
            string appVersion = GetAppVersion();
            string errMsg = string.Empty;

            errMsg += $"\n Exception: {ex?.Message ?? "No exception message"}\n Stack Trace: {ex?.StackTrace ?? "No stack trace"} \n Extra: {extra}";
            errMsg += $"\n Operating System: {Environment.OSVersion}";
            errMsg += $"\n 64-bit OS: {Environment.Is64BitOperatingSystem}";
            errMsg += $"\n Machine Name: {Environment.MachineName}";
            errMsg += $"\n System Directory: {Environment.SystemDirectory}";
            errMsg += $"\n User Name: {Environment.UserName}";
            return new
            {
                ipAddress = ip,
                machineName = Environment.MachineName,
                errorMessage = errMsg,
                appName,
                appVersion                
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