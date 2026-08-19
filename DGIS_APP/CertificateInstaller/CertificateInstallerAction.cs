using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
namespace CertificateInstaller
{
    [RunInstaller(true)]
    public class CertificateInstallerAction : System.Configuration.Install.Installer
    {
      
        string ipPort = "0.0.0.0:55102";
        string certHash = "debe38cb14453fbe826052065798b7447291673f"; 
        string appId = "{00112233-4455-6677-8899-AABBCCDDEEFF}"; 

        #region Install exe
        public override void Install(System.Collections.IDictionary stateSaver)
        {
            base.Install(stateSaver); 
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
                    RemoveOldDGISDesktop();
                    RemoveOldDGISStartMenu();
                    RemoveOldDGISStartMenu1();
                    OpenPort();
                    DemandHttp();
                    Advfirewall();

                    foreach (var process in Process.GetProcessesByName("DGISAPP"))
                    {
                        process.Kill();
                        process.WaitForExit();
                    } 
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
        public void Advfirewall()
        {
            try
            {
                // The command to enable to http service to start on demand
                string command = $@"netsh advfirewall firewall add rule name=DGIS_TCP_55102 dir=in action=allow protocol=TCP localport=55102";



                ExecuteNetshCommand(command);


            }
            catch (Exception ex)
            {
                // Log exception or handle errors as necessary
                Console.WriteLine($"Error: {ex.Message}");
                ErrorLog.LogErrorToFile(ex);
                //return false;
            }
        }

        public void DemandHttp()
        {
            try
            {
                // The command to enable to http service to start on demand
                string command1 = $@"sc config http start= demand";

                ExecuteNetshCommand(command1);
                // The command to start the http service
                string command2 = $@"net start http";

                ExecuteNetshCommand(command2);


            }
            catch (Exception ex)
            {
                // Log exception or handle errors as necessary
                Console.WriteLine($"Error: {ex.Message}");
                ErrorLog.LogErrorToFile(ex);
                //return false;
            }
        }
        public void OpenPort()
        {
            try
            { 
                string command = $@"netsh http add urlacl url=https://+:55102/ user=everyone";

                ExecuteNetshCommand(command);
            }
            catch (Exception ex)
            { 
                Console.WriteLine($"Error: {ex.Message}"); 
            }
        }
        public void checkHostIsorNot()
        {
            try
            {
                string hostsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), @"drivers\etc\hosts");
                 
                var lines = File.ReadAllLines(hostsFilePath);
                 
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
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, @"Microsoft\Windows\Start Menu\Programs\Startup\DGIS App.appref-ms");
             
            if (File.Exists(filePath))
            {

                try
                { 
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
                string command = $@"del ""%userprofile%\Desktop\DGIS App.appref-ms""";
                 
                var processInfo = new ProcessStartInfo
                {



                    FileName = "cmd.exe",
                    Arguments = $"/C {command}",
                    Verb = "runas",  
                    UseShellExecute = true,
                    CreateNoWindow = true
                };
                 
                using (var process = Process.Start(processInfo))
                {
                    process.WaitForExit();

                }
            }
            catch (Exception ex)
            {
                 
                Console.WriteLine($"Error: {ex.Message}");
                
            }
        }
        private void RunExecutable()
        {
            try
            {
                string exePath = Context.Parameters["TARGETDIR"] + "DGISAPP.exe";  
                ProcessStartInfo procInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true, 
                    Verb = "runas" 
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
                string command = $@"echo {ipAddress} {hostName} >> %windir%\System32\drivers\etc\hosts";
                ExecuteNetshCommand(command);
                return true;

            }
            catch (Exception ex)
            { 
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }
        private X509Certificate2 InstallCertificate(string certFilePath)
        { 
            X509Certificate2 cert = new X509Certificate2(certFilePath); 
             
            X509Store store = new X509Store("Root", StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
             
            store.Add(cert);
             
            store.Close();

            return cert;
        }
        private X509Certificate2 InstallCertificatepers(string certFilePath)
        { 
            X509Certificate2 cert = new X509Certificate2(certFilePath, "123456", X509KeyStorageFlags.MachineKeySet);
             
            X509Store store = new X509Store("My", StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
             
            store.Add(cert);
             
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
                    Arguments = "/c " + arguments,  
                    Verb = "runas", 
                    RedirectStandardOutput = true, 
                    RedirectStandardError = true,   
                    UseShellExecute = false, 
                    CreateNoWindow = true     
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

            if (string.IsNullOrEmpty(certHash))
            {
                throw new ArgumentException("Certificate thumbprint not provided.");
            }
            try
            {
                RemoveCertificateMY(certHash);
                RemoveCertificateRoot(certHash); 

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during certificate removal: {ex.Message}");
                throw;
            }
        }
        private void RemoveCertificateMY(string thumbprint)
        { 
            X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
             
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
             
            store.Close();
        }
        private void RemoveCertificateRoot(string thumbprint)
        { 
            X509Store store = new X509Store("Root", StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
             
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
                 
                var lines = File.ReadAllLines(hostsFilePath);
                 
                string entryToRemove = $"{ipAddress} {hostName}";
                var updatedLines = new List<string>();

                foreach (var line in lines)
                {
                    if (!line.Trim().Equals(entryToRemove, StringComparison.OrdinalIgnoreCase))
                    {
                        updatedLines.Add(line);
                    }
                }
                 
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
