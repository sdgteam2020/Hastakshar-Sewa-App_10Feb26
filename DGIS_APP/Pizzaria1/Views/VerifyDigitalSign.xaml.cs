using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.Win32;
using SignService;
using SignService.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using WinniesMessageBox;

namespace DGISApp
{
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

                if ((ni.OperationalStatus != OperationalStatus.Up) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
                    continue;

                if (ni.Speed < minimumSpeed)
                    continue;

                if ((ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

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

                if (RBModePdfWord.IsChecked == true)
                {
                    var allowed = files
                        .Where(f => f.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)
                                 || f.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (allowed.Length == 0)
                    {
                        MyMessageBox.Show("Please drop only PDF or XML files in PDF/Word mode.");
                        return;
                    }

                    verifyDigitalSign(allowed);
                }
                else
                {

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

                    byte[] fileContent = File.ReadAllBytes(filename);
                    string xmlString = Encoding.UTF8.GetString(fileContent);

                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xmlString);
                    string plainText = doc.InnerXml;
                    string ret = VerifySignXml(plainText);

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
                             
                            sb.AppendFormat("Signature: {0}", digitalVerifyDetails.SignatureRemarks + "\n");
                            sb.AppendFormat("Signature By: {0}", digitalVerifyDetails.SignatureBy + "\n");

                        }
                        else
                        {
                            digitalVerifyDetails = service1.DigitalVerify(xmlDoc.DocumentElement, i);
                            signers.Add(digitalVerifyDetails);
                            sb.Append("\n Digital Signature " + i + "\n");
                             
                            sb.AppendFormat("Signature: {0}", digitalVerifyDetails.SignatureRemarks + "\n");
                            sb.AppendFormat("Signature By: {0}", digitalVerifyDetails.SignatureBy + "\n");

                        }

                    }
                }
                else
                {
                    sb.Append("DigitalSignature \n");
                     
                    sb.AppendFormat("Signature: {0}", digitalVerifyDetails.SignatureRemarks);
                   
                }
            }
            catch (Exception ex)
            {
                 
                sb.AppendFormat("Signature: {0}", "Invalid");
                ErrorLog.LogErrorToFile(ex);

            }
            return sb.ToString();
        }
        public static int CountSignatureElements(XmlDocument xmlDoc)
        { 
            XmlNamespaceManager nsMgr = new XmlNamespaceManager(xmlDoc.NameTable);
            nsMgr.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#");

             
            XmlNodeList signatureNodes = xmlDoc.SelectNodes("//ds:Signature", nsMgr);
             
            return signatureNodes.Count;
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

        private async void btnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();

            try
            {

                if (RBModePdfWord.IsChecked == true)
                {
                    openFileDialog.Filter = "Pdf files (*.pdf;*.PDF)|*.pdf;*.PDF|XML files (*.xml;*.XML)|*.xml;*.XML";
                    openFileDialog.Multiselect = true;

                    openFileDialog.InitialDirectory =
                        string.IsNullOrWhiteSpace(ConfigurationManager.AppSettings["LastSelectedLocation"])
                        ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                        : ConfigurationManager.AppSettings["LastSelectedLocation"];

                    if (openFileDialog.ShowDialog() == true)
                    {
                        DropList.IsEnabled = false;
                        BusyBar.IsBusy = true;


                        verifyDigitalSign(openFileDialog.FileNames);
                    }
                }
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
            Dispatcher.Invoke(() => progress.Value = percent);
        }
    }
}
