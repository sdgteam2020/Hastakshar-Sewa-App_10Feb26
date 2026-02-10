
using DGISAPP.SessionManagement;
using SignService;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Controls;
using WinniesMessageBox;

namespace DGISApp
{
    public partial class Home : UserControl
    {
        private static readonly string VersionUrl = ConfigurationManager.AppSettings["VersionUrl"].ToString() + "version.txt";
        private static readonly string UpdatePackageUrl = ConfigurationManager.AppSettings["VersionUrl"].ToString() + "/DGISApp.zip";
        private static readonly HttpClient httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(5) // Adjust timeout as needed
        };
        public  Home()
        {
            InitializeComponent();

            if(!GlobalVariables.IsStatus)
            Checkupdate();

            //Checking Self Api , Host File , Port , Trusted Certificate and Host File
            CheckUrlStatusAsync();
            // ImageData imageData = null;
            //using (StreamReader sr = new StreamReader(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + @"\DigitalSign.png"))
            //{
            //    imageData = ImageDataFactory.Create(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DigitalSign.png");
            //}
           
        }
      
        private async void CheckUrlStatusAsync()
        {
            await Task.Delay(3000);

            string url = ConfigurationManager.AppSettings["UrlApi"].ToString()+"/HasInternetConnectionAsyncTest";
            //Check Self Api Running Or Not
            bool isRunning = await IsUrlRunningAsync(url);
            if (!isRunning)
            {
                var failures = new List<string>();
                var steps = new List<(string message, bool success)>();

                // 1) Host check
                bool ishost = checkHostIsorNot();
                if (!ishost)
                {
                    string error = "dgisapp.army.mil is Not in Host File";
                    failures.Add(error);
                    steps.Add((error, false));
                    ErrorLog.LogErrorToFile(null, error);
                }
                else
                {
                    steps.Add(("dgisapp.army.mil is in Host File", true));
                }

                // 2) Port check
                bool isPortOpen = IsPortOpen(55102);
                if (!isPortOpen)
                {
                    string error = "Port 55102 is CLOSED.";
                    failures.Add(error);
                    steps.Add((error, false));
                    ErrorLog.LogErrorToFile(null, error);
                }
                else
                {
                    steps.Add(("Port 55102 is Open", true));
                }

                // 3) Cert check
                bool iscert = ISCertTrusted("dgisapp.army.mil");
                if (!iscert)
                {
                    string error = "SSL Certificate is not in trusted store.";
                    failures.Add(error);
                    steps.Add((error, false));
                    ErrorLog.LogErrorToFile(null, error);
                }
                else
                {
                    steps.Add(("SSL Certificate is in trusted store", true));
                }

                // ✅ Show popup ONLY if any error happened
                if (failures.Count > 0)
                {
                    var popup = new PopupWin();
                    popup.Show(); // or ShowDialog() if you want modal (see note below)

                    // Show all steps (optional: show only failed ones if you want)
                    foreach (var s in steps)
                        await popup.RunProcessStep(s.message, s.success);
                }
            }
        }
        static bool ISCertTrusted(string Issuer)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C certutil -store root | findstr {Issuer}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // If the output contains the port number, it means the port is open
                return !string.IsNullOrEmpty(output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
        static bool IsPortOpen(int port)
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.Arguments = $"/C netstat -ano | findstr :{port}";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                // If the output contains the port number, it means the port is open
                return !string.IsNullOrEmpty(output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
        public bool checkHostIsorNot()
        {
            try
            {
                string hostsFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");

                // Read all lines from the hosts file
                var lines = File.ReadAllLines(hostsFilePath);
                
                // Filter out the line to be removed
                string entryToCheck = $"{ConfigurationManager.AppSettings["localhostIp"]} {ConfigurationManager.AppSettings["cerificateIssuer"]}";
                var updatedLines = new List<string>();
                int count = 0;
                foreach (var line in lines)
                {
                    if (line.Trim().Equals(entryToCheck, StringComparison.OrdinalIgnoreCase))
                    {
                        count = 1;
                    }
                }

                if (count == 0)
                {
                    return false;
                }
                return true;

            }
            catch (Exception)
            {
                return false ;

            }
        }
        private static async Task<bool> IsUrlRunningAsync(string url)
        {
            try
            {
                    var response = await httpClient.GetAsync(url);
                    return response.IsSuccessStatusCode;
              
            }
            catch (HttpRequestException)
            {
                return false; // Handle network failures
            }
            catch (TaskCanceledException)
            {
                return false; // Handle timeout
            }
        }
     
        public async Task Checkupdate()
        {
            Service1 service1 = new Service1();
            if (!await service1.HasInternetConnectionAsyncTest())
            {
            }
            else
            {
                _ = GetUpdateAsync();
            }
        }
        private static async Task<string> GetLatestVersionAsync()
        {
            try
            {
                HttpClient client = new HttpClient();
                return await client.GetStringAsync(VersionUrl);
            }
            catch (Exception ex)
            {
                return "";
            }


        }
        private static bool IsNewVersionAvailable(string latestVersion)
        {
            return Version.Parse(latestVersion) > GetCurrentVesrion();
        }
        public static Version GetCurrentVesrion()
        {
            Assembly assembly = Assembly.GetEntryAssembly();

            Version version = Version.Parse(assembly.GetName().Version.ToString());
           // Version version = Version.Parse(ConfigurationManager.AppSettings["Version"].ToString());
            return version;
        }
        public async Task GetUpdateAsync()
        {
            try
            {
               
                    string latestVersion = await GetLatestVersionAsync();
                    if (IsNewVersionAvailable(latestVersion))
                    {
                        Console.WriteLine($"New version {latestVersion} is available!");
                        string result = MyMessageBox.ShowDialog($"New version {latestVersion} is available ! \n\n Update will take few minutes. Would you like to continue ?", MyMessageBox.Buttons.Yes_No);

                        if (result == "1")
                        {
                            DownloadZipFromUrl(UpdatePackageUrl);

                        }
                    else
                    {
                        GlobalVariables.IsStatus = true;
                    }
                        // Optionally, ask the user if they want to download the update

                    }
                    
              
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
            }
        }
        public string DownloadZipFromUrl(string url)
        {
            try
            {
                // Get the system's temp folder path
                string tempPath = System.IO.Path.GetTempPath();

                // Create a file name for the downloaded ZIP file
                string fileName = System.IO.Path.GetFileName(url);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = "DownloadedZipFile.zip"; // Default file name
                }

                string filePath = System.IO.Path.Combine(tempPath, fileName);

                // Download the ZIP file from the URL and save it to the temp folder
                using (WebClient client = new WebClient())
                {
                    client.DownloadFile(new Uri(url), filePath);
                }

                // Return the path where the file was saved
                // Unzip the file
                string filePath1 = System.IO.Path.Combine(tempPath, "DGISApp" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                ZipFile.ExtractToDirectory(filePath, filePath1);
                // Start the process
                using (Process process = Process.Start(System.IO.Path.Combine(filePath1, "DGISApp\\setup.exe")))
                {
                    foreach (var process1 in Process.GetProcessesByName("DGISAPP"))
                    {
                        process1.Kill();
                        process1.WaitForExit();
                    }
                    // Read the output
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();  // Wait for the process to complete

                    // Console.WriteLine("Output: " + output);
                }

                return filePath;
                ///
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
                // Handle exceptions and return the error message
                return $"Error downloading the file: {ex.Message}";

            }
        }
    }
}
