using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.Win32;
using SignService;
using SignService.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using WinniesMessageBox;

namespace DGISApp
{
    /// <summary>
    /// Interaction logic for SumitTest.xaml
    /// </summary>
    /// 



    public partial class VerifyDigitalSign : UserControl
    {


        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        string[] droppedFilePaths = null;
        public string download = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads";
        public VerifyDigitalSign()
        {
            InitializeComponent();

        }


        public static bool IsConnectedToInternet()
        {
            int Desc;
            return InternetGetConnectedState(out Desc, 0);
        }

        public static bool IsNetworkAvailable(long minimumSpeed)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // discard because of standard reasons
                if ((ni.OperationalStatus != OperationalStatus.Up) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
                    continue;

                // this allow to filter modems, serial, etc.
                // I use 10000000 as a minimum speed for most cases
                if (ni.Speed < minimumSpeed)
                    continue;

                // discard virtual cards (virtual box, virtual pc, etc.)
                if ((ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

                // discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
                if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
                    continue;

                return true;
            }
            return false;
        }

        private void DropList_DragEnter(object sender, DragEventArgs e)
        {


        }

        private async void DropList_Drop(object sender, DragEventArgs e)
        {
            try
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop, true))
                    return;

                var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                if (files == null || files.Length == 0)
                    return;

                DropList.IsEnabled = false;
                BusyBar.IsBusy = true;

                // MODE 1: PDF/Word
                if (RBModePdfWord.IsChecked == true)
                {
                    // Optional: allow only pdf/xml for this mode
                    var allowed = files
                        .Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                                 || f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (allowed.Length == 0)
                    {
                        MyMessageBox.Show("Please drop only PDF or XML files in PDF/Word mode.");
                        return;
                    }

                    // Heavy work off UI thread
                    verifyDigitalSign(allowed);
                }
                // MODE 2: Any File (Generic Sign)
                else
                {
                    // In your button code you use Multiselect=false for AnyFile, so take first file
                    var file = files[0];
                    var signatureFile = file + ".sig.json";

                    if (!File.Exists(signatureFile))
                    {
                        MyMessageBox.Show("Signature file not found: " + signatureFile);
                        return;
                    }

                    if (progress != null)
                    {
                        progress.Visibility = Visibility.Hidden;
                        progress.Value = 0;
                    }

                    bool ok = await HugeFileSignatureService.VerifyPortableAsync(file, signatureFile, UpdateProgress);

                    if (ok)
                        MyMessageBox.Show("Digital Signature is successfully verified.");
                    else
                        MyMessageBox.Show("Digital Signature verification failed or signature not found.");
                }
            }
            catch (Exception ex)
            {
                MyMessageBox.ShowDialog(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }
            finally
            {
                DropList.IsEnabled = true;
                BusyBar.IsBusy = false;
            }
        }

        void upload()
        {

        }


