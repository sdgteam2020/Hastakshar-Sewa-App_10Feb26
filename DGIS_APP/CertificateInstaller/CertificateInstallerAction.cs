using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace CertificateInstaller
{
    [RunInstaller(true)]
    public class CertificateInstallerAction : System.Configuration.Install.Installer
    {
      
        string ipPort = "0.0.0.0:55102";///Ip And Port
        string certHash = "debe38cb14453fbe826052065798b7447291673f"; //old cert-"f3ba8d0ffba333cd43ff1f8009c470edab93fbaa"; // Replace with your certificate thumbprint         
        string appId = "{00112233-4455-6677-8899-AABBCCDDEEFF}"; // Replace with your application GUID

        #region Install exe
        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver);

            // Get the certificate file path from the custom action parameter
            string certFilePath = Context.Parameters["certFilePath"];
            string certFilePathLocal = Context.Parameters["CertFilePathpfx"];
            if (!string.IsNullOrEmpty(certFilePath))
            {
                try
                {

                  
                    X509Certificate2 certificate = InstallCertificate(certFilePath);
                    X509Certificate2 certificate1 = InstallCertificatepers(certFilePathLocal);

                    var ss = AddSslCert(ipPort, certHash, appId);
                    checkHostIsorNot();
                    //AddToStartup();
                    RemoveOldDGISDesktop();
                    RemoveOldDGISStartMenu();
                    RemoveOldDGISStartMenu1();
                    OpenPort();

                    foreach (var process in Process.GetProcessesByName("DGISAPP"))
                    {
                        process.Kill();
                        process.WaitForExit();
                    }

                    // RunExecutable();
                }
                catch (Exception ex)
                {

                    throw new System.Configuration.Install.InstallException("Failed to install the certificate", ex);
                }
            }


        }
        public override void Commit(System.Collections.IDictionary savedState)
        {
            base.Commit(savedState);
            RunExecutable();
        }
        public void OpenPort()
        {
            try
            {
                // The command to remove Shot DGIS APP
                string command = $@"netsh http add urlacl url=https://+:55102/ user=everyone";

                ExecuteNetshCommand(command);
            }
            catch (Exception ex)
            {
                // Log exception or handle errors as necessary
                Console.WriteLine($"Error: {ex.Message}");
                //return false;
            }
        }
        public void checkHostIsorNot()
        {
            try
            {
                string hostsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");

                // Read all lines from the hosts file
                var lines = File.ReadAllLines(hostsFilePath);

                // Filter out the line to be removed
                string entryToCheck = $"{"127.0.0.1"} {"dgisapp.army.mil"}";
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
                    AddHostName("127.0.0.1", "dgisapp.army.mil");
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while modifying the hosts file: {ex.Message}");

            }
        }
        private void RemoveOldDGISStartMenu1()
        {
            //C: \Users\asdc\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, @"Microsoft\Windows\Start Menu\Programs\Startup\DGIS App.appref-ms");
            //MessageBox.Show(filePath);
            if (File.Exists(filePath))
            {

                try
                {
                    // Command to delete the file using CMD
                    string command = $"/C del \"{filePath}\"";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = command,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = new Process { StartInfo = psi })
                    {
                        process.Start();
                        process.WaitForExit();

                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        if (!string.IsNullOrEmpty(error))
                        {
                            Console.WriteLine("Error: " + error);
                        }
                        else
                        {
                            Console.WriteLine("File deleted successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("File not found.");
            }
        }
        private void RemoveOldDGISStartMenu()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, @"Microsoft\Windows\Start Menu\Programs\DGIS\1\DGIS App.appref-ms");

            if (File.Exists(filePath))
            {
                try
                {
                    // Command to delete the file using CMD
                    string command = $"/C del \"{filePath}\"";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = command,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = new Process { StartInfo = psi })
                    {
                        process.Start();
                        process.WaitForExit();

                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        if (!string.IsNullOrEmpty(error))
                        {
                            Console.WriteLine("Error: " + error);
                        }
                        else
                        {
                            Console.WriteLine("File deleted successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("File not found.");
            }
        }
        private void RemoveOldDGISDesktop()
        {
            try
            {
                // The command to remove Shot DGIS APP
                string command = $@"del ""%userprofile%\Desktop\DGIS App.appref-ms""";

                // Using netsh or command prompt to modify the hosts file
                var processInfo = new ProcessStartInfo
                {



                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    Verb = "runas", // Ensures the process runs with administrator privileges
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                // Start the process
                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();

                }
            }
            catch (Exception ex)
            {
                // Log exception or handle errors as necessary
                Console.WriteLine($"Error: {ex.Message}");
                //return false;
            }
        }
        private void RunExecutable()
        {
            try
            {
                string exePath = Context.Parameters["TARGETDIR"] + "DGISAPP.exe"; // Replace with your .exe name
                ProcessStartInfo procInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true, // Required for running as admin
                    Verb = "runas" // Requests admin privileges
                };

                try
                {
                    Process.Start(procInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running executable: {ex.Message}");
                throw;
            }
        }
        public static string AddSslCert(string ipPort, string certHash, string appId)
        {
            string command = $"netsh http add sslcert ipport={ipPort} certhash={certHash} appid=\"{appId}\" certstorename=MY";
            return ExecuteNetshCommand(command);
        }
        public static bool AddHostName(string ipAddress, string hostName)
        {
            try
            {
                // The command to append to the hosts file
                string command = $@"echo {ipAddress} {hostName} >> %windir%\System32\drivers\etc\hosts";
                ExecuteNetshCommand(command);
                return true;

            }
            catch (Exception ex)
            {
                // Log exception or handle errors as necessary
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
        private X509Certificate2 InstallCertificate(string certFilePath)
        {
            // Load the certificate
            X509Certificate2 cert = new X509Certificate2(certFilePath);
            // X509Certificate2 cert = new X509Certificate2(certFilePath, "123456", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            // Open the certificate store (LocalMachine in this case)
            X509Store store = new X509Store("Root", StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            // Add the certificate to the store
            store.Add(cert);

            // Close the store
            store.Close();

            return cert;
        }
        private X509Certificate2 InstallCertificatepers(string certFilePath)
        {
            // Load the certificate
            //X509Certificate2 cert = new X509Certificate2(certFilePath);
            X509Certificate2 cert = new X509Certificate2(certFilePath, "123456", X509KeyStorageFlags.MachineKeySet);

            // Open the certificate store (LocalMachine in this case)
            X509Store store = new X509Store("My", StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            // Add the certificate to the store
            store.Add(cert);

            // Close the store
            store.Close();

            return cert;
        }

        private static string ExecuteNetshCommand(string arguments)
        {
            try
            {
                ProcessStartInfo processInfo = new ProcessStartInfo
                {

                    FileName = "cmd.exe",
                    Arguments = "/c " + arguments, // "/c" means execute and terminate
                    Verb = "runas", // This makes the process run as administrator
                    RedirectStandardOutput = true, // Capture output
                    RedirectStandardError = true,  // Capture errors
                    UseShellExecute = false, // Required for redirecting output
                    CreateNoWindow = true    // Prevents showing a command window
                };

                using (Process process = new Process())
                {
                    process.StartInfo = processInfo;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        throw new Exception($"Error: {error}");
                    }

                    //Console.WriteLine("Command executed successfully.");
                    return (output);
                }
            }
            catch (Exception ex)
            {
                return ($"Exception: {ex.Message}");
            }
        }


        #endregion End

        #region UnInstall
        public override void Uninstall(IDictionary savedState)
        {
            base.Uninstall(savedState);
            // Replace with the thumbprint of the certificate to be removed
            //string certThumbprint = Context.Parameters["CertThumbprint"];

            if (string.IsNullOrEmpty(certHash))
            {
                throw new ArgumentException("Certificate thumbprint not provided.");
            }
            try
            {
                RemoveCertificateMY(certHash);
                RemoveCertificateRoot(certHash);
                //var ss = RemoveSslCert(ipPort);
                //RemoveHostEntry("127.0.0.1", "dgisapp.army.mil");
                //RemoveFromStartup();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during certificate removal: {ex.Message}");
                throw;
            }
        }
        private void RemoveCertificateMY(string thumbprint)
        {
            // Open the certificate store
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            // Find the certificate by thumbprint
            X509Certificate2Collection certCollection = store.Certificates.Find(
                X509FindType.FindByThumbprint, thumbprint, false);

            if (certCollection.Count > 0)
            {
                foreach (X509Certificate2 cert in certCollection)
                {
                    store.Remove(cert);
                    Console.WriteLine($"Certificate '{cert.Subject}' removed successfully.");
                }
            }
            else
            {
                Console.WriteLine($"Certificate with thumbprint {thumbprint} not found.");
            }

            // Close the certificate store
            store.Close();
        }
        private void RemoveCertificateRoot(string thumbprint)
        {
            // Open the certificate store
            X509Store store = new X509Store("Root", StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);

            // Find the certificate by thumbprint
            X509Certificate2Collection certCollection = store.Certificates.Find(
                X509FindType.FindByThumbprint, thumbprint, false);

            if (certCollection.Count > 0)
            {
                foreach (X509Certificate2 cert in certCollection)
                {
                    store.Remove(cert);
                    Console.WriteLine($"Certificate '{cert.Subject}' removed successfully.");
                }
            }
            else
            {
                Console.WriteLine($"Certificate with thumbprint {thumbprint} not found.");
            }

            // Close the certificate store
            store.Close();
        }

        public static string RemoveSslCert(string ipPort)
        {
            string command = $"netsh http delete sslcert ipport={ipPort}";
            return ExecuteNetshCommand(command);
        }
        public static bool RemoveHostEntry(string ipAddress, string hostName)
        {
            try
            {
                string hostsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");

                // Read all lines from the hosts file
                var lines = File.ReadAllLines(hostsFilePath);

                // Filter out the line to be removed
                string entryToRemove = $"{ipAddress} {hostName}";
                var updatedLines = new List<string>();

                foreach (var line in lines)
                {
                    if (!line.Trim().Equals(entryToRemove, StringComparison.OrdinalIgnoreCase))
                    {
                        updatedLines.Add(line);
                    }
                }

                // Write the updated lines back to the hosts file
                File.WriteAllLines(hostsFilePath, updatedLines);

                Console.WriteLine($"Removed {entryToRemove} from the hosts file successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while modifying the hosts file: {ex.Message}");
                return false;
            }
        }


        #endregion

    }
}
