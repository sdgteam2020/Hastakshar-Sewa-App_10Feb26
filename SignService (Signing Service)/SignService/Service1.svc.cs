using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Signatures;
using Microsoft.Office.Interop.Word;
using SignService.Helpers;
using SignService.HttpClients;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using ValidateCertificate;
using WinniesMessageBox;
using static iText.Signatures.PdfSigner;


namespace SignService
{
    public class Service1 : IService1
    {
        public static string PrevThumbNail = "";
        bool IsLocalToken = true;
        public string GetData(string element)
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.OpenExistingOnly);

            X509Certificate2 cert1 = X509Certificate2UI.SelectFromCollection(store.Certificates, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
            X509Certificate2 certificate = cert1;

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(element);
            XmlDocument xml1 = SignXML(xml, certificate);
            return string.Format("You entered: 0");
        }

        public async Task<XmlElement> SignXml(XmlElement value)
        {
            try
            {
                X509Certificate2Collection fcollection = await helper.GetCertificates();

                if (fcollection.Count == 0)
                {
                    string message = "No Token Found";
                    XmlDocument xml = new XmlDocument();
                    xml.LoadXml("<Root>" + message + "</Root>");
                    return xml.DocumentElement;

                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }
                    X509Certificate2 certificate = cert1;


                    string result = "Success";
                    if (result == "Success")
                    {
                        XmlDocument xml = new XmlDocument();
                        XmlDocument xml1 = null;
                        xml.LoadXml(value.OuterXml);
                        int count = 0;
                        var signatureNode = xml.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);


                        XmlDocument xmlDoc = new XmlDocument();
                        count = signatureNode.Count + 1;


                        XmlElement root = xmlDoc.CreateElement("DigitalSignature" + count);
                        xmlDoc.AppendChild(root);
                        xmlDoc.DocumentElement.AppendChild(xmlDoc.ImportNode(xml.DocumentElement, true));




                        xml1 = SignXML(xmlDoc, certificate);





                        return xml1.DocumentElement;
                    }
                    else
                    {
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(result);
                        return xml.DocumentElement;
                    }
                }

            }
            catch (Exception ex)
            {
                XmlDocument xml = new XmlDocument();
                xml.LoadXml(ex.Message);
                ErrorLog.LogErrorToFile(ex);
                return xml.DocumentElement;

            }
        }
        public static XmlDocument SignXML(XmlDocument doc, X509Certificate2 cert)
        {
            try
            {
                SignedXml signed = new SignedXml(doc);

                var rsaKey = cert.GetRSAPrivateKey();
                signed.SigningKey = cert.PrivateKey;

                Reference reference = new Reference();
                reference.Uri = "";
                reference.AddTransform(new XmlDsigEnvelopedSignatureTransform());
                signed.AddReference(reference);

                KeyInfo keyInfo = new KeyInfo();
                keyInfo.AddClause(new KeyInfoX509Data(cert));

                signed.KeyInfo = keyInfo;
                signed.ComputeSignature();
                XmlElement xmlSig = signed.GetXml();


                doc.DocumentElement.AppendChild(doc.ImportNode(xmlSig, true));
                return doc;
            }
            catch (Exception ex)
            {

                XmlNode rootElement = doc.SelectSingleNode("/SignXmlRequest/XmlData/RootElement");
                XmlElement Exception = doc.CreateElement("Exception");
                Exception.InnerText = ex.Message.ToString();

                if ("Hi" != null)
                {
                    rootElement.AppendChild(Exception);
                }
                ErrorLog.LogErrorToFile(ex);

                return doc;

            }
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }


        public async Task<List<TokenDetails>> FetchPersID()
        {

            List<TokenDetails> TokenDetailList = new List<TokenDetails>();
            try
            {
                X509Certificate2Collection fcollection = await helper.GetCertificates();
                if (fcollection.Count == 0)
                {
                    var TokenDetails = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchPersID",
                        CRL_OCSPCheck = false,
                        Status = "404",
                        Remarks = "Certificate not Found. Please insert valid Token and Try agian!"

                    };
                    TokenDetailList.Add(TokenDetails);

                    return TokenDetailList.ToList();
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }

                    string[] SubjectSplit = cert1.Subject.Split(',');
                    string PersNo = "";
                    for (int i = 0; i < SubjectSplit.Length; i++)
                    {
                        if (SubjectSplit[i].Contains("SERIALNUMBER="))
                            PersNo = SubjectSplit[i].ToString().Replace("SERIALNUMBER=", "").Trim();
                    }



                    bool TokenValidity = false;
                    string Remark = "";
                    if (DateTime.Now <= cert1.NotAfter)
                    {
                        TokenValidity = true;
                        Remark = "Personal No of Unique Cert is fetched for the inserted Token";
                    }
                    else
                    {
                        TokenValidity = false;
                        Remark = "Token Expired";
                    }

                    if (!string.IsNullOrEmpty(PersNo))
                    {
                        var TokenDetails = new TokenDetails
                        {
                            API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchPersID",
                            CRL_OCSPCheck = false,
                            subject = PersNo,
                            issuer = null,
                            Thumbprint = null,
                            ValidFrom = cert1.NotBefore.ToString(),
                            ValidTo = cert1.NotAfter.ToString(),
                            Status = "200",
                            Remarks = Remark,
                            TokenValid = TokenValidity
                        };
                        TokenDetailList.Add(TokenDetails);
                        return TokenDetailList.ToList();
                    }
                    else
                    {
                        throw new Exception("Personal No is Empty. Pl report and try with different Token");
                    }

                }

            }
            catch (Exception ex)
            {

                var TokenDetails = new TokenDetails
                {
                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchUniqueTokenDetails",
                    CRL_OCSPCheck = false,
                    Status = "500",
                    Remarks = "Exception Occured-" + ex.Message.ToString()

                };
                TokenDetailList.Add(TokenDetails);
                ErrorLog.LogErrorToFile(ex);
                return TokenDetailList.ToList();
            }

        }

        public async Task<bool> ValidatePersID2FA(string inputPersID)
        {
            try
            {
                X509Certificate2Collection fcollection = await helper.GetCertificates();

                if (fcollection.Count == 0)
                {
                    return false;
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }

                    X509Certificate2 certificate = cert1;
                    try
                    {
                        string[] SubjectSplit = cert1.Subject.Split(',');
                        string response = "";
                        for (int i = 0; i < SubjectSplit.Length; i++)
                        {
                            if (SubjectSplit[i].Contains("SERIALNUMBER="))
                                response = SubjectSplit[i].ToString().Replace("SERIALNUMBER=", "").Trim();
                        }
                        if (inputPersID == response)
                        {
                            if (VerifyCertificatePassword(cert1))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    catch (CryptographicException)
                    {
                        return false;
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }

        private bool VerifyCertificatePassword(X509Certificate2 certificate)
        {
            try
            {
                if (!certificate.HasPrivateKey)
                {
                    return false;
                }

                using (RSA rsa = certificate.GetRSAPrivateKey())
                {
                    byte[] message = Encoding.UTF8.GetBytes("2FA");
                    byte[] signature = rsa.SignData(message, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    bool verified = rsa.VerifyData(message, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    return verified;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }


        public async Task<List<PersIdValidation>> ValidatePersID(string inputPersID)
        {

            List<PersIdValidation> PersIdValid = new List<PersIdValidation>();

            try
            {
                X509Certificate2Collection fcollection = await helper.GetCertificates();

                if (fcollection.Count == 0)
                {
                    var validation = new PersIdValidation
                    {
                        vaildId = false,
                        Expired = false,
                        Status = "404",
                        Remark = "Token Not Found !"
                    };
                    PersIdValid.Add(validation);
                    return PersIdValid.ToList();
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }
                    X509Certificate2 certificate = cert1;

                    string result = "Success";

                    if (result == "Success")


                    {
                        string[] SubjectSplit = cert1.Subject.Split(',');
                        string response = "";
                        for (int i = 0; i < SubjectSplit.Length; i++)
                        {
                            if (SubjectSplit[i].Contains("SERIALNUMBER="))
                                response = SubjectSplit[i].ToString().Replace("SERIALNUMBER=", "").Trim();
                        }
                        bool TokenExpity = false;
                        string StatusMsg = "200";

                        if (DateTime.Now > cert1.NotAfter)
                        {
                            TokenExpity = true;
                            StatusMsg = "201";
                        }

                        if (inputPersID == response)
                        {
                            var validation = new PersIdValidation
                            {
                                vaildId = true,
                                Expired = TokenExpity,
                                Status = StatusMsg,
                                Remark = "Token is Valid !"
                            };
                            PersIdValid.Add(validation);
                            return PersIdValid.ToList();

                        }
                        else
                        {
                            var validation = new PersIdValidation
                            {
                                vaildId = false,
                                Expired = TokenExpity,
                                Status = "200",
                                Remark = "Token is Not Valid !"
                            };
                            PersIdValid.Add(validation);
                            return PersIdValid.ToList();
                        }

                    }
                    else
                    {
                        var validation = new PersIdValidation
                        {
                            vaildId = false,
                            Expired = false
                        };
                        PersIdValid.Add(validation);
                        return PersIdValid.ToList();
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                var validation = new PersIdValidation
                {
                    vaildId = false,
                    Expired = false,
                    Status = "404",
                    Remark = "Token is Not Valid !"
                };
                PersIdValid.Add(validation);
                return PersIdValid.ToList();
            }
        }


        public async Task<List<TokenDetails>> FetchUniqueTokenDetails()
        {
            List<TokenDetails> TokenDetailList = new List<TokenDetails>();
            try
            {
                X509Certificate2Collection fcollection = await helper.GetCertificates();

                if (fcollection.Count == 0)
                {
                    var TokenDetails = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchUniqueTokenDetails",
                        CRL_OCSPCheck = false,
                        Status = "404",
                        Remarks = "Certificate not Found. Please insert valid Token and Try agian!",
                        TokenValid = false,
                    };
                    TokenDetailList.Add(TokenDetails);
                    return TokenDetailList.ToList();
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }

                    bool TokenValidity = false;
                    if (DateTime.Now <= cert1.NotAfter)
                    {
                        TokenValidity = true;
                    }
                    else
                    {
                        TokenValidity = false;
                    }

                    var TokenDetails = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchUniqueTokenDetails",
                        CRL_OCSPCheck = false,
                        subject = cert1.Subject,
                        issuer = cert1.Issuer,
                        Thumbprint = cert1.Thumbprint,
                        ValidFrom = cert1.NotBefore.ToString(),
                        ValidTo = cert1.NotAfter.ToString(),
                        Status = "200",
                        Remarks = "Unique Cert details of inserted Token",
                        TokenValid = TokenValidity
                    };
                    TokenDetailList.Add(TokenDetails);
                    return TokenDetailList.ToList();
                }
            }
            catch (Exception ex)
            {

                var TokenDetails = new TokenDetails
                {
                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchUniqueTokenDetails",
                    CRL_OCSPCheck = false,
                    Status = "500",
                    Remarks = "Exception Occured-" + ex.Message.ToString(),
                    TokenValid = false
                };
                ErrorLog.LogErrorToFile(ex);
                TokenDetailList.Add(TokenDetails);
                return TokenDetailList.ToList();
            }

        }


        public async Task<List<TokenDetails>> FetchTokenDetails()
        {
            List<TokenDetails> TokenDetailList = new List<TokenDetails>();
            try
            {
                X509Certificate2Collection fcollection = await helper.GetCertificates();

                if (fcollection.Count > 0)
                {
                    int i = 1;
                    foreach (X509Certificate2 cert1 in fcollection)
                    {
                        bool TokenValidity = false;
                        if (DateTime.Now <= cert1.NotAfter)
                        {
                            TokenValidity = true;
                        }
                        else
                        {
                            TokenValidity = false;
                        }

                        var detail = new TokenDetails
                        {
                            API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenDetails",
                            CRL_OCSPCheck = false,
                            subject = cert1.Subject,
                            issuer = cert1.Issuer,
                            Thumbprint = cert1.Thumbprint,
                            ValidFrom = cert1.NotBefore.ToString(),
                            ValidTo = cert1.NotAfter.ToString(),
                            Status = "200",
                            Remarks = "Details of Cert No-" + i + "- are as given above",
                            TokenValid = TokenValidity,

                        };
                        i++;
                        TokenDetailList.Add(detail);
                    }
                    return TokenDetailList.ToList();
                }
                else
                {
                    var detail = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenDetails",
                        CRL_OCSPCheck = false,
                        Status = "404",
                        Remarks = "Certificate not Found. Please insert valid Token and Try agian!",
                        TokenValid = false
                    };
                    TokenDetailList.Add(detail);
                    return TokenDetailList.ToList();
                }

            }
            catch (Exception ex)
            {
                var TokenDetails = new TokenDetails
                {
                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenDetails",
                    CRL_OCSPCheck = false,
                    Status = "500",
                    Remarks = "Exception Occured-" + ex.Message.ToString(),
                    TokenValid = false
                };

                TokenDetailList.Add(TokenDetails);
                ErrorLog.LogErrorToFile(ex);
                return TokenDetailList.ToList();
            }

        }

        public async Task<List<TokenDetails>> FetchTokenOCSPCrlDetailsAsync(bool IsCheckCrl, string ThumbPrint)
        {
            string MsgCrlOCSP = "";
            bool BlnCrlOCSP = true;
            List<TokenDetails> TokenDetailList = new List<TokenDetails>();
            try
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                X509Certificate2Collection fcollection = new X509Certificate2Collection();


                if (ThumbPrint == "")
                {
                    fcollection = await helper.GetCertificates();
                }
                else
                {
                    X509Certificate2Collection fcol = new X509Certificate2Collection();
                    fcol = await helper.GetCertificates();

                    X509Certificate2 selectedCertificate = fcol.Cast<X509Certificate2>().FirstOrDefault(cert => cert.Thumbprint.Equals(ThumbPrint, StringComparison.OrdinalIgnoreCase));
                    if (selectedCertificate != null)
                    {
                        fcollection.Add(selectedCertificate);
                    }

                }
                


                if (fcollection.Count == 0)
                {
                    var TokenDetails = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                        CRL_OCSPCheck = BlnCrlOCSP,
                        CRL_OCSPMsg = MsgCrlOCSP,
                        Status = "404",
                        Remarks = "Certificate not Found. Please insert valid Token and Try agian!",
                        TokenValid = false
                    };
                    TokenDetailList.Add(TokenDetails);
                    return TokenDetailList.ToList();
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        try
                        {
                            X509Certificate2Collection selectedCertificates = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection);

                            if (selectedCertificates.Count > 0)
                            {
                                cert1 = selectedCertificates[0];
                            }
                            else
                            {
                                var TokenDetails = new TokenDetails
                                {

                                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                                    CRL_OCSPCheck = BlnCrlOCSP,
                                    CRL_OCSPMsg = MsgCrlOCSP,
                                    subject = null,
                                    issuer = null,
                                    Thumbprint = null,
                                    ValidFrom = null,
                                    ValidTo = null,
                                    Status = "200",
                                    Remarks = "No Certificate Selected !",
                                    TokenValid = false,
                                };
                                TokenDetailList.Add(TokenDetails);
                                return TokenDetailList.ToList();
                            }
                        }
                        catch
                        {
                            var TokenDetails = new TokenDetails
                            {

                                API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                                CRL_OCSPCheck = BlnCrlOCSP,
                                CRL_OCSPMsg = MsgCrlOCSP,
                                subject = null,
                                issuer = null,
                                Thumbprint = null,
                                ValidFrom = null,
                                ValidTo = null,
                                Status = "200",
                                Remarks = "No Certificate Selected !",
                                TokenValid = false,
                            };
                            TokenDetailList.Add(TokenDetails);
                        }
                    }


                    if (IsCheckCrl == true)
                    {
                        if (PrevThumbNail != "")
                        {
                            if (cert1.Thumbprint == PrevThumbNail)
                            {
                                IsCheckCrl = false;
                            }
                            else
                            {
                                PrevThumbNail = cert1.Thumbprint;
                            }
                        }
                        else
                        {
                            PrevThumbNail = cert1.Thumbprint;
                        }
                    }


                    var (ValidateCertificateAsyncOutput, validationMsg, CrlMsg, OCSPMsg, CrlValid, OCSPValid) = await ValidateCertificate.ValidateCert.ValidateCertificateAsync(cert1, IsCheckCrl);


                    if (CrlValid == true || OCSPValid == true)
                    {
                        if (OCSPMsg == "Good" || CrlValid == true)
                        {
                            MsgCrlOCSP = "OCSP Verified";
                            BlnCrlOCSP = true;
                        }
                        else if (OCSPMsg == "NotFound" || CrlValid == false)
                        {
                            MsgCrlOCSP = "Digital Cert of token cannot be verified with CA due to Network issues";
                            BlnCrlOCSP = false;
                        }
                        else
                        {
                            MsgCrlOCSP = "Crl and OCSP Not Checked";
                            BlnCrlOCSP = false;
                        }


                    }
                    else
                    {
                        MsgCrlOCSP = "Crl or OCSP is Revoked";
                        BlnCrlOCSP = false;
                    }

                    if (ValidateCertificateAsyncOutput == true)
                    {
                        var TokenDetails = new TokenDetails
                        {

                            API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                            CRL_OCSPCheck = BlnCrlOCSP,
                            CRL_OCSPMsg = MsgCrlOCSP,
                            subject = cert1.Subject,
                            issuer = cert1.Issuer,
                            Thumbprint = cert1.Thumbprint,
                            ValidFrom = cert1.NotBefore.ToString(),
                            ValidTo = cert1.NotAfter.ToString(),
                            Status = "200",
                            Remarks = "Unique Cert details of inserted Token",
                            TokenValid = true,
                        };
                        TokenDetailList.Add(TokenDetails);
                    }
                    else
                    {
                        var TokenDetails = new TokenDetails
                        {

                            API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                            CRL_OCSPCheck = BlnCrlOCSP,
                            CRL_OCSPMsg = MsgCrlOCSP,
                            subject = cert1.Subject,
                            issuer = cert1.Issuer,
                            Thumbprint = cert1.Thumbprint,
                            ValidFrom = cert1.NotBefore.ToString(),
                            ValidTo = cert1.NotAfter.ToString(),
                            Status = "200",
                            Remarks = validationMsg,
                            TokenValid = false,
                        };
                        TokenDetailList.Add(TokenDetails);
                    }
                    return TokenDetailList.ToList();
                }
            }
            catch (Exception ex)
            {

                var TokenDetails = new TokenDetails
                {
                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchTokenOCSPCrlDetailsAsync",
                    CRL_OCSPCheck = BlnCrlOCSP,
                    CRL_OCSPMsg = MsgCrlOCSP,
                    Status = "500",
                    Remarks = "Exception Occured-" + ex.Message.ToString(),
                    TokenValid = false

                };
                TokenDetailList.Add(TokenDetails);
                ErrorLog.LogErrorToFile(ex);
                return TokenDetailList.ToList();
            }
        }

        private static void ExportAllCert()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);

            store.Open(OpenFlags.ReadOnly);

            X509Certificate2Collection certificates = store.Certificates;

            string exportPath = @"D:\Certificates";

            foreach (X509Certificate2 certificate in certificates)
            {

                byte[] certBytes = certificate.Export(X509ContentType.Cert);


                string filePath = Path.Combine(exportPath, $"{certificate.Thumbprint}.cer");

                File.WriteAllBytes(filePath, certBytes);
            }
            store.Close();
        }

        public X509Certificate2 DownloadCert(string url)
        {

            try
            {
                HttpWebRequest myReq = (HttpWebRequest)WebRequest.Create(url);
                WebResponse myResp = myReq.GetResponse();

                byte[] b = null;
                using (Stream stream = myResp.GetResponseStream())
                using (MemoryStream ms = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        byte[] buf = new byte[1024];
                        count = stream.Read(buf, 0, 1024);
                        ms.Write(buf, 0, count);
                    } while (stream.CanRead && count > 0);
                    b = ms.ToArray();
                }

                X509Certificate2 cert = new X509Certificate2(b);
                return cert;
            }
            catch (WebException)
            {
                return null;
            }
        }

        public void updateCert(X509Certificate2 cert, string subjectName)
        {
            if (cert != null)
            {
                X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);

                store.Open(OpenFlags.ReadWrite);
                X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
                X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(X509FindType.FindBySubjectName, subjectName, false);
                X509Certificate2 x509Certificate2 = new X509Certificate2(fcollection[0]);

                if (x509Certificate2.Thumbprint != cert.Thumbprint)
                {
                    store.Add(cert);
                }
            }
        }
        public async Task<ResponseMessage> DigitalSignAsync(List<DigitalSignData> reqData)
        {
            ResponseMessage responseMessage = new ResponseMessage();
            ResponseBulkSign apiResponse = await DigitalSignBulkAsync(reqData);

            if (apiResponse != null)
            {

                string resultstring = "";
                int count = 0;
                int Signed = 0;
                if (apiResponse.ResponseMessage != null)
                {
                    resultstring = "Congratulations!\n\nDocument is successfully Signed.\n";
                    resultstring += apiResponse.ResponseMessage.Message + "\n";
                    Signed = 1;
                }
                if (apiResponse.ResponseMessagelst != null)
                {
                    foreach (ResponseMessage data in apiResponse.ResponseMessagelst)
                    {

                        if (count == 0)
                        {
                            resultstring += "\n Opps!\nDocument is Not successfully Signed.\n";
                            resultstring += "This Docu Not Sign Either Password Protected or Page Not Found.\n";

                            count++;
                        }

                        resultstring += data.Message + "\n ";





                    }
                }
                if (resultstring != "")
                {
                    if (Signed > 0)
                    {
                        responseMessage.Message = resultstring;
                        responseMessage.Valid = true;

                    }
                    else
                    {
                        responseMessage.Message = resultstring;
                        responseMessage.Valid = false;
                    }

                }
                else
                {
                    if (apiResponse.ResponseMessage != null)
                    {
                        responseMessage.Message = $"Error:" + apiResponse.ResponseMessage.Message;
                        responseMessage.Valid = false;

                    }
                }

            }
            else
            {
                responseMessage.Message = $"Error:" + apiResponse.ResponseMessage.Message;
                responseMessage.Valid = false;
            }
            return responseMessage;
        }

        public async Task<ResponseBulkSign> DigitalSignBulkAsync(List<DigitalSignData> reqData)
        {
            string message = null;
            bool isAnyFileSigned = false;
            DTOSaveDigitalSignInfo saveDigitalSignInfo = new DTOSaveDigitalSignInfo();
            ResponseBulkSign ResponseMsgbullst = new ResponseBulkSign();

            ResponseMessage ResponseMsg = new ResponseMessage();
            List<ResponseMessage> ResponseMsglist = new List<ResponseMessage>();
            String NewFileName = "";
            int Pageno = 0;
            List<DigitalSignData> delData = new List<DigitalSignData>();

            var headers = WebOperationContext.Current?.IncomingRequest?.Headers;

            string origin = headers?["Origin"];
            string referer = headers?["Referer"];

            try
            {
                string ThumbPrint = reqData.First().Thumbprint;

                X509Certificate2Collection certCollection = new X509Certificate2Collection();

                X509Certificate2Collection fcol = new X509Certificate2Collection();
                fcol = await helper.GetCertificates();

                if (fcol.Count == 0)
                {
                    ResponseMsg.Message = "No Certificate Found !";
                    ResponseMsg.Valid = false;
                    ResponseMsgbullst.ResponseMessage = ResponseMsg;
                    return ResponseMsgbullst;
                }

                X509Certificate2 selectedCertificate = fcol.Cast<X509Certificate2>().FirstOrDefault(cert => cert.Thumbprint.Equals(ThumbPrint, StringComparison.OrdinalIgnoreCase));
                certCollection.Add(selectedCertificate);



                if (certCollection.Count == 0)
                {
                    ResponseMsg.Message = "Thumbprint not matched !";
                    ResponseMsg.Valid = false;
                    ResponseMsgbullst.ResponseMessage = ResponseMsg;
                    return ResponseMsgbullst;
                }

                X509Certificate2 cert1 = certCollection[0];

                if (DateTime.Now > cert1.NotAfter)
                {
                    ResponseMsg.Message = "Token Expired !";
                    ResponseMsg.Valid = false;
                    ResponseMsgbullst.ResponseMessage = ResponseMsg;
                    return ResponseMsgbullst;
                }


                string[] files = Directory.GetFiles(reqData.First().FolderLoc);

                int totalFiles = files.Count();
                int SingedFiles = 0;
                PdfSigner signer = null;
                FileStream fileStream = null;
                string Download = reqData.First().OutputFolderLoc;

                int Xaxis = reqData.First().XCoordinate;
                int Yaxis = reqData.First().YCoordinate;
                string CustomText = reqData.First().CustomText;
                if (reqData.First().Page != 0)
                {
                    Pageno = reqData.First().Page;
                }
                else
                {
                    Pageno = 1;
                }

                saveDigitalSignInfo.SignedDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
                var PublicKey = await GetPublicKey();
                byte[] textBytes = Encoding.UTF8.GetBytes(PublicKey.Public_Key);
                saveDigitalSignInfo.PublicKey = Convert.ToBase64String(textBytes);
                saveDigitalSignInfo.ValidToken = PublicKey.TokenValid;
                saveDigitalSignInfo.ValidFrom = PublicKey.ValidFrom;
                saveDigitalSignInfo.ValidTo = PublicKey.ValidTo;
                saveDigitalSignInfo.OriginForSign = origin;
                saveDigitalSignInfo.RefererForSign = referer;
                foreach (string filename in files)
                {
                nextfile:
                    string fileforloop = filename;
                    ResponseMessage ResponseMsg1 = new ResponseMessage();

                    if (NewFileName != "")
                    {
                        fileforloop = NewFileName;
                    }
                    else
                    {
                        fileforloop = filename;
                    }
                    if (Path.GetExtension(fileforloop) == ".pdf")
                    {

                        string FileFullName = Download + '\\' + Path.GetFileName(fileforloop) + "_DS_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + ".pdf";

                        PdfReader reader = new PdfReader(fileforloop);
                        IExternalSignature es = new X509Certificate2Signature(cert1, "SHA-1", ref message);

                        if (message != null)
                        {
                            ResponseMsg.Message = message;
                            ResponseMsg.Valid = false;
                        }
                        else
                        {
                            if (es.GetEncryptionAlgorithm() != null)
                            {
                                Org.BouncyCastle.X509.X509CertificateParser cp1 = new Org.BouncyCastle.X509.X509CertificateParser();

                                Org.BouncyCastle.X509.X509Certificate[] chain3 = new[] { cp1.ReadCertificate(cert1.RawData) };

                                await System.Threading.Tasks.Task.Run(() =>
                                {
                                    StampingProperties stampProp = new StampingProperties();
                                    stampProp.PreserveEncryption();
                                    ImageData imageData = null;

                                    using (StreamReader sr = new StreamReader(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + @"\DigitalSignWT.png"))
                                    {
                                        imageData = ImageDataFactory.Create(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DigitalSignWT.png");
                                    }

                                    string[] SubjectSplit = cert1.Subject.Split(',');

                                    string StrName = "";
                                    string StrICNo = "";
                                    string StrRank = "";
                                    for (int i = 0; i < SubjectSplit.Length; i++)
                                    {
                                        if (SubjectSplit[i].Contains("SERIALNUMBER="))
                                            StrICNo = SubjectSplit[i].ToString().Replace("SERIALNUMBER=", "").Trim();
                                        if (SubjectSplit[i].Contains("CN="))
                                            StrName = SubjectSplit[i].ToString().Replace("CN=", "").Trim();
                                        if (SubjectSplit[i].Contains("T="))
                                            StrRank = SubjectSplit[i].ToString().Replace("T=", "").Trim();
                                    }
                                    saveDigitalSignInfo.SerialNo = StrICNo;
                                    saveDigitalSignInfo.DocumentName = Path.GetFileName(FileFullName);



                                    iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(new PdfReader(fileforloop));
                                    SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
                                    IList<string> sigNames = signatureUtil.GetSignatureNames();
                                    iText.Kernel.Font.PdfFont font = PdfFontFactory.CreateFont(FontProgramFactory.CreateFont(StandardFonts.TIMES_BOLD));
                                    String StrSignature = "";
                                    if (CustomText != "")
                                        StrSignature = CustomText + "\n\n Digitally Signed by \n " + StrRank + " " + StrName + " \n Date : " + saveDigitalSignInfo.SignedDateTime + " \n © Hastakshar SEWA, DGIS";
                                    else
                                        StrSignature = "Digitally Signed by \n " + StrRank + " " + StrName + " \n Date : " + saveDigitalSignInfo.SignedDateTime + " \n © Hastakshar SEWA, DGIS";

                                    try
                                    {
                                        fileStream = new FileStream(FileFullName, FileMode.Create);
                                        if (sigNames.Count == 0)
                                        {
                                            signer = new PdfSigner(reader, fileStream, new StampingProperties());
                                        }
                                        else
                                        {
                                            HelperCert helperCert = new HelperCert();
                                            var getXYaxis = helperCert.GetSignatureCordinate(fileforloop);
                                            if (getXYaxis != null)
                                            {
                                                if (sigNames.Count % 2 == 0)
                                                {
                                                    Yaxis = getXYaxis[sigNames.Count - 1].YCoordinate + 50;
                                                    Xaxis = getXYaxis[0].XCoordinate;
                                                }
                                                else
                                                {
                                                    Yaxis = getXYaxis[sigNames.Count - 1].YCoordinate;
                                                    Xaxis = getXYaxis[sigNames.Count - 1].XCoordinate + 200;
                                                    if (Xaxis > 300)
                                                    {
                                                        Yaxis = getXYaxis[sigNames.Count - 1].YCoordinate + 50;
                                                        Xaxis = getXYaxis[0].XCoordinate;
                                                    }
                                                }
                                            }
                                            signer = new PdfSigner(reader, fileStream, stampProp.UseAppendMode());
                                        }
                                        PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                                            .SetLayer2Text(StrSignature)
                                            .SetImage(imageData).SetImageScale(-50)
                                            .SetReuseAppearance(false);
                                        iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(Xaxis, Yaxis, 180, 50);
                                        if (Xaxis == 0 && Yaxis == 0)
                                        {
                                            rect = new iText.Kernel.Geom.Rectangle(220, 15, 180, 50);

                                        }
                                        appearance
                                            .SetPageRect(rect)
                                            .SetPageNumber(Pageno);
                                        signer.SetFieldName(signer.GetNewSigFieldName());
                                        try
                                        {
                                            signer.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                            SingedFiles = SingedFiles + 1;
                                            ResponseMsg1.Message = Convert.ToString(SingedFiles) + " files Signed out of " + Convert.ToString(totalFiles) + " !";
                                            ResponseMsg1.Valid = true;
                                            ResponseMsgbullst.ResponseMessage = ResponseMsg1;
                                            isAnyFileSigned = true;
                                        }
                                        catch
                                        {
                                            reader.Close();
                                            if (fileStream != null)
                                            {
                                                fileStream.Close();
                                            }
                                            DigitalSignData filedata = new DigitalSignData();
                                            filedata.pdfpath = FileFullName;
                                            delData.Add(filedata);
                                            ResponseMsg1.Message = Path.GetFileName(fileforloop) + " !";
                                            ResponseMsg1.Valid = true;
                                            ResponseMsglist.Add(ResponseMsg1);
                                        }

                                        reader.Close();

                                    }
                                    catch
                                    {
                                        reader.Close();
                                        if (fileStream != null)
                                        {
                                            fileStream.Close();
                                        }
                                        DigitalSignData filedata = new DigitalSignData();
                                        filedata.pdfpath = FileFullName;
                                        delData.Add(filedata);

                                    }
                                });
                            }


                        }
                    }
                    else if (Path.GetExtension(filename) == ".docx" || Path.GetExtension(filename) == ".doc")
                    {
                        String DocfileName = Path.GetFileNameWithoutExtension(filename);
                        NewFileName = System.IO.Path.GetTempPath() + "\\" + DocfileName + ".pdf";

                        if (NewFileName.Length > 255)
                        {
                            ResponseMsg1.Message = "FileName too Large :-" + DocfileName;
                            ResponseMsg1.Valid = false;
                            ResponseMsglist.Add(ResponseMsg1);
                        }
                        else
                        {
                            helper.ConvertPDF(filename, NewFileName, WdSaveFormat.wdFormatPDF);

                        }

                        goto nextfile;
                    }
                }

                try
                {
                    foreach (var file in delData)
                    {
                        if (file.pdfpath != "")
                        {
                            FileInfo fi = new FileInfo(file.pdfpath);
                            if (fi.Length == 0)
                            {
                                File.Delete(file.pdfpath);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLog.LogErrorToFile(ex);
                }
                ResponseMsgbullst.ResponseMessagelst = ResponseMsglist;
                if (isAnyFileSigned) await SaveDigitalSignedDataToAnalytics(saveDigitalSignInfo);
                return ResponseMsgbullst;
            }
            catch (Exception ex)
            {
                ResponseMsg.Message = "Error Occured in Signing Document " + ex.Message;
                ResponseMsg.Valid = false;
                ErrorLog.LogErrorToFile(ex);
                ResponseMsgbullst.ResponseMessage = ResponseMsg;
                return ResponseMsgbullst;
            }
        }

        public async Task<ResponseMessage> ByteDigitalSignAsync(List<DigitalSignData> reqData)
        {
            string message = null;
            DTOSaveDigitalSignInfo saveDigitalSignInfo = new DTOSaveDigitalSignInfo();
            ResponseMessage ResponseMsg = new ResponseMessage();
            var headers = WebOperationContext.Current?.IncomingRequest?.Headers;

            string origin = headers?["Origin"];
            string referer = headers?["Referer"];
            try
            {
                string ThumbPrint = reqData.First().Thumbprint;

                X509Certificate2Collection certCollection = new X509Certificate2Collection();

                X509Certificate2Collection fcol = new X509Certificate2Collection();
                fcol = await helper.GetCertificates();

                X509Certificate2 selectedCertificate = fcol.Cast<X509Certificate2>().FirstOrDefault(cert => cert.Thumbprint.Equals(ThumbPrint, StringComparison.OrdinalIgnoreCase));

                if (selectedCertificate == null)
                {
                    ResponseMsg.Message = "No Certificate found !";
                    ResponseMsg.Valid = false;
                    return ResponseMsg;
                }
                certCollection.Add(selectedCertificate);


                if (certCollection.Count == 0)
                {
                    ResponseMsg.Message = "Thumbprint not matched !";
                    ResponseMsg.Valid = false;
                    return ResponseMsg;
                }

                X509Certificate2 cert1 = certCollection[0];

                if (DateTime.Now > cert1.NotAfter)
                {
                    ResponseMsg.Message = "Token Expired !";
                    ResponseMsg.Valid = false;
                    return ResponseMsg;
                }



                int Xaxis = reqData.First().XCoordinate;
                int Yaxis = reqData.First().YCoordinate;
                string pathss = reqData.First().pdfpath;
                string CustomText = reqData.First().CustomText;
                ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                WebClient client = new WebClient();
                byte[] pdfBytes = client.DownloadData(pathss);


                saveDigitalSignInfo.SignedDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
                var PublicKey = await GetPublicKey();
                byte[] textBytes = Encoding.UTF8.GetBytes(PublicKey.Public_Key);
                saveDigitalSignInfo.PublicKey = Convert.ToBase64String(textBytes);
                saveDigitalSignInfo.ValidToken = PublicKey.TokenValid;
                saveDigitalSignInfo.ValidFrom = PublicKey.ValidFrom;
                saveDigitalSignInfo.ValidTo = PublicKey.ValidTo;
                saveDigitalSignInfo.OriginForSign = origin;
                saveDigitalSignInfo.RefererForSign = referer;
                saveDigitalSignInfo.DocumentName = Path.GetFileName(pathss);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (var inputPdfStream = new MemoryStream(pdfBytes))
                    {

                        PdfReader reader = new PdfReader(inputPdfStream);
                        IExternalSignature es = new X509Certificate2Signature(cert1, "SHA-1", ref message);

                        if (message != null)
                        {
                            ResponseMsg.Message = message;
                            ResponseMsg.Valid = false;
                            return ResponseMsg;
                        }
                        else
                        {
                            if (es.GetEncryptionAlgorithm() != null)
                            {
                                Org.BouncyCastle.X509.X509CertificateParser cp1 = new Org.BouncyCastle.X509.X509CertificateParser();

                                Org.BouncyCastle.X509.X509Certificate[] chain3 = new[] { cp1.ReadCertificate(cert1.RawData) };

                                await System.Threading.Tasks.Task.Run(() =>
                                {
                                    StampingProperties stampProp = new StampingProperties();
                                    stampProp.PreserveEncryption();
                                    ImageData imageData = null;

                                    using (StreamReader sr = new StreamReader(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + @"\DigitalSignWT.png"))
                                    {
                                        imageData = ImageDataFactory.Create(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DigitalSignWT.png");
                                    }

                                    string[] SubjectSplit = cert1.Subject.Split(',');
                                    string StrName = "";
                                    string StrICNo = "";
                                    string StrRank = "";
                                    for (int i = 0; i < SubjectSplit.Length; i++)
                                    {
                                        if (SubjectSplit[i].Contains("SERIALNUMBER="))
                                            StrICNo = SubjectSplit[i].ToString().Replace("SERIALNUMBER=", "").Trim();
                                        if (SubjectSplit[i].Contains("CN="))
                                            StrName = SubjectSplit[i].ToString().Replace("CN=", "").Trim();
                                        if (SubjectSplit[i].Contains("T="))
                                            StrRank = SubjectSplit[i].ToString().Replace("T=", "").Trim();
                                    }
                                    inputPdfStream.Position = 0;

                                    saveDigitalSignInfo.SerialNo = StrICNo;

                                    iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(new PdfReader(inputPdfStream));
                                    SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
                                    IList<string> sigNames = signatureUtil.GetSignatureNames();
                                    iText.Kernel.Font.PdfFont font = PdfFontFactory.CreateFont(FontProgramFactory.CreateFont(StandardFonts.TIMES_BOLD));
                                    String StrSignature = "";
                                    if (CustomText != "")
                                        StrSignature = CustomText + "\n\n Digitally Signed by \n " + StrRank + " " + StrName + " \n Date : " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") + " \n © Hastakshar SEWA, DGIS";
                                    else
                                        StrSignature = "Digitally Signed by \n " + StrRank + " " + StrName + " \n Date : " + DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss") + " \n © Hastakshar SEWA, DGIS";

                                    try
                                    {
                                        HelperCert helperCert = new HelperCert();
                                        var getXYaxis = helperCert.GetSignatureCordinate(pathss);
                                        if (getXYaxis != null)
                                        {
                                            if (sigNames.Count % 2 == 0)
                                            {
                                                Yaxis = getXYaxis[sigNames.Count - 1].YCoordinate + 50;
                                                Xaxis = getXYaxis[0].XCoordinate;
                                            }
                                            else
                                            {
                                                Yaxis = getXYaxis[sigNames.Count - 1].YCoordinate;
                                                Xaxis = getXYaxis[sigNames.Count - 1].XCoordinate + 200;
                                                if (Xaxis > 300)
                                                {
                                                    Yaxis = getXYaxis[sigNames.Count - 1].YCoordinate + 50;
                                                    Xaxis = getXYaxis[0].XCoordinate;
                                                }
                                            }
                                        }

                                        PdfSigner signer = new PdfSigner(reader, ms, new StampingProperties());
                                        PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                                            .SetLayer2Text(StrSignature)
                                            .SetImage(imageData).SetImageScale(-50)
                                            .SetReuseAppearance(false);
                                        iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(Xaxis, Yaxis, 180, 50);
                                        appearance
                                            .SetPageRect(rect)
                                            .SetPageNumber(1);
                                        signer.SetFieldName(signer.GetNewSigFieldName());

                                        signer.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                    }
                                    catch
                                    {
                                        ResponseMsg.Message = "No Docu Sign !";
                                        ResponseMsg.Valid = false;
                                    }
                                });
                            }


                        }

                    }
                    byte[] byteArray = ms.ToArray();
                    string base64String = Convert.ToBase64String(byteArray);
                    ResponseMsg.Message = base64String;
                    ResponseMsg.Valid = true;
                   // await SaveDigitalSignedDataToAnalytics(saveDigitalSignInfo);
                    return ResponseMsg;
                }
            }
            catch (Exception ex)
            {
                ResponseMsg.Message = "Error Occured in Signing Document " + ex.Message;
                ResponseMsg.Valid = false;
                ErrorLog.LogErrorToFile(ex);
                return ResponseMsg;
            }
        }

        public async Task<bool> HasInternetConnectionAsyncTest()
        {
            try
            {
                return await helper.HasInternetConnectionAsyncTest();
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
                return false;
            }
        }



        public string SignHash(string message)
        {
            string status = null;
            if (message == null)
            {
                return "No value recived for Digital Signature";
            }
            try
            {
                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                X509Certificate2Collection fcollection = new X509Certificate2Collection();
                store.Open(OpenFlags.OpenExistingOnly);

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

                    }
                }
                store.Close();

                if (fcollection.Count == 0)
                {
                    return "No Token Found !";
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }
                    X509Certificate2 certificate = cert1;
                    Console.WriteLine("Public Key: {0}{1}", cert1.PublicKey.Key.ToXmlString(false), Environment.NewLine);

                    RSACryptoServiceProvider csp = (RSACryptoServiceProvider)certificate.PrivateKey;

                    byte[] data = new ASCIIEncoding().GetBytes(message);
                    byte[] hash = new SHA1Managed().ComputeHash(data);

                    string response = Convert.ToBase64String(csp.SignHash(hash, CryptoConfig.MapNameToOID("SHA-256")));

                    return response;

                }
            }

            catch (ArgumentOutOfRangeException ex)
            {
                ErrorLog.LogErrorToFile(ex);
                return ex.Message;
            }
        }
        #region Xml Signature Verification
        public List<DigitalVerifyDetails> VerifySignXml(XmlElement data)
        {
            List<DigitalVerifyDetails> signers = new List<DigitalVerifyDetails>();
            DigitalVerifyDetails digitalVerifyDetails = new DigitalVerifyDetails();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(data.OuterXml);
                string plainText = doc.InnerXml;


                xmlDoc.LoadXml(plainText);
                string digital = "DigitalSignature";
                int signatureCount = CountSignatureElements(xmlDoc);
                if (signatureCount > 0)
                {
                    for (int i = 1; i <= signatureCount; i++)
                    {
                        XmlDocument xmlDoc1 = new XmlDocument();
                        string tagdigital = digital + i;
                        XmlElement childNodes = (XmlElement)xmlDoc.SelectSingleNode("//" + tagdigital);
                        if (childNodes != null)
                        {
                            digitalVerifyDetails = DigitalVerify(childNodes, i);
                            signers.Add(digitalVerifyDetails);
                        }
                        else
                        {
                            digitalVerifyDetails = DigitalVerify(xmlDoc.DocumentElement, i);
                            signers.Add(digitalVerifyDetails);
                        }
                    }
                }
                else
                {
                    digitalVerifyDetails.IsVerified = false;
                    digitalVerifyDetails.SignatureRemarks = "Xml Not Signature";
                    digitalVerifyDetails.IsDigest = false;
                    digitalVerifyDetails.DigestRemarks = "Reference digest is Invalid";
                    signers.Add(digitalVerifyDetails);
                }
            }
            catch (Exception ex)
            {
                digitalVerifyDetails.IsVerified = false;
                digitalVerifyDetails.SignatureRemarks = "Invalid";
                digitalVerifyDetails.IsDigest = false;
                digitalVerifyDetails.DigestRemarks = "digest is Invalid";

                ErrorLog.LogErrorToFile(ex);
            }
            return signers;
        }
        public static int CountSignatureElements(XmlDocument xmlDoc)
        {

            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

            XmlNodeList signatureNodes = xmlDoc.SelectNodes("//ds:Signature", nsMgr);

            return signatureNodes.Count;
        }
        public DigitalVerifyDetails DigitalVerify(XmlElement data, int count)
        {
            DigitalVerifyDetails ret = new DigitalVerifyDetails();
            try
            {

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                string ss = data.OuterXml.Replace(" />", "/>");
                xmlDoc.LoadXml(ss);

                XmlDocument xmldigest = new XmlDocument();
                xmldigest.PreserveWhitespace = true;
                xmldigest.LoadXml(data.OuterXml);

                XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsMgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

                XmlNodeList signatureNode = xmldigest.SelectNodes("//ds:Signature", nsMgr);

                if (signatureNode != null)
                {
                    int lastsigncount = 1;
                    foreach (XmlNode node in signatureNode)
                    {
                        if (node is XmlElement element)
                        {
                            if (lastsigncount == count)
                                node.ParentNode.RemoveChild(node);
                        }
                        lastsigncount++;
                    }

                }

                XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
                nsManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

                XmlNodeList signatureElement1 = xmlDoc.SelectNodes("//ds:Signature", nsManager);
                XmlElement signatureElement = null;
                int countsign = 1;
                foreach (XmlNode node in signatureElement1)
                {
                    if (node is XmlElement element)
                    {
                        if (countsign == count)
                            signatureElement = element;
                    }
                    countsign++;



                }

                if (signatureElement == null)
                {
                    ret.IsVerified = false;
                    ret.SignatureRemarks = "Signature " + count + " element not found in the document";
                }

                SignedXml signedXml = new SignedXml(xmlDoc);

                signedXml.LoadXml(signatureElement);

                bool isSignatureValid = signedXml.CheckSignature();
                if (isSignatureValid)
                {
                    ret.IsVerified = isSignatureValid;
                    ret.SignatureRemarks = "Signature " + count + " is Verifed";
                    List<X509Certificate2> certificates = new List<X509Certificate2>();
                    XmlNodeList certificateNodes = xmlDoc.GetElementsByTagName("X509Certificate");
                    foreach (XmlNode node in certificateNodes)
                    {
                        string base64EncodedCertificate = node.InnerText;
                        byte[] certBytes = Convert.FromBase64String(base64EncodedCertificate);
                        X509Certificate2 certificate = new X509Certificate2(certBytes);
                        certificates.Add(certificate);

                        var subdata = certificate.Subject.Split(',');

                        string StrName = "";
                        string StrICNo = "";
                        string StrRank = "";
                        for (int i = 0; i < subdata.Length; i++)
                        {
                            if (subdata[i].Contains("SERIALNUMBER="))
                                StrICNo = subdata[i].ToString().Replace("SERIALNUMBER=", "").Trim();
                            if (subdata[i].Contains("CN="))
                                StrName = subdata[i].ToString().Replace("CN=", "").Trim();
                            if (subdata[i].Contains("T="))
                                StrRank = subdata[i].ToString().Replace("T=", "").Trim();
                        }

                        ret.SignatureBy = StrICNo + " (" + StrName + ") ";


                    }
                }
                else
                {
                    ret.IsVerified = isSignatureValid;
                    ret.SignatureRemarks = "Signature " + count + " is Not Verifed: ";
                }

                foreach (Reference reference in signedXml.SignedInfo.References)
                {

                    if (string.IsNullOrEmpty(reference.Uri))
                    {

                        XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
                        transform.LoadInput(xmlDoc);

                        byte[] canonicalizedData = GetCanonicalizedBytes(xmldigest);

                        byte[] computedDigest;
                        using (System.Security.Cryptography.HashAlgorithm hashAlg = System.Security.Cryptography.HashAlgorithm.Create(reference.DigestMethod))
                        {
                            computedDigest = hashAlg.ComputeHash(canonicalizedData);
                        }

                        bool digestValid = CompareByteArrays(computedDigest, reference.DigestValue);
                        if (digestValid == true)
                        {
                            ret.IsDigest = true;
                            ret.DigestRemarks = "Reference " + count + " digest is valid";
                        }
                        else
                        {
                            ret.IsDigest = false;
                            ret.DigestRemarks = "Reference " + count + " digest is Invalid because the computed digest differs from the digest in the XML";
                        }
                    }

                }

            }
            catch (Exception ex)
            {
                ret.IsVerified = false;
                if (ex.Message == "Invalid length for a Base-64 char array or string.")
                    ret.SignatureRemarks = "Signature X509Certificate Invalid";
                else
                    ret.SignatureRemarks = "Signature Invalid";
                ErrorLog.LogErrorToFile(ex);
            }

            return ret;
        }
        public static byte[] GetCanonicalizedBytes(XmlDocument xmlDoc)
        {

            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();


            transform.LoadInput(xmlDoc);

            using (Stream stream = (Stream)transform.GetOutput(typeof(Stream)))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }

        private static bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }
            return true;
        }
        #endregion

        #region Public Key

        public async Task<TokenDetails> GetPublicKey()
        {
            TokenDetails TokenDetailList = new TokenDetails();
            try
            {
                X509Certificate2Collection fcollection = await helper.GetCertificates();

                if (fcollection.Count == 0)
                {
                    var TokenDetails = new TokenDetails
                    {
                        API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/GetPublicKey",
                        CRL_OCSPCheck = false,
                        Status = "404",
                        Remarks = "Token not detected. Please insert the IACA token and try again !"

                    };


                    return TokenDetails;
                }
                else
                {
                    X509Certificate2 cert1 = null;
                    if (fcollection.Count == 1)
                    {
                        cert1 = fcollection[0];
                    }
                    else if (fcollection.Count > 1)
                    {
                        cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
                    }

                    string[] SubjectSplit = cert1.Subject.Split(',');

                    string PersNo = "";
                    for (int i = 0; i < SubjectSplit.Length; i++)
                    {
                        if (SubjectSplit[i].Contains("SERIALNUMBER="))
                            PersNo = SubjectSplit[i].ToString().Replace("SERIALNUMBER=", "").Trim();
                    }

                    bool TokenValidity = false;
                    string Remark = "";
                    if (DateTime.Now <= cert1.NotAfter || IsLocalToken)
                    {
                        TokenValidity = true;
                        Remark = "Personal No of Unique Cert is fetched for the inserted Token";
                    }
                    else
                    {

                        TokenValidity = false;
                        Remark = "The certificate on the inserted token has expired. Please use a token with a valid certificate and try again!";
                    }


                    if (!string.IsNullOrEmpty(PersNo))
                    {
                        RSA rsa = cert1.GetRSAPublicKey();
                        string xmlPublicKey = rsa.ToXmlString(false);
                        var TokenDetails = new TokenDetails
                        {

                            API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/GetPublicKey",
                            CRL_OCSPCheck = false,
                            subject = cert1.Subject,
                            issuer = null,
                            Thumbprint = cert1.Thumbprint,
                            ValidFrom = cert1.NotBefore.ToString(),
                            ValidTo = cert1.NotAfter.ToString(),
                            Status = "200",
                            Remarks = Remark,
                            TokenValid = TokenValidity,
                            Public_Key = xmlPublicKey,

                        };

                        return TokenDetails;
                    }
                    else
                    {
                        throw new Exception("Personal No is Empty. Pl report and try with different Token");
                    }

                }
            }
            catch (Exception ex)
            {

                var TokenDetails = new TokenDetails
                {
                    API = "https://dgisapp.army.mil:55102/Temporary_Listen_Addresses/FetchUniqueTokenDetails",
                    CRL_OCSPCheck = false,
                    Status = "500",
                    Remarks = "Exception Occured-" + ex.Message.ToString()

                };

                ErrorLog.LogErrorToFile(ex);
                return TokenDetails;
            }
        }

        public string Getpdffile()
        {

            if (CustomSignCordinate.PdfFile != null)
            {

                if (File.Exists(CustomSignCordinate.PdfFile))
                {
                    FileInfo fi = new FileInfo(CustomSignCordinate.PdfFile);
                    byte[] pdfBytes = File.ReadAllBytes(CustomSignCordinate.PdfFile);
                    string base64String = Convert.ToBase64String(pdfBytes);

                    return base64String;
                }

            }



            return null;
        }

        public int PdfCordinatefile(DTOCustomSignCordinate customSignCordinate)
        {

            CustomSignCordinate.X = customSignCordinate.X;
            CustomSignCordinate.Y = customSignCordinate.Y;
            CustomSignCordinate.PageNo = customSignCordinate.PageNo;

            if (customSignCordinate.X > 0)
            {
                return 1;
            }
            else if (CustomSignCordinate.X <= 0)
            {
                CustomSignCordinate.UpdatedOn = DateTime.Now;
                return -1;
            }
            return -1;
        }

        public async Task<ResponseMessage> AsymmetricEncryption(List<AsymmetricEncryptionData> reqData)
        {
            await System.Threading.Tasks.Task.Yield();
            ResponseMessage responseMessage = new ResponseMessage();
            string[] files = null;
            string Download = "";
            if (!string.IsNullOrEmpty(reqData.First().FolderLoc))
            {
                files = Directory.GetFiles(reqData.First().FolderLoc);
                Download = reqData.First().FolderLoc;
            }
            else if (!string.IsNullOrEmpty(reqData.First().FilePath))
            {
                files = new string[] { reqData.First().FilePath };
                Download = Path.GetDirectoryName(reqData.First().FilePath);
            }
            else
            {
                responseMessage.Message = "Please provide asymmetric encryption for files or folders.";
                responseMessage.Valid = false;
                return responseMessage;
            }

            int totalFiles = files.Count();

            int processedFiles = 0;
            foreach (string filename in files)
            {
            nextfile:
                string fileforloop = filename;

                FileInfo fi = new FileInfo(fileforloop);


                if (fi.Extension == ".mil")
                {
                    responseMessage.Message = "File is already encrypted.";
                    responseMessage.Valid = false;
                    processedFiles++;
                    continue;
                }

                byte[] magicHeader = Encoding.UTF8.GetBytes("ASDC_AESGCM256");
                string Output = Download + "\\" + fi.Name + "_RSA_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + ".mil";
                if (!string.IsNullOrWhiteSpace(reqData.First().Publickey))
                    EncryptFile(fileforloop, Output, reqData.First().Publickey, magicHeader);

                processedFiles++;


            }
            if (processedFiles == totalFiles)
            {

                responseMessage.Message = "Congratulations! Document is successfully Encrypted.";
                responseMessage.Valid = true;

            }
            else
            {
                responseMessage.Message = "An error occurred.";
                responseMessage.Valid = false;
            }
            return responseMessage;
        }

        public async Task<ResponseMessage> AsymmetricDencryption(List<AsymmetricEncryptionData> reqData)
        {
            ResponseMessage responseMessage = new ResponseMessage();

            string[] files = null;
            string Download = "";
            if (!string.IsNullOrEmpty(reqData.First().FolderLoc))
            {
                files = Directory.GetFiles(reqData.First().FolderLoc);
                Download = reqData.First().FolderLoc;
            }
            else if (!string.IsNullOrEmpty(reqData.First().FilePath))
            {
                files = new string[] { reqData.First().FilePath };
                Download = Path.GetDirectoryName(reqData.First().FilePath);
            }
            else
            {
                responseMessage.Message = "Please provide dencryption for files or folders.";
                responseMessage.Valid = false;
                return responseMessage;
            }


            int totalFiles = files.Count();

            int processedFiles = 0;
            int ret1=0;
            X509Certificate2Collection fcollection = await helper.GetCertificates();
            X509Certificate2 cert1 = null;
            if (fcollection.Count == 1)
            {
                cert1 = fcollection[0];
            }
            else if (fcollection.Count > 1)
            {
                cert1 = X509Certificate2UI.SelectFromCollection(fcollection, "Caption", "Message", X509SelectionFlag.SingleSelection)[0];
            }
            foreach (string filename in files)
            {
            nextfile:
                string fileforloop = filename;

                FileInfo fi = new FileInfo(fileforloop);


                if (fi.Extension != ".mil")
                {
                    responseMessage.Message = "File is not encrypted.";
                    responseMessage.Valid = false;
                    processedFiles++;
                    continue;
                }

                if (fcollection.Count == 0)
                {
                    responseMessage.Message = "Token Not Found.";
                    responseMessage.Valid = false;
                    return responseMessage;
                }
                else
                {
                    string filePath = Download + "\\" + fi.Name.Split('.')[0] + "_DEC_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + "." + betweenStrings(fi.Name, ".", "_");


                    if (cert1 != null)
                    {
                        string macDetails;   // declare variable first

                         ret1 = Service1.DecryptFile(fileforloop, filePath, cert1, out macDetails);
                       // ret1 = Service1.DecryptFile(fileforloop, filePath, cert1);

                    }
                   
                    if (ret1 ==0)
                    {
                        responseMessage.Message = "Wrong Token Inserted Does Not Match Private Key.";
                        responseMessage.Valid = false;
                        return responseMessage;
                    }
                    else
                    {
                        processedFiles++;
                    }


                }
            }
            if (processedFiles == totalFiles)
            {

                responseMessage.Message = "Congratulations! Document is successfully Decrypted.\n";
                responseMessage.Valid = true;
                return responseMessage;


            }
            else
            {
                responseMessage.Message = "Congratulations! Document is successfully Partial Decrypted.\n";
                responseMessage.Valid = true;
                return responseMessage;

            }
            return responseMessage;
        }
        public static String betweenStrings(String text, String start, String end)
        {
            int p1 = text.IndexOf(start) + start.Length;
            int p2 = text.IndexOf(end, p1);

            if (end == "") return (text.Substring(p1));
            else return text.Substring(p1, p2 - p1) + "";
        }
        public static bool EncryptFile(string inputFile, string outputFile, string rsaKeyXml, byte[] magicHeader, string macAddress = null)
        {
            const int BufferSize = 1024 * 1024; // 1 MB buffer
            bool outputFileStarted = false;

            try
            {
                if (string.IsNullOrWhiteSpace(inputFile))
                    throw new ArgumentException("Input file path is required.", nameof(inputFile));

                if (string.IsNullOrWhiteSpace(outputFile))
                    throw new ArgumentException("Output file path is required.", nameof(outputFile));

                if (!File.Exists(inputFile))
                    throw new FileNotFoundException("Input file was not found.", inputFile);

                string fullInputPath = Path.GetFullPath(inputFile);
                string fullOutputPath = Path.GetFullPath(outputFile);

                if (string.Equals(
                    fullInputPath,
                    fullOutputPath,
                    StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        "Input and output file paths cannot be the same.");
                }

                using (FileStream inputStream = new FileStream(
                    inputFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    BufferSize,
                    FileOptions.SequentialScan))
                {
                    /*
                     * Your existing encrypted payload stores the original file size
                     * as Int32. Therefore, this format supports files up to 2 GB.
                     */
                    if (inputStream.Length > int.MaxValue)
                    {
                        throw new NotSupportedException(
                            "The current encrypted-file format supports files up to 2 GB.");
                    }

                    using (Aes aes = Aes.Create())
                    {
                        aes.GenerateKey();
                        aes.GenerateIV();

                        byte[] encryptedKey;
                        byte[] encryptedIV;

                        using (RSA rsa = RSA.Create())
                        {
                            rsa.FromXmlString(rsaKeyXml);

                            encryptedKey = rsa.Encrypt(
                                aes.Key,
                                RSAEncryptionPadding.Pkcs1);

                            encryptedIV = rsa.Encrypt(
                                aes.IV,
                                RSAEncryptionPadding.Pkcs1);
                        }

                        using (FileStream outputStream = new FileStream(
                            outputFile,
                            FileMode.Create,
                            FileAccess.ReadWrite,
                            FileShare.None,
                            BufferSize))
                        {
                            outputFileStarted = true;

                            using (BinaryWriter outerWriter = new BinaryWriter(
                                outputStream,
                                Encoding.UTF8,
                                true))
                            {
                                // Write encrypted AES key.
                                outerWriter.Write(encryptedKey.Length);
                                outerWriter.Write(encryptedKey);

                                // Write encrypted AES IV.
                                outerWriter.Write(encryptedIV.Length);
                                outerWriter.Write(encryptedIV);

                                /*
                                 * Reserve four bytes for encrypted-data length.
                                 * The actual length will be written after encryption.
                                 */
                                long encryptedLengthPosition = outputStream.Position;
                                outerWriter.Write(0);
                                outerWriter.Flush();

                                long encryptedDataStartPosition = outputStream.Position;

                                using (ICryptoTransform encryptor = aes.CreateEncryptor())
                                using (CryptoStream cryptoStream = new CryptoStream(
                                    outputStream,
                                    encryptor,
                                    CryptoStreamMode.Write,
                                    true))
                                {
                                    using (BinaryWriter payloadWriter = new BinaryWriter(
                                        cryptoStream,
                                        Encoding.UTF8,
                                        true))
                                    {
                                        bool hasMac =
                                            !string.IsNullOrWhiteSpace(macAddress);

                                        payloadWriter.Write(hasMac);

                                        if (hasMac)
                                        {
                                            byte[] macBytes =
                                                Encoding.UTF8.GetBytes(macAddress);

                                            payloadWriter.Write(macBytes.Length);
                                            payloadWriter.Write(macBytes);
                                        }

                                        /*
                                         * Maintain compatibility with your existing
                                         * decryption format.
                                         */
                                        payloadWriter.Write((int)inputStream.Length);

                                        byte[] buffer = new byte[BufferSize];
                                        int bytesRead;

                                        while ((bytesRead = inputStream.Read(
                                            buffer,
                                            0,
                                            buffer.Length)) > 0)
                                        {
                                            payloadWriter.Write(
                                                buffer,
                                                0,
                                                bytesRead);
                                        }

                                        payloadWriter.Flush();
                                    }

                                    cryptoStream.FlushFinalBlock();
                                }

                                long encryptedDataEndPosition = outputStream.Position;

                                long encryptedDataLength =
                                    encryptedDataEndPosition -
                                    encryptedDataStartPosition;

                                if (encryptedDataLength > int.MaxValue)
                                {
                                    throw new NotSupportedException(
                                        "Encrypted data exceeds the supported file-format size.");
                                }

                                /*
                                 * Return to the reserved position and write the actual
                                 * encrypted-data length.
                                 */
                                outputStream.Position = encryptedLengthPosition;
                                outerWriter.Write((int)encryptedDataLength);
                                outerWriter.Flush();

                                // Return to the end before closing the stream.
                                outputStream.Position = encryptedDataEndPosition;
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);

                // Remove incomplete encrypted output.
                if (outputFileStarted)
                {
                    try
                    {
                        if (File.Exists(outputFile))
                            File.Delete(outputFile);
                    }
                    catch
                    {
                        // Avoid replacing the original encryption exception.
                    }
                }

                return false;
            }
            //try
            //{
            //    byte[] fileData = File.ReadAllBytes(inputFile);

            //    using (Aes aes = Aes.Create())
            //    {
            //        aes.GenerateKey();
            //        aes.GenerateIV();

            //        byte[] encryptedData;

            //        using (MemoryStream ms = new MemoryStream())
            //        using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
            //        using (BinaryWriter bw = new BinaryWriter(cs, Encoding.UTF8, true))
            //        {
            //            bool hasMac = !string.IsNullOrWhiteSpace(macAddress);
            //            bw.Write(hasMac);

            //            if (hasMac)
            //            {
            //                byte[] macBytes = Encoding.UTF8.GetBytes(macAddress);
            //                bw.Write(macBytes.Length);
            //                bw.Write(macBytes);
            //            }

            //            bw.Write(fileData.Length);
            //            bw.Write(fileData);

            //            bw.Flush();
            //            cs.FlushFinalBlock();

            //            encryptedData = ms.ToArray();
            //        }


            //        RSA rsa = RSA.Create();
            //        rsa.FromXmlString(rsaKeyXml);

            //        byte[] encryptedKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.Pkcs1);
            //        byte[] encryptedIV = rsa.Encrypt(aes.IV, RSAEncryptionPadding.Pkcs1);

            //        // Save encrypted AES key, IV, and file data
            //        using (FileStream fs = new FileStream(outputFile, FileMode.Create))
            //        using (BinaryWriter writer = new BinaryWriter(fs))
            //        {
            //            writer.Write(encryptedKey.Length);
            //            writer.Write(encryptedKey);
            //            writer.Write(encryptedIV.Length);
            //            writer.Write(encryptedIV);
            //            writer.Write(encryptedData.Length);
            //            writer.Write(encryptedData);
            //        }
            //    }
            //    return true;
            //}
            //catch (Exception ex)
            //{
            //    ErrorLog.LogErrorToFile(ex);
            //    return false;
            //}


        }
        public static int DecryptFile(string encryptedFile, string outputFile, X509Certificate2 privateCert, out string macDetails)
        {
            const int BufferSize = 1024 * 1024; // 1 MB
            const long MaximumAllowedFileSize = 500L * 1024L * 1024L;

            macDetails = string.Empty;

            string actualOutputFile = null;
            bool outputFileStarted = false;

            try
            {
                if (string.IsNullOrWhiteSpace(encryptedFile))
                    throw new ArgumentException(
                        "Encrypted file path is required.",
                        nameof(encryptedFile));

                if (string.IsNullOrWhiteSpace(outputFile))
                    throw new ArgumentException(
                        "Output file path is required.",
                        nameof(outputFile));

                if (!File.Exists(encryptedFile))
                    throw new FileNotFoundException(
                        "Encrypted file was not found.",
                        encryptedFile);

                if (privateCert == null)
                    throw new ArgumentNullException(nameof(privateCert));

                if (!privateCert.HasPrivateKey)
                    throw new CryptographicException(
                        "The selected certificate does not contain a private key.");

                using (FileStream encryptedStream = new FileStream(
                    encryptedFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    BufferSize,
                    FileOptions.SequentialScan))
                using (BinaryReader headerReader = new BinaryReader(
                    encryptedStream,
                    Encoding.UTF8,
                    true))
                {
                    /*
                     * Read RSA-encrypted AES key.
                     * This is small, normally 256 or 512 bytes.
                     */
                    int keyLength = headerReader.ReadInt32();

                    ValidateHeaderLength(
                        keyLength,
                        "encrypted AES key",
                        1,
                        16 * 1024);

                    byte[] encryptedKey = ReadExactBytes(
                        headerReader,
                        keyLength,
                        "encrypted AES key");

                    /*
                     * Read RSA-encrypted AES IV.
                     */
                    int ivLength = headerReader.ReadInt32();

                    ValidateHeaderLength(
                        ivLength,
                        "encrypted AES IV",
                        1,
                        16 * 1024);

                    byte[] encryptedIV = ReadExactBytes(
                        headerReader,
                        ivLength,
                        "encrypted AES IV");

                    /*
                     * Read encrypted payload length.
                     *
                     * Do not call ReadBytes(dataLength), because that would load
                     * the complete 500 MB encrypted file into memory.
                     */
                    int encryptedDataLength = headerReader.ReadInt32();

                    if (encryptedDataLength <= 0)
                    {
                        throw new InvalidDataException(
                            "The encrypted payload length is invalid.");
                    }

                    long remainingEncryptedBytes =
                        encryptedStream.Length - encryptedStream.Position;

                    if (remainingEncryptedBytes != encryptedDataLength)
                    {
                        throw new InvalidDataException(
                            "The encrypted file is incomplete, corrupted, or has an invalid payload length.");
                    }

                    using (RSA rsa = privateCert.GetRSAPrivateKey())
                    {
                        if (rsa == null)
                        {
                            throw new CryptographicException(
                                "No RSA private key was found in the certificate.");
                        }

                        byte[] aesKey = rsa.Decrypt(
                            encryptedKey,
                            RSAEncryptionPadding.Pkcs1);

                        byte[] aesIV = rsa.Decrypt(
                            encryptedIV,
                            RSAEncryptionPadding.Pkcs1);

                        using (Aes aes = Aes.Create())
                        {
                            aes.Key = aesKey;
                            aes.IV = aesIV;

                            using (ICryptoTransform decryptor =
                                aes.CreateDecryptor())
                            using (CryptoStream cryptoStream = new CryptoStream(
                                encryptedStream,
                                decryptor,
                                CryptoStreamMode.Read,
                                true))
                            using (BinaryReader payloadReader = new BinaryReader(
                                cryptoStream,
                                Encoding.UTF8,
                                true))
                            {
                                /*
                                 * Read MAC information from the decrypted payload.
                                 */
                                bool hasMac = payloadReader.ReadBoolean();

                                if (hasMac)
                                {
                                    int macLength = payloadReader.ReadInt32();

                                    ValidateHeaderLength(
                                        macLength,
                                        "MAC address",
                                        1,
                                        4096);

                                    byte[] macBytes = ReadExactBytes(
                                        payloadReader,
                                        macLength,
                                        "MAC address");

                                    macDetails = Encoding.UTF8.GetString(macBytes);

                                    if (string.IsNullOrWhiteSpace(macDetails))
                                    {
                                        throw new InvalidDataException(
                                            "The encrypted file contains invalid MAC address information.");
                                    }
                                }

                                /*
                                 * Read original unencrypted file size.
                                 */
                                int originalFileLength =
                                    payloadReader.ReadInt32();

                                if (originalFileLength < 0)
                                {
                                    throw new InvalidDataException(
                                        "The original file length is invalid.");
                                }

                                if (originalFileLength >
                                    MaximumAllowedFileSize)
                                {
                                    throw new InvalidDataException(
                                        "The decrypted file exceeds the maximum allowed size of 500 MB.");
                                }

                                    actualOutputFile = outputFile;
                                
                                string encryptedFullPath =
                                    Path.GetFullPath(encryptedFile);

                                string outputFullPath =
                                    Path.GetFullPath(actualOutputFile);

                                if (string.Equals(
                                    encryptedFullPath,
                                    outputFullPath,
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    throw new InvalidOperationException(
                                        "The encrypted input file and decrypted output file cannot be the same.");
                                }

                                string outputDirectory =
                                    Path.GetDirectoryName(outputFullPath);

                                if (!string.IsNullOrWhiteSpace(outputDirectory) &&
                                    !Directory.Exists(outputDirectory))
                                {
                                    Directory.CreateDirectory(outputDirectory);
                                }

                                using (FileStream outputStream = new FileStream(
                                    outputFullPath,
                                    FileMode.Create,
                                    FileAccess.Write,
                                    FileShare.None,
                                    BufferSize))
                                {
                                    outputFileStarted = true;

                                    byte[] buffer = new byte[BufferSize];
                                    long remainingBytes = originalFileLength;

                                    /*
                                     * Copy decrypted bytes directly to the output file.
                                     *
                                     * Only the 1 MB buffer remains in memory.
                                     */
                                    while (remainingBytes > 0)
                                    {
                                        int bytesToRead = (int)Math.Min(
                                            buffer.Length,
                                            remainingBytes);

                                        int bytesRead = payloadReader.Read(
                                            buffer,
                                            0,
                                            bytesToRead);

                                        if (bytesRead <= 0)
                                        {
                                            throw new EndOfStreamException(
                                                "The encrypted file ended before the complete original file was decrypted.");
                                        }

                                        outputStream.Write(
                                            buffer,
                                            0,
                                            bytesRead);

                                        remainingBytes -= bytesRead;
                                    }

                                    /*
                                     * Read until the end of the CryptoStream.
                                     * This forces AES padding validation and helps
                                     * detect tampered or corrupted encrypted files.
                                     */
                                    int additionalBytes = payloadReader.Read(
                                        buffer,
                                        0,
                                        buffer.Length);

                                    if (additionalBytes > 0)
                                    {
                                        throw new InvalidDataException(
                                            "Unexpected additional data was found after the decrypted file.");
                                    }

                                    outputStream.Flush();
                                }

                                return hasMac ? 4 : 1;
                            }
                        }
                    }
                }
            }
            catch (CryptographicException ex)
            {
                DeleteIncompleteOutputFile(
                    actualOutputFile,
                    outputFileStarted);

                ErrorLog.LogErrorToFile(ex);

                // Invalid key, invalid certificate, tampered file,
                // corrupted AES data, or invalid padding.
                return 0;
            }
            catch (Exception ex)
            {
                DeleteIncompleteOutputFile(
                    actualOutputFile,
                    outputFileStarted);

                ErrorLog.LogErrorToFile(ex);
                return 0;
            }
            //macDetails = "";
            //try
            //{
            //    byte[] decryptedData;

            //    using (FileStream fs = new FileStream(encryptedFile, FileMode.Open))
            //    using (BinaryReader reader = new BinaryReader(fs))
            //    {
            //        int keyLength = reader.ReadInt32();
            //        byte[] encryptedKey = reader.ReadBytes(keyLength);

            //        int ivLength = reader.ReadInt32();
            //        byte[] encryptedIV = reader.ReadBytes(ivLength);

            //        int dataLength = reader.ReadInt32();
            //        byte[] encryptedData = reader.ReadBytes(dataLength);

            //        using (RSA rsa = privateCert.GetRSAPrivateKey())
            //        {
            //            if (rsa == null)
            //            {
            //                throw new Exception("No private key found in the certificate.");
            //            }

            //            byte[] aesKey = rsa.Decrypt(encryptedKey, RSAEncryptionPadding.Pkcs1);
            //            byte[] aesIV = rsa.Decrypt(encryptedIV, RSAEncryptionPadding.Pkcs1);

            //            using (Aes aes = Aes.Create())
            //            {
            //                aes.Key = aesKey;
            //                aes.IV = aesIV;

            //                string macAddress = null;
            //                string useraname = null;
            //                DateTime validityDate= new DateTime();

            //                using (MemoryStream ms = new MemoryStream())
            //                using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write))
            //                {
            //                    cs.Write(encryptedData, 0, encryptedData.Length);
            //                    cs.FlushFinalBlock();

            //                    ms.Position = 0;

            //                    using (BinaryReader br = new BinaryReader(ms, Encoding.UTF8))
            //                    {
            //                        bool hasMac = br.ReadBoolean();
            //                        macDetails = "";
            //                        if (hasMac)
            //                        {
            //                            int macLength = br.ReadInt32();
            //                            byte[] macBytes = br.ReadBytes(macLength);
            //                            string CanCat_macAddress = Encoding.UTF8.GetString(macBytes);
            //                            macDetails = CanCat_macAddress;

            //                        }

            //                        int fileLength = br.ReadInt32();
            //                        decryptedData = br.ReadBytes(fileLength);
            //                    }
            //                }
            //                if (!string.IsNullOrEmpty(macDetails))
            //                {


            //                        string DownloadPath = System.IO.Path.GetDirectoryName(outputFile);
            //                        FileInfo fi = new FileInfo(outputFile);
            //                        string fileName = fi.Name.Split('_')[0];
            //                        outputFile = DownloadPath+"\\"+ fileName+".pdf";
            //                        File.WriteAllBytes(outputFile, decryptedData);
            //                        return 4;
            //                }

            //            }
            //        }
            //    }

            //    File.WriteAllBytes(outputFile, decryptedData);
            //    return 1;
            //}
            //catch (Exception ex)
            //{
            //    ErrorLog.LogErrorToFile(ex);
            //    return 0;


            //}
        }
        private static byte[] ReadExactBytes(
    BinaryReader reader,
    int length,
    string fieldName)
        {
            byte[] data = reader.ReadBytes(length);

            if (data.Length != length)
            {
                throw new EndOfStreamException(
                    "The encrypted file ended while reading " +
                    fieldName + ".");
            }

            return data;
        }

        private static void ValidateHeaderLength(
            int length,
            string fieldName,
            int minimumLength,
            int maximumLength)
        {
            if (length < minimumLength ||
                length > maximumLength)
            {
                throw new InvalidDataException(
                    "The " + fieldName +
                    " length is invalid.");
            }
        }


        private static void DeleteIncompleteOutputFile(
            string outputFile,
            bool outputFileStarted)
        {
            if (!outputFileStarted ||
                string.IsNullOrWhiteSpace(outputFile))
            {
                return;
            }

            try
            {
                if (File.Exists(outputFile))
                    File.Delete(outputFile);
            }
            catch
            {
                // Do not replace the original decryption exception.
            }
        }
        public static bool ValidatePassword(string password)
        {
            const int MIN_LENGTH = 4;
            const int MAX_LENGTH = 18;

            if (password == null) throw new ArgumentNullException();

            bool meetsLengthRequirements = password.Length >= MIN_LENGTH && password.Length <= MAX_LENGTH;
            bool hasUpperCaseLetter = false;
            bool hasLowerCaseLetter = false;
            bool hasDecimalDigit = false;
            bool hasSpecialChar = false;

            if (meetsLengthRequirements)
            {
                int PasswordSpecialChar = password.Count(p => !char.IsLetterOrDigit(p));

                if (PasswordSpecialChar > 0)
                {
                    hasSpecialChar = true;
                }

                foreach (char c in password)
                {
                    if (char.IsUpper(c)) hasUpperCaseLetter = true;
                    else if (char.IsLower(c)) hasLowerCaseLetter = true;
                    else if (char.IsDigit(c)) hasDecimalDigit = true;
                }
            }

            bool isValid = meetsLengthRequirements;
            return isValid;

        }
        public async Task<ResponseMessage> SymmetricEncryption(SymmetricEncryptionData reqData)
        {
            await System.Threading.Tasks.Task.Yield();
            ResponseMessage responseMessage = new ResponseMessage();

            try
            {

                if (reqData.Password.ToString() == "")
                {
                    responseMessage.Message = "Please Enter Password for Encryption.";
                    responseMessage.Valid = false;
                    return responseMessage;

                }
                string[] files = null;
                string Download = "";
                if (!string.IsNullOrEmpty(reqData.FolderLoc))
                {
                    files = Directory.GetFiles(reqData.FolderLoc);
                    Download = reqData.FolderLoc;
                }
                else if (!string.IsNullOrEmpty(reqData.FilePath))
                {
                    files = new string[] { reqData.FilePath };
                    Download = Path.GetDirectoryName(reqData.FilePath);
                }
                else
                {
                    responseMessage.Message = "Please provide encryption for files or folders.";
                    responseMessage.Valid = false;
                    return responseMessage;
                }

                if (Service1.ValidatePassword(reqData.Password))
                {

                    string DownloadPath = "";
                    int totalFiles = files.Count();
                    int processedFiles = 0;

                    foreach (var path in files)
                    {

                        DownloadPath = System.IO.Path.GetDirectoryName(path);
                        FileInfo fi = new FileInfo(path);

                        if (fi.Extension == ".mil")
                        {
                            responseMessage.Message = "File is already encrypted.";
                            responseMessage.Valid = false;
                            processedFiles++;
                            continue;

                        }

                        FileStream stream = File.OpenRead(path);
                        byte[] bytes = new byte[stream.Length];
                        stream.Read(bytes, 0, bytes.Length);
                        stream.Close();

                        string rsaKeyXml = reqData.Password;

                        string Output = DownloadPath + "\\" + fi.Name + "_AES_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + ".mil";

                        byte[] encrypted = AesGcm256.SimpleEncryptWithPassword(bytes, reqData.Password.ToString());

                        using (Stream file = File.OpenWrite(DownloadPath + "\\" + fi.Name + "_AES_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + ".mil"))
                        {
                            file.Write(encrypted, 0, encrypted.Length);
                        }
                        processedFiles++;

                    }
                    if (processedFiles == totalFiles)
                    {

                        responseMessage.Message = "Congratulations! Document is successfully Encrypted.";
                        responseMessage.Valid = true;

                    }
                    else
                    {
                        responseMessage.Message = "An error occurred.";
                        responseMessage.Valid = false;
                    }
                }
                else
                {
                    responseMessage.Message = "Password Length should be between 4 to 16 Characters.";
                    responseMessage.Valid = false;

                }
                return responseMessage;
            }
            catch (Exception ex)
            {
                responseMessage.Message = ex.Message;
                responseMessage.Valid = false;
                return responseMessage;
            }
        }

        public async Task<ResponseMessage> SymmetricDencryption(SymmetricEncryptionData reqData)
        {
            await System.Threading.Tasks.Task.Yield();
            ResponseMessage responseMessage = new ResponseMessage();
            Aes myAes = Aes.Create();
            try
            {
                if (reqData.Password.ToString() == "")
                {
                    responseMessage.Message = "Please Enter Password for Encryption.";
                    responseMessage.Valid = false;
                    return responseMessage;

                }
                string[] files = null;
                string Download = "";
                if (!string.IsNullOrEmpty(reqData.FolderLoc))
                {
                    files = Directory.GetFiles(reqData.FolderLoc);
                    Download = reqData.FolderLoc;
                }
                else if (!string.IsNullOrEmpty(reqData.FilePath))
                {
                    files = new string[] { reqData.FilePath };
                    Download = Path.GetDirectoryName(reqData.FilePath);
                }
                else
                {
                    responseMessage.Message = "Please provide Dencryption for files or folders.";
                    responseMessage.Valid = false;
                    return responseMessage;
                }
                if (ValidatePassword(reqData.Password.ToString()))
                {

                    string Password = reqData.Password.ToString();
                    byte[] Mykey = null;

                    if (string.IsNullOrWhiteSpace(Password) || Password.Length < AesGcm256.MinPasswordLength)
                    {
                        responseMessage.Message = String.Format("Please enter password with atleast {0} characters as per ACSP-2017.", AesGcm256.MinPasswordLength);
                        responseMessage.Valid = false;
                        return responseMessage;
                    }

                    byte[] Hashbytes = Encoding.Unicode.GetBytes(Password);
                    SHA256Managed hashstring = new SHA256Managed();
                    Mykey = hashstring.ComputeHash(Hashbytes);


                    byte[] MyIV = Encoding.ASCII.GetBytes(Password.PadRight(16, ' '));

                    myAes.Key = Mykey;
                    myAes.IV = MyIV;
                    int processedFiles = 0;
                    int totalFiles = files.Count();
                    foreach (var path in files)
                    {

                        Download = System.IO.Path.GetDirectoryName(path);

                        FileInfo fi = new FileInfo(path);
                        if (fi.Extension == ".mil")
                        {
                            FileStream stream1 = File.OpenRead(path);
                            byte[] bytes1 = new byte[stream1.Length];
                            stream1.Read(bytes1, 0, bytes1.Length);

                            stream1.Close();

                            char dd = '_';
                            int levelOfEncryption = fi.FullName.Count(s => s == dd);

                            //string mac;
                            //byte[] roundtrip = AesGcm256.SimpleDecryptWithPassword(bytes1, reqData.Password.ToString(), out mac);

                            byte[] roundtrip = AesGcm256.SimpleDecryptWithPassword(bytes1, reqData.Password.ToString());
                            if (roundtrip == null)
                            {

                                responseMessage.Message = "Password incorrect !";
                                responseMessage.Valid = false;
                                return responseMessage;

                            }
                            else
                            {
                                string filePath = Download + "\\" + fi.Name.Split('.')[0] + "_DEC_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + "." + betweenStrings(fi.Name, ".", "_");


                                using (Stream file = File.OpenWrite(filePath))
                                {

                                    file.Write(roundtrip, 0, roundtrip.Length);
                                }

                                processedFiles++;
                            }







                        }
                        else
                        {
                            processedFiles++;
                        }

                    }
                    if (processedFiles == totalFiles)
                    {

                        responseMessage.Message = "Congratulations! Document is successfully Encrypted.";
                        responseMessage.Valid = true;

                    }
                    else
                    {

                        responseMessage.Message = "An error occurred.";
                        responseMessage.Valid = false;

                    }
                    return responseMessage;
                }
                else
                {
                    responseMessage.Message = "Password Length should be between 4 to 16 Characters.";
                    responseMessage.Valid = false;
                    return responseMessage;

                }
            }
            catch (Exception ex)
            {
                responseMessage.Message = ex.Message;
                responseMessage.Valid = false;
                return responseMessage;
            }
        }

        public async Task<ResponseMessage> AddWaterMarks(DtoWaterMarkData Data)
        {
            ResponseMessage responseMessage = new ResponseMessage();
            try
            {

                if (Data.Datetime == true || Data.IpAddress == true || Data.CustomText != "")
                {
                    return await upload(Data);
                }
                else
                {
                    responseMessage.Message = "Please send atleast one value for Watermarking";
                    responseMessage.Valid = false;
                    return responseMessage;
                }
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
                responseMessage.Message = ex.Message;
                responseMessage.Valid = false;
                return responseMessage;
            }
        }
        public async Task<ResponseMessage> upload(DtoWaterMarkData Data)
        {
            await System.Threading.Tasks.Task.Yield();
            ResponseMessage responseMessage = new ResponseMessage();
            string DownloadPath = "";

            String NewFileName = "";

            string WatermarkedPDFFileName = "";

            string[] files = null;
            string Download = "";
            if (!string.IsNullOrEmpty(Data.FolderLoc))
            {
                files = Directory.GetFiles(Data.FolderLoc);
                Download = Data.FolderLoc;
            }
            else if (!string.IsNullOrEmpty(Data.FilePath))
            {
                files = new string[] { Data.FilePath };
                Download = Path.GetDirectoryName(Data.FilePath);
            }
            else
            {
                responseMessage.Message = "Please provide Watermark for files or folders.";
                responseMessage.Valid = false;
                return responseMessage;
            }
            if (Data.CustomText == null)
                Data.CustomText = "";

            string WaterMarkingText = Data.CustomText;

            string[] stringArray = Data.CustomText.Split(',');

            int j = 0;

            foreach (var path in files)
            {
            nextfile:
                string fileforloop = path;
                DownloadPath = Path.GetDirectoryName(path);
                if (NewFileName != "")
                {
                    fileforloop = NewFileName;
                }
                else
                {
                    fileforloop = path;
                }
                if (fileforloop == null || fileforloop.Length == 0)
                {
                    responseMessage.Message = "No file uploaded.";
                    responseMessage.Valid = false;
                    return responseMessage;
                }

                FileInfo fi = new FileInfo(fileforloop);
                if (fi.Extension == ".pdf")
                {
                    foreach (string item in stringArray)
                    {
                        WaterMarkingText = item;
                        WatermarkedPDFFileName = DownloadPath + "\\" + fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length) + "_WM_" + WaterMarkingText + "_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + ".pdf";

                        PdfDocument pdfDoc = new PdfDocument(new PdfReader(fi.FullName), new PdfWriter(WatermarkedPDFFileName));

                        PdfCanvas under = new PdfCanvas(pdfDoc.GetFirstPage().NewContentStreamBefore(), new PdfResources(), pdfDoc);

                        PdfFont font = PdfFontFactory.CreateFont(FontProgramFactory.CreateFont(StandardFonts.TIMES_ROMAN));
                        iText.Layout.Element.Paragraph paragraph = new iText.Layout.Element.Paragraph("This watermark is added UNDER the existing content")
                                .SetFont(font)
                                .SetBold()
                                .SetFontColor(ColorConstants.RED)
                                .SetFontSize(48);

                        for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++) 
                        { 
                            PdfCanvas over = new PdfCanvas(pdfDoc.GetPage(i)); 
                            if (Data.Datetime == true && Data.IpAddress == false)
                            { 
                                paragraph = new iText.Layout.Element.Paragraph(DateTime.Now.ToString() + "\n" + WaterMarkingText) 
                                    .SetFont(font) 
                                  .SetFontColor(ColorConstants.RED) 
                                  .SetFontSize(68); 
                                over.SaveState(); 
                                PdfExtGState gs3 = new PdfExtGState(); 
                                gs3.SetFillOpacity(0.5f); 
                                over.SetExtGState(gs3); 
                                iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize()) 
                                        .ShowTextAligned(paragraph, 297, 450, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45); 
                                canvasWatermark.Close(); 
                            } 
                            else if (Data.IpAddress == true && Data.Datetime == false) 
                            { 
                                System.Net.IPAddress[] a = Dns.GetHostByName(Dns.GetHostName()).AddressList; 
                                string ip = a[0].ToString(); 
                                paragraph = new iText.Layout.Element.Paragraph(ip + "\n" + WaterMarkingText) 
                                   .SetFont(font) 
                                  .SetFontColor(ColorConstants.RED) 
                                  .SetFontSize(68); 
                                over.SaveState(); 

                                PdfExtGState gs3 = new PdfExtGState();

                                gs3.SetFillOpacity(0.5f);

                                over.SetExtGState(gs3);

                                iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize()) 
                                        .ShowTextAligned(paragraph, 297, 450, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45); 
                                canvasWatermark.Close();

                            } 
                            else if (Data.Datetime == true && Data.IpAddress == true) 
                            { 
                                System.Net.IPAddress[] a = Dns.GetHostByName(Dns.GetHostName()).AddressList; 
                                string ip = a[0].ToString(); 
                                paragraph = new iText.Layout.Element.Paragraph(DateTime.Now.ToString() + "\n" + ip + "\n" + WaterMarkingText) 
                                  .SetFont(font) 
                                  .SetFontColor(ColorConstants.RED) 
                                  .SetFontSize(68); 
                                over.SaveState(); 
                                PdfExtGState gs3 = new PdfExtGState(); 
                                gs3.SetFillOpacity(0.5f); 
                                over.SetExtGState(gs3); 
                                iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize()) 
                                        .ShowTextAligned(paragraph, 200, 450, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45); 
                                canvasWatermark.Close(); 
                            } 
                            else 
                            { 
                                paragraph = new iText.Layout.Element.Paragraph(WaterMarkingText) 
                                      .SetFont(font) 
                                      .SetFontColor(ColorConstants.RED) 
                                      .SetFontSize(68); 
                                over.SaveState(); 

                                PdfExtGState gs3 = new PdfExtGState(); 
                                gs3.SetFillOpacity(0.5f); 
                                over.SetExtGState(gs3); 

                                iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize()) 
                                        .ShowTextAligned(paragraph, 297, 450, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45);

                                canvasWatermark.Close(); 
                            } 
                            over.RestoreState(); 
                        } 
                        pdfDoc.Close(); 
                        NewFileName = ""; 
                    } 
                    j = j + 1;   
                } 
                else if (Path.GetExtension(path) == ".docx" || Path.GetExtension(path) == ".doc")
                {

                    String DocfileName = Path.GetFileNameWithoutExtension(path);

                    NewFileName = System.IO.Path.GetTempPath() + "\\" + DocfileName + ".pdf";

                    if (NewFileName.Length > 255)
                    {
                        responseMessage.Message = "FileName too long!."; 
                        goto nextfile;

                    }
                    else
                    {
                        helper.ConvertPDF(path, NewFileName, WdSaveFormat.wdFormatPDF);
                    } 
                    goto nextfile; 
                } 
                else 
                {
                    responseMessage.Message = "Please select only PDF/Doc document for WaterMarking.";
                    responseMessage.Valid = false;
                    return responseMessage;  
                } 
            } 

            if (j == files.Length) 
            { 
                string Result = "0"; 
                responseMessage.Message = "Congratulations ! \n\n Document is successfully WaterMarked.";
                responseMessage.Valid = true;
                return responseMessage;  
            }
            else 
            {
                responseMessage.Message = "some document not successfully WaterMarked.";
                responseMessage.Valid = false;
                return responseMessage; 
            }  
        }

        public ResponseMessage DigitalSignVerifyAsync(DigitalSignData Data)
        {
            bool NotModified = true;
            ResponseMessage responseMessage = new ResponseMessage();
            string DownloadPath = "";

            String NewFileName = "";

            string WatermarkedPDFFileName = "";

            string[] files = null;
            string Download = "";
            if (!string.IsNullOrEmpty(Data.FolderLoc))
            {
                files = Directory.GetFiles(Data.FolderLoc);
                Download = Data.FolderLoc;
            }
            else if (!string.IsNullOrEmpty(Data.pdfpath))
            {
                files = new string[] { Data.pdfpath };  
                Download = Path.GetDirectoryName(Data.pdfpath);
            }
            else
            {
                responseMessage.Message = "Please provide Verify for files or folders.";
                responseMessage.Valid = false;
                return responseMessage;
            }
            foreach (string filename in files)
            {
                int numValid = 0;
                int numinvalid = 0;
                string fileExtension = Path.GetExtension(filename).ToLower();
                PdfDocument pdfDocument = new PdfDocument(new PdfReader(filename));
                
                bool genuineAndWasNotModified = false;

                SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
                IList<string> sigNames = signatureUtil.GetSignatureNames();
                if (sigNames.Count == 0)
                {
                    responseMessage.Message = "Digital Signature not found.";
                    responseMessage.Valid = false;
                  
                    numinvalid = numinvalid + 1;
                    continue;
                }
                else
                {

                    foreach (string sigName in sigNames)
                    {
                        try
                        {
                            PdfPKCS7 signature1 = signatureUtil.VerifySignature(sigName);
                            var documentNotModifie = signatureUtil.SignatureCoversWholeDocument(sigName);
                            NotModified = documentNotModifie;
                            var cal = signature1.GetSignDate();
                            var pkc = signature1.GetCertificates();
                            pkc = signature1.GetSignCertificateChain();

                            var revocationValid = signature1.IsRevocationValid();



                            if (pkc[0].IsValidNow)
                            {
                                if (signature1 != null)
                                {
                                    genuineAndWasNotModified = signature1.VerifySignatureIntegrityAndAuthenticity();
                                    if (genuineAndWasNotModified)
                                    {
                                        numValid = numValid + 1;
                                    }
                                    else
                                    {
                                        numinvalid = numinvalid + 1;
                                    }

                                }
                            }
                            else if (!documentNotModifie)
                            {
                                responseMessage.Message = "The revision of the document that was covered by this signature has not been altered; however, there have been subsequent changes in the document.";
                                responseMessage.Valid = false;

                                pdfDocument.Close();

                            }
                            else
                            {
                                responseMessage.Message = "The Signer's identity is invalid because it has expired or is not yet valid.";
                                responseMessage.Valid = false;
                                pdfDocument.Close();

                            }
                        }
                        catch (Exception)
                        {
                            
                        }
                    }
                    if (numValid == sigNames.Count)
                    {
                        if (!NotModified)
                        {
                            responseMessage.Message = "Congratulations ! \\n\\n \" + sigNames.Count + \" Digital Signature(s) is/are successfully verified. \\n However, there have been subsequent changes in the document.";
                            responseMessage.Valid = false;
                            pdfDocument.Close();

                        }
                        else
                        {
                            responseMessage.Message = "Congratulations ! \\n\\n \" + sigNames.Count + \" Digital Signature(s) is/are successfully verified.";
                            responseMessage.Valid = false;
                            pdfDocument.Close();
                        }
                    }
                    else
                    {
                        if (numinvalid > 0)
                        {
                            responseMessage.Message = "One or More Digital Signature Tampered.";
                            responseMessage.Valid = false;
                            pdfDocument.Close();
                        }
                    }
                }
            }
            return responseMessage;
        }


        #endregion

       
        public async Task<bool> SaveDigitalSignedDataToAnalytics(DTOSaveDigitalSignInfo saveDigitalSignInfo)
        {
            var ips = Dns.GetHostAddresses(Dns.GetHostName());
            saveDigitalSignInfo.IpAddress = ips.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString()
                                           ?? "Unknown";
             
            var result = await new ApiClient().PostRequestAsync<bool>(
                "api/DigitalSign/SaveDigitalSign",
                saveDigitalSignInfo
            );

            return result;
        }

        public async Task<MacResponse> GetMacAddress()
        {
            await System.Threading.Tasks.Task.Yield();
            try
            {
                var mac = GetPrimaryMacAddress();
                var machine = Environment.MachineName;               
                var ip = GetClientIpAddressSafe();

                if (string.IsNullOrWhiteSpace(mac))
                {
                    SetHttpStatusSafe(HttpStatusCode.NotFound);

                    return new MacResponse
                    {
                        Status = false,
                        Message = "MAC address not found (no active physical adapter detected).",
                        MachineName = machine,
                        MacAddress = null,
                        WindowsUserName = Environment.UserName,
                        ClientIpAddress = ip
                    };
                }

                SetHttpStatusSafe(HttpStatusCode.OK);

                return new MacResponse
                {
                    Status = true,
                    Message = "MAC address fetched successfully.",
                    MachineName = machine,
                    MacAddress = mac,
                    WindowsUserName = Environment.UserName,
                    ClientIpAddress = ip
                };
            }
            catch (Exception ex)
            {
                SetHttpStatusSafe(HttpStatusCode.InternalServerError);

                return new MacResponse
                {
                    Status = false,
                    Message = "Failed to read MAC address. " + ex.Message,
                    MachineName = Environment.MachineName,
                    MacAddress = null,
                    WindowsUserName = Environment.UserName,
                    ClientIpAddress = GetClientIpAddressSafe()
                };
            }
        }

        public async Task<MacVerifyResponse> VerifyMac(DeviceVerifyRequest req)
        {
            await System.Threading.Tasks.Task.Yield();

            try
            {
                var machine = Environment.MachineName;
                 
                var deviceMac = GetPrimaryMacAddress();
                var deviceMacNorm = NormalizeMac(deviceMac);

                var deviceUser = Environment.UserName;          

                var deviceIp  = GetClientIpAddressSafe();            
                
                if (req == null)
                {
                    SetHttpStatusSafe(HttpStatusCode.BadRequest);
                    return new MacVerifyResponse
                    {
                        Status = false,
                        Message = "Request body is required.",
                        MachineName = machine,
                        DeviceMac = deviceMac,
                        DeviceUserName = deviceUser,
                        DeviceIpAddress = deviceIp,
                        IsAllMatch = false
                    };
                }

                var inputMacNorm = NormalizeMac(req.Mac);
                var inputUserNorm = req.UserName;
                var inputIpNorm = req.IpAddress;

                if (inputMacNorm == null || inputUserNorm == null || inputIpNorm == null)
                {
                    SetHttpStatusSafe(HttpStatusCode.BadRequest);
                    return new MacVerifyResponse
                    {
                        Status = false,
                        Message = "Mac/UserName/IpAddress are required and must be valid.",
                        InputMac = req.Mac,
                        InputUserName = req.UserName,
                        InputIpAddress = req.IpAddress,
                        MachineName = machine,
                        DeviceMac = deviceMac,
                        DeviceUserName = deviceUser,
                        DeviceIpAddress = deviceIp,
                        IsAllMatch = false
                    };
                }

                if (string.IsNullOrWhiteSpace(deviceMac) || deviceMacNorm == null || string.IsNullOrWhiteSpace(deviceUser) || string.IsNullOrWhiteSpace(deviceIp))
                {
                    SetHttpStatusSafe(HttpStatusCode.NotFound);
                    return new MacVerifyResponse
                    {
                        Status = false,
                        Message = "Device identity not available on this system (MAC/User/IP missing).",
                        InputMac = req.Mac,
                        InputUserName = req.UserName,
                        InputIpAddress = req.IpAddress,
                        MachineName = machine,
                        DeviceMac = deviceMac,
                        DeviceUserName = deviceUser,
                        DeviceIpAddress = deviceIp,
                        IsAllMatch = false
                    };
                }
                 
                var macMatch = string.Equals(inputMacNorm, deviceMacNorm, StringComparison.OrdinalIgnoreCase);
                var userMatch = string.Equals(inputUserNorm, deviceUser, StringComparison.OrdinalIgnoreCase);
                var ipMatch = string.Equals(inputIpNorm, deviceIp, StringComparison.OrdinalIgnoreCase);

                var all = macMatch && userMatch && ipMatch;

                SetHttpStatusSafe(all ? HttpStatusCode.OK : HttpStatusCode.Unauthorized);

                return new MacVerifyResponse
                {
                    Status = true,
                    Message = all ? "Device verified successfully." : "Device verification failed.",

                    InputMac = req.Mac,
                    InputUserName = req.UserName,
                    InputIpAddress = req.IpAddress,

                    MachineName = machine,
                    DeviceMac = deviceMac,
                    DeviceUserName = deviceUser,
                    DeviceIpAddress = deviceIp,

                    IsMacMatch = macMatch,
                    IsUserMatch = userMatch,
                    IsIpMatch = ipMatch,
                    IsAllMatch = all
                };
            }
            catch (Exception ex)
            {
                SetHttpStatusSafe(HttpStatusCode.InternalServerError);
                return new MacVerifyResponse
                {
                    Status = false,
                    Message = "VerifyDevice failed: " + ex.Message,
                    IsAllMatch = false
                };
            }
        }
         
        private static void SetHttpStatusSafe(HttpStatusCode code)
        {
             
            var ctx = WebOperationContext.Current;
            if (ctx?.OutgoingResponse != null)
                ctx.OutgoingResponse.StatusCode = code;
        }
        private static string NormalizeMac(string mac)
        {
            if (string.IsNullOrWhiteSpace(mac)) return null;

            
            var cleaned = Regex.Replace(mac, "[^0-9a-fA-F]", "");
            if (cleaned.Length != 12) return null;

            
            if (!Regex.IsMatch(cleaned, "^[0-9a-fA-F]{12}$")) return null;

            return cleaned.ToUpperInvariant();  
        }
        private static string GetPrimaryMacAddress()
        {
             
            var nics = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n =>
                    n.OperationalStatus == OperationalStatus.Up &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    n.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                    n.GetPhysicalAddress() != null &&
                    n.GetPhysicalAddress().GetAddressBytes().Length == 6)
                .Select(n => new
                {
                    Nic = n,
                    Props = SafeGetProps(n),
                    Mac = n.GetPhysicalAddress()
                })
                .ToList();

            var best = nics
                .OrderByDescending(x => HasGateway(x.Props))
                .ThenByDescending(x => x.Nic.Speed)
                .FirstOrDefault();

            if (best == null) return null;

            return FormatMac(best.Mac);
        }

        private static IPInterfaceProperties SafeGetProps(NetworkInterface n)
        {
            try { return n.GetIPProperties(); }
            catch { return null; }
        }

        private static bool HasGateway(IPInterfaceProperties props)
        {
            try
            {
                if (props?.GatewayAddresses == null) return false;
                return props.GatewayAddresses.Any(g =>
                    g?.Address != null &&
                    !IPAddress.IsLoopback(g.Address) &&
                    !Equals(g.Address, IPAddress.Any) &&
                    !Equals(g.Address, IPAddress.IPv6Any));
            }
            catch { return false; }
        }

        private static string FormatMac(PhysicalAddress pa)
        { 
            var bytes = pa.GetAddressBytes();
            return string.Join("-", bytes.Select(b => b.ToString("X2")));
        }

        public static string GetClientIpAddressSafe()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var ip = host.AddressList.FirstOrDefault(a =>
                    a.AddressFamily == AddressFamily.InterNetwork &&
                    !IPAddress.IsLoopback(a));

                return ip?.ToString();
            }
            catch
            {
                return null;
            }
        }
    }
}