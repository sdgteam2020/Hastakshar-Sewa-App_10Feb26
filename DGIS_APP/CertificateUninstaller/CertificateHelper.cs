using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace CertificateUninstaller
{
    
    public class CertificateHelper
    {
        string certStore = "LocalMachine";
        string storeLocation = "My";
        string ipPort = "0.0.0.0:8443";///Ip And Port
        string certHash = "f3ba8d0ffba333cd43ff1f8009c470edab93fbaa"; // Replace with your certificate thumbprint
        string appId = "{00112233-4455-6677-8899-AABBCCDDEEFF}"; // Replace with your application GUID
        public static void UninstallCertificate(string thumbprint)
        {
            try
            {
                // Access the LocalMachine store and the My (Personal) certificate store
                X509Store store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                store.Open(OpenFlags.ReadWrite);

                // Find the certificate by thumbprint
                X509Certificate2Collection certificates = store.Certificates.Find(
                    X509FindType.FindByThumbprint, thumbprint, false);

                // Remove the certificate if it exists
                foreach (X509Certificate2 cert in certificates)
                {
                    store.Remove(cert);
                    Console.WriteLine($"Certificate '{cert.Subject}' removed successfully.");
                }

                store.Close();
            }
            catch (Exception ex)
            {
                //File.WriteAllText(@"C:\CertificateRemovalLog.txt", $"Error: {ex.Message}");
                throw;
            }
        }
    }
}
