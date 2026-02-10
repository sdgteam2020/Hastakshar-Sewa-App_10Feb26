using Microsoft.Office.Interop.Word;
using System;
using System.Configuration;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Document = Microsoft.Office.Interop.Word.Document;
namespace SignService.Helpers
{
    public static class helper
    {
        public static void ConvertPDF(string inputpath, string outputPath, WdSaveFormat format)
        {
            try
            {
                FileInfo f1 = new FileInfo(outputPath);
                if (f1.Exists)
                {
                    File.Delete(outputPath);
                }
                
                Application wordApp = new Application();
                Document wordDoc = null;
                wordDoc = wordApp.Documents.Open(inputpath);
            
                // Save as PDF
                wordDoc.SaveAs2(outputPath, WdSaveFormat.wdFormatPDF);
                wordDoc.Close();    
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
            }
        }
        
        public static async Task<bool> HasInternetConnectionAsyncTest()
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(2); // Adjust the timeout as needed
                    //var request = new HttpRequestMessage(HttpMethod.Head, "https://google.com");
                    var request = new HttpRequestMessage(HttpMethod.Head, ConfigurationManager.AppSettings["HasInternetConnection"]);
                    var response = await httpClient.SendAsync(request);

                    return response.IsSuccessStatusCode;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
                return false; // Return false if there's an issue with the HTTP request
            }
        }
        public static async Task<X509Certificate2Collection> GetCertificates()
        {
            //X509Certificate2 cert1 = null;
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            X509Certificate2Collection fcollection = new X509Certificate2Collection();

            try
            {
                store.Open(OpenFlags.OpenExistingOnly);
                await System.Threading.Tasks.Task.Run(() =>
                {

                    foreach (X509Certificate2 cert in store.Certificates)
                    {
                        try
                        {
                            if (!(cert.Subject.Contains("localhost") || cert.Subject.Contains("DESKTOP")))
                            {
                                if (cert.PrivateKey is RSACryptoServiceProvider rsaProvider && rsaProvider.CspKeyContainerInfo.HardwareDevice)
                                {
                                    fcollection.Add(cert);
                                }
                            }
                        }
                        catch (CryptographicException)
                        {
                            // Handle any exception when accessing the private key
                            // You can log the error or skip this certificate
                        }
                    }
                    store.Close();
                });

            }
            catch (Exception ex)
            {
                fcollection = null;
                ErrorLog.LogErrorToFile(ex);
            }

            return fcollection;
        }
    }
}