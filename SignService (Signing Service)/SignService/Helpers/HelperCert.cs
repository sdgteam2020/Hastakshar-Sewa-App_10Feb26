using iText.Forms.Fields;
using iText.Forms;
using iText.Kernel.Pdf;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Security.Cryptography;
using static iText.StyledXmlParser.Css.Parse.CssDeclarationValueTokenizer;

namespace SignService.Helpers
{
    public class HelperCert
    {
        string CertThumbPrint = "";
        public List<DigitalSignData> GetSignatureCordinate(string pdfPath)
        {
            List<DigitalSignData> lst = new List<DigitalSignData>();
            using (PdfReader reader = new PdfReader(pdfPath))
            {
                using (PdfDocument pdfDoc = new PdfDocument(reader))
                {
                    PdfAcroForm acroForm = PdfAcroForm.GetAcroForm(pdfDoc, false);
                    if (acroForm == null)
                    {
                        Console.WriteLine("No signature fields found.");
                        return null;
                    }

                    IDictionary<string, PdfFormField> fields = acroForm.GetFormFields();

                    foreach (var field in fields)
                    {
                        DigitalSignData digitalSignData = new DigitalSignData();
                        if (field.Value is PdfSignatureFormField signatureField)
                        {
                            string signatureName = field.Key;
                            var rect = signatureField.GetWidgets()[0].GetRectangle().ToRectangle();
                            digitalSignData.XCoordinate = (int)rect.GetX();
                            digitalSignData.YCoordinate = (int)rect.GetY();

                            lst.Add(digitalSignData);
                        }
                    }
                    return lst;
                }
            }
        }
        public async Task<ResponseStatus> CheckSomethingAsync()
        {
            ResponseStatus responseStatus=new ResponseStatus();
            X509Certificate2 cert1 = null;
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            X509Certificate2Collection fcollection = new X509Certificate2Collection();

            try
            {
                store.Open(OpenFlags.OpenExistingOnly);
                await Task.Run(() =>
                {

                    foreach (X509Certificate2 cert in store.Certificates)
                    {
                        try
                        {
                            if (!(cert.Subject.Contains("localhost") || cert.Subject.Contains("DESKTOP")))
                            {
                                //if(cert.Subject.Contains("SERIALNUMBER"))
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
                if (fcollection.Count == 0)
                {
                    responseStatus.Status = "0";
                    responseStatus.Remark = "Pl insert valid Token !";
                    return responseStatus;
                    //MyMessageBox.ShowDialog("Pl insert valid Token !");
                }
                else
                {
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                        CertThumbPrint = cert1.Thumbprint;
                        responseStatus.Status = "1";
                        responseStatus.Remark = CertThumbPrint;
                        return responseStatus;
                    }
                    else
                    {
                        try
                        {
                            X509Certificate2Collection selectedCertificates = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection);

                            if (selectedCertificates.Count > 0)
                            {
                                cert1 = selectedCertificates[0];
                                string[] SubjectSplit = cert1.Subject.Split(',');
                                if (DateTime.Now <= cert1.NotAfter)
                                {
                                    CertThumbPrint = cert1.Thumbprint;
                                    responseStatus.Status = "1";
                                    responseStatus.Remark = CertThumbPrint;
                                    return responseStatus;
                                }
                                else
                                {
                                   // CertThumbPrint = cert1.Thumbprint;
                                   // responseStatus.Status = "1";
                                     responseStatus.Status = "-1";
                                     responseStatus.Remark = "Token is expired. Pl contact issuer!";
                                  //  responseStatus.Remark = CertThumbPrint;
                                    return responseStatus;
                                    
                                }

                                
                                
                            }
                        }
                        catch
                        {
                            responseStatus.Status = "2";
                            responseStatus.Remark = "";
                            return responseStatus;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
                if (fcollection.Count == 0)
                {
                    CertThumbPrint = "";
                    responseStatus.Status = "0";
                    responseStatus.Remark = "Pl insert valid Token !";
                    return responseStatus;
                    // MyMessageBox.ShowDialog("Pl insert valid Token !");
                   // CertThumbPrint = "";
                }
                else
                {
                    CertThumbPrint = "";
                    responseStatus.Status = "-1";
                    responseStatus.Remark = "Try again or report to ASDC. Reason1:- " + ex.Message;
                    return responseStatus;
                   // // MyMessageBox.ShowDialog("Try again or report to ASDC. Reason1:- " + ex.Message);
                   
                }
             
            }
            responseStatus.Status = "0";
            responseStatus.Remark = "Try again";
            return responseStatus;
            // return "Check completed!";
        }


       
    }
}