        public void verifyDigitalSign(string[] files)
        {
            bool NotModified = true;
            foreach (string filename in files)
            {
                ConfigurationManager.AppSettings["LastSelectedLocation"] = Path.GetDirectoryName(filename);
                string fileExtension = Path.GetExtension(filename).ToLower();
                if (fileExtension == ".pdf")
                {
                    PdfDocument pdfDocument = new PdfDocument(new PdfReader(filename));
                    // Checks that signature is genuine and the document was not modified.
                    bool genuineAndWasNotModified = false;

                    SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
                    IList<string> sigNames = signatureUtil.GetSignatureNames();
                    if (sigNames.Count == 0)
                    {
                        MyMessageBox.Show("Digital Signature not found.");
                        return;
                    }
                    else
                    {
                        int numValid = 0;
                        int numinvalid = 0;
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

                                        //
                                    }
                                }
                                else if (!documentNotModifie)
                                {
                                    MyMessageBox.Show("The revision of the document that was covered by this signature has not been altered; however, there have been subsequent changes in the document.");
                                    pdfDocument.Close();
                                    return;
                                }
                                else
                                {
                                    MyMessageBox.Show("The Signer's identity is invalid because it has expired or is not yet valid.");
                                    pdfDocument.Close();
                                    return;
                                }
                            }
                            catch (Exception)
                            {
                                // ignoring exceptions,
                                // we are only interested in signatures that are passing the check successfully
                            }
                        }
                        if (numValid == sigNames.Count)
                        {
                            if (!NotModified)
                            {
                                MyMessageBox.Show("Congratulations ! \n\n " + sigNames.Count + " Digital Signature(s) is/are successfully verified. \n However, there have been subsequent changes in the document.");
                                pdfDocument.Close();
                                return;
                            }
                            else
                            {
                                MyMessageBox.Show("Congratulations ! \n\n " + sigNames.Count + " Digital Signature(s) is/are successfully verified.");
                                pdfDocument.Close();
                                return;
                            }
                        }
                        else
                        {
                            if (numinvalid > 0)
                            {
                                MyMessageBox.Show("One or More Digital Signature Tampered.");
                                pdfDocument.Close();
                                return;
                            }
                        }
                    }
                }
                else if (fileExtension == ".xml")
                {
                    Service1 service1 = new Service1();
                    // var retcheckCrl= await service1.FetchUniqueTokenDetails();
                    // var retcheckCrl= await service1.FetchTokenOCSPCrlDetailsAsync(true,"");
                    byte[] fileContent = File.ReadAllBytes(filename);
                    string xmlString = Encoding.UTF8.GetString(fileContent);
                    //XmlDocument xmlDoc = new XmlDocument();
                    //xmlDoc.LoadXml(xmlString);  // Load the XML from the string
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xmlString);
                    string plainText = doc.InnerXml;
                    string ret = VerifySignXml(plainText);
                    //string ret= VerifySignXml(xmlString);
                    MyMessageBox.Show(ret);
                }
            }
        }

        #region Xml Signature Verification
        public string VerifySignXml(string data)
        {
            List<DigitalVerifyDetails> signers = new List<DigitalVerifyDetails>();
            DigitalVerifyDetails digitalVerifyDetails = new DigitalVerifyDetails();
            StringBuilder sb = new StringBuilder();
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.PreserveWhitespace = true;
                string ss = data;
                xmlDoc.LoadXml(ss);
                string digital = "DigitalSignature";
                int signatureCount = CountSignatureElements(xmlDoc);
                Service1 service1 = new Service1();
                if (signatureCount > 0)
                {
                    for (int i = 1; i <= signatureCount; i++)
                    {
                        XmlDocument xmlDoc1 = new XmlDocument();
                        string tagdigital = digital + i;
                        XmlElement childNodes = (XmlElement)xmlDoc.SelectSingleNode("//" + tagdigital);
                        if (childNodes != null)
                        {

                            digitalVerifyDetails = service1.DigitalVerify(childNodes, i);
                            signers.Add(digitalVerifyDetails);
                            sb.Append("\n Digital Signature " + i + "\n");
                            // Append format
                            sb.AppendFormat("Signature: {0}", digitalVerifyDetails.SignatureRemarks + "\n");
                            sb.AppendFormat("Signature By: {0}", digitalVerifyDetails.SignatureBy + "\n");

                        }
                        else
                        {
                            digitalVerifyDetails = service1.DigitalVerify(xmlDoc.DocumentElement, i);
                            signers.Add(digitalVerifyDetails);
                            sb.Append("\n Digital Signature " + i + "\n");
                            // Append format
                            sb.AppendFormat("Signature: {0}", digitalVerifyDetails.SignatureRemarks + "\n");
                            sb.AppendFormat("Signature By: {0}", digitalVerifyDetails.SignatureBy + "\n");

                        }

                    }
                }
                else
                {
                    sb.Append("DigitalSignature \n");
                    // Append format
                    sb.AppendFormat("Signature: {0}", digitalVerifyDetails.SignatureRemarks);
                    //digitalVerifyDetails.Signature = "Xml Not Signature throw DGIS Application";

                    //signers.Add(digitalVerifyDetails);
                }
            }
            catch (Exception ex)
            {

                //digitalVerifyDetails.Signature = "Invalid";
                sb.AppendFormat("Signature: {0}", "Invalid");
                ErrorLog.LogErrorToFile(ex);

            }
            return sb.ToString();
        }
        public static int CountSignatureElements(XmlDocument xmlDoc)
        {
            // Create a namespace manager and add the XMLDSIG namespace
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

            // Select all <Signature> elements in the XML
            XmlNodeList signatureNodes = xmlDoc.SelectNodes("//ds:Signature", nsMgr);

            // Return the count of <Signature> elements
            return signatureNodes.Count;
        }
        //public DigitalVerifyDetailsForUser DigitalVerify(XmlElement data, int count)
        //{
        //    DigitalVerifyDetailsForUser ret = new DigitalVerifyDetailsForUser();
        //    try
        //    {

        //        // Load the signed XML document
        //        XmlDocument xmlDoc = new XmlDocument();
        //        xmlDoc.PreserveWhitespace = true;
        //        string ss = data.OuterXml.Replace(" />", "/>");
        //        xmlDoc.LoadXml(ss);

        //        XmlDocument xmldigest = new XmlDocument();
        //        xmldigest.PreserveWhitespace = true;
        //        xmldigest.LoadXml(data.OuterXml);
        //        // Find the <Signature> element and remove it
        //        XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
        //        nsMgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

        //        // Find the <Signature> element (with namespace) and remove it
        //        // XmlNode signatureNode = xmlDoc1.SelectSingleNode("//ds:Signature", nsMgr);
        //        XmlNodeList signatureNode = xmldigest.SelectNodes("//ds:Signature", nsMgr);
        //        // Check if the <Signature> node exists
        //        if (signatureNode != null)
        //        {
        //            int lastsigncount = 1;
        //            foreach (XmlNode node in signatureNode)
        //            {
        //                if (node is XmlElement element)
        //                {
        //                    if (lastsigncount == count)
        //                        node.ParentNode.RemoveChild(node);
        //                }
        //                lastsigncount++;
        //            }
        //            // Remove the <Signature> node from its parent
        //            //signatureNode.ParentNode.RemoveChild(signatureNode);

        //        }
        //        // Create an XmlNamespaceManager for managing namespaces in XPath queries
        //        XmlNamespaceManager nsManager = new XmlNamespaceManager(xmlDoc.NameTable);
        //        nsManager.AddNamespace("ds", SignedXml.XmlDsigNamespaceUrl);

        //        // Find the Signature element
        //        XmlNodeList signatureElement1 = xmlDoc.SelectNodes("//ds:Signature", nsManager);
        //        XmlElement signatureElement = null;
        //        int countsign = 1;
        //        foreach (XmlNode node in signatureElement1)
        //        {
        //            if (node is XmlElement element)
        //            {
        //                if (countsign == count)
        //                    signatureElement = element;
        //            }
        //            countsign++;



        //        }

        //        //XmlElement signatureElement = xmlDoc.SelectSingleNode("//ds:Signature", nsManager) as XmlElement;
        //        if (signatureElement == null)
        //        {
        //            ret.Signature = "Signature " + count + " element not found in the document";
        //        }

        //        // Create a SignedXml object
        //        SignedXml signedXml = new SignedXml(xmlDoc);

        //        // Load the signature element into the SignedXml object
        //        signedXml.LoadXml(signatureElement);

        //        // Check overall signature validity (optional)
        //        bool isSignatureValid = signedXml.CheckSignature();
        //        if (isSignatureValid)
        //        {

        //            ret.Signature = "Signature " + count + " is Verifed";
        //            List<X509Certificate2> certificates = new List<X509Certificate2>();
        //            XmlNodeList certificateNodes = xmlDoc.GetElementsByTagName("X509Certificate");
        //            foreach (XmlNode node in certificateNodes)
        //            {
        //                string base64EncodedCertificate = node.InnerText;
        //                byte[] certBytes = Convert.FromBase64String(base64EncodedCertificate);
        //                X509Certificate2 certificate = new X509Certificate2(certBytes);
        //                certificates.Add(certificate);

        //                var subdata = certificate.Subject.Split(',');

        //                ret.SignatureBy = subdata[1].Replace("SERIALNUMBER=", "") + " (" + subdata[0].Replace("CN=", "") + ") ";


        //            }
        //        }
        //        else
        //        {

        //            ret.Signature = "Signature " + count + " is Not Verifed: ";
        //        }
        //        // Now handle references with missing or blank URI
        //        foreach (Reference reference in signedXml.SignedInfo.References)
        //        {
        //            // Check if the URI is blank
        //            if (string.IsNullOrEmpty(reference.Uri))
        //            {
        //                // Console.WriteLine("Blank Reference.Uri, assuming the entire document is signed.");

        //                // Canonicalize the entire document (or relevant root element)
        //                XmlDsigC14NTransform transform = new XmlDsigC14NTransform();
        //                transform.LoadInput(xmldigest); // Canonicalize the root element (entire document)

        //                // Get canonicalized data as a byte array
        //                byte[] canonicalizedData = GetCanonicalizedBytes(xmldigest);//(byte[])transform.GetOutput(typeof(byte[]));

        //                // Compute the digest using the specified digest method (e.g., SHA-256)
        //                byte[] computedDigest;
        //                using (System.Security.Cryptography.HashAlgorithm hashAlg = System.Security.Cryptography.HashAlgorithm.Create(reference.DigestMethod))
        //                {
        //                    computedDigest = hashAlg.ComputeHash(canonicalizedData);
        //                }

        //                // Compare the computed digest with the digest value from the XML signature
        //                bool digestValid = CompareByteArrays(computedDigest, reference.DigestValue);
        //                if (digestValid == true)
        //                {

        //                    ret.Signature = "Signature " + count + " is Verifed";
        //                }
        //                else
        //                {

        //                    ret.Signature = "Signature " + count + " is Not Verifed: ";
        //                    ret.SignatureBy = "";
        //                }
        //            }

        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        //ret.IsVerified = false;//"Signature element not found in the document.";
        //        if (ex.Message == "Invalid length for a Base-64 char array or string.")
        //            ret.Signature = "Signature X509Certificate Invalid";
        //        else
        //            ret.Signature = "Signature Invalid";
        //        ErrorLog.LogErrorToFile(ex);
        //    }

        //    return ret;
        //}
        public static byte[] GetCanonicalizedBytes(XmlDocument xmlDoc)
        {
            // Create a new XmlDsigC14NTransform for canonicalization
            XmlDsigC14NTransform transform = new XmlDsigC14NTransform();

            // Load the XML data into the transform
            transform.LoadInput(xmlDoc);

            // Get the canonicalized output as a byte array
            using (Stream stream = (Stream)transform.GetOutput(typeof(Stream)))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }
        }
        // Helper method to compare two byte arrays
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

        private async void btnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            try
            {
                // --- Mode 1: PDF/Word (your filter is PDF/XML currently) ---
                if (RBModePdfWord.IsChecked == true)
                {
                    openFileDialog.Filter = "Pdf files (*.pdf;*.PDF)|*.pdf;*.PDF|XML files (*.xml;*.XML)|*.xml;*.XML";
                    openFileDialog.Multiselect = true; // enable if you want multiple

                    openFileDialog.InitialDirectory =
                        string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["LastSelectedLocation"])
                        ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                        : ConfigurationManager.AppSettings["LastSelectedLocation"];

                    if (openFileDialog.ShowDialog() == true)
                    {
                        DropList.IsEnabled = false;
                        BusyBar.IsBusy = true;

                        // Run heavy work off the UI thread
                        verifyDigitalSign(openFileDialog.FileNames);
                    }
                }
                // --- Mode 2: Any file (Generic Sign verify) ---
                else
                {
                    openFileDialog.Title = "Select any file";
                    openFileDialog.Filter = "All Files (*.*)|*.*";
                    openFileDialog.Multiselect = false;

                    openFileDialog.InitialDirectory =
                        string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["LastSelectedLocation"])
                        ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                        : ConfigurationManager.AppSettings["LastSelectedLocation"];

                    if (openFileDialog.ShowDialog() == true)
                    {
                        DropList.IsEnabled = false;
                        BusyBar.IsBusy = true;

                        var file = openFileDialog.FileName;
                        var signatureFile = file + ".sig.json";

                        if (!File.Exists(signatureFile))
                        {
                            MyMessageBox.Show("Signature file not found: " + signatureFile);
                            return;
                        }

                        if (progress != null)
                        {
                            progress.Visibility = Visibility.Hidden;
                            progress.Value = 0;
                        }

                        bool ok = await HugeFileSignatureService.VerifyPortableAsync(file, signatureFile, UpdateProgress);

                        if (ok)
                            MyMessageBox.Show("Digital Signature is successfully verified.");
                        else
                            MyMessageBox.Show("Digital Signature verification failed or signature not found.");
                    }
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                // optional: show message if needed
            }
            catch (Exception ex)
            {
                MyMessageBox.ShowDialog(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }
            finally
            {
                // Always restore UI state
                DropList.IsEnabled = true;
                BusyBar.IsBusy = false;
            }
        }

        private void RBModePdfWord_Checked(object sender, RoutedEventArgs e)
        {
            lblStep1.Content = "Step 1. Select Digitally Signed Document and wait for few seconds.";
        }

        private void RBModeAnyFile_Checked(object sender, RoutedEventArgs e)
        {
            lblStep1.Content = "Step 1. Select Digitally Signed Original File and wait for few seconds.";
        }

        private void UpdateProgress(double percent)
        {
            // This can be called from any thread
            Dispatcher.Invoke(() => progress.Value = percent);
        }
    }
}
