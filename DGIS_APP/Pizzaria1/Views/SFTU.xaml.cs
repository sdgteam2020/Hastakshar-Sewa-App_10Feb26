using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Signatures;
using MaterialDesignThemes.Wpf;
using Microsoft.Office.Interop.Word;
using Microsoft.Win32;
using MyApp;
using Newtonsoft.Json;
using SignService;
using SignService.Helpers;
using SignService.HttpClients;
using Spire.Pdf.Fields;
using Spire.Pdf.Graphics;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using WinniesMessageBox;
using static DGISApp.DigitalSign;
using static iText.Signatures.PdfSigner;
using static ValidateCertificate.ValidateCert;
using Brushes = System.Windows.Media.Brushes;
using Console = System.Console;

namespace DGISAPP.Views
{
    /// <summary>
    /// Interaction logic for SFTU.xaml
    /// </summary>
    public partial class SFTU : UserControl
    {
        string[] droppedFilePaths = null;
       
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        bool crloscp = false;
        string crlocspmsg = "";
        string CertThumbPrint = "";
        
        Aes myAes = Aes.Create();



        public SFTU()
        {
            InitializeComponent();
        }

        private void DropList_DragEnter(object sender, DragEventArgs e)
        {
        }

        private void DropList_Drop(object sender, DragEventArgs e)
        {

            try
            {
               
                if (textpassword.Password.ToString() == "")
                {
                   
                    if (Encrypt.IsChecked == true)
                    {
                       
                        if (textpassword.Password == "" && RArmyNo.IsChecked == false && RName.IsChecked == false)
                            MyMessageBox.ShowDialog("Please Get PublicKey");

                        if (RArmyNo.IsChecked == true || RName.IsChecked == true)
                        {
                            if (txtSearch.Text == "")
                                MyMessageBox.ShowDialog("Please Search Name.ArmyNo");
                        }
                        return;
                    }
                    
                }
                droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                if (Encrypt.IsChecked == true)
                {
                    string macAddress = txtMacAddress.Text.ToString();
                    string username = txtUsername.Text.ToString();
                    string dateValidity = dpValidity.Text.ToString();
                    if (macAddress == "")
                    {

                        MyMessageBox.ShowDialog("Please Enter Mac Address");
                        return;
                    }
                    if (username == "")
                    {

                        MyMessageBox.ShowDialog("Please Enter username");
                        return;
                    }
                    if (dateValidity == "")
                    {

                        MyMessageBox.ShowDialog("Please Enter Validity");
                        return;
                    }
                    macAddress=macAddress+"_"+username+"_"+dateValidity;
                    if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                    {
                        fileEncrypt(droppedFilePaths, macAddress);
                    }
                }
                else if (Export.IsChecked == true)
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                    {
                        ExportSingleFileAsync(droppedFilePaths);
                    }
                   
                }
               
            }
            catch (Exception ex)
            {
                if (ex.Message == "Value cannot be null.\r\nParameter name: value")
                {
                    MyMessageBox.ShowDialog("Please Select  Key Size");
                }
                else
                {
                    MyMessageBox.ShowDialog(ex.Message);
                }
                ErrorLog.LogErrorToFile(ex);
            }
        }
      
        public async void fileEncrypt(string[] files, string macAddress)
        {

            string DownloadPath = "";
            int totalFiles = files.Count(); 
            int processedFiles = 0;

            foreach (var path in files)
            {
                string MacAddress = null;
                if (!string.IsNullOrEmpty(macAddress)) {
                     MacAddress = macAddress;
                }
                ConfigurationManager.AppSettings["LastSelectedLocation"] = System.IO.Path.GetDirectoryName(path);
                DownloadPath = System.IO.Path.GetDirectoryName(path);

                FileInfo fi = new FileInfo(path);
                if (fi.Length <= 524288000)
                {
                    byte[] expectedHeader = System.Text.Encoding.UTF8.GetBytes("ASDC_AESGCM256");
                    byte[] fileHeader = new byte[expectedHeader.Length];

                    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        fs.Read(fileHeader, 0, fileHeader.Length);
                    }

                    var IsFileEncrypted = expectedHeader.SequenceEqual(fileHeader);
                    if (fi.Extension == ".mil")
                    {
                        MyMessageBox.ShowDialog("mil File Extension Not Allow .");
                        break;
                    }
                    if (IsFileEncrypted)
                    {
                        MyMessageBox.ShowDialog("This is File Encrypted.");
                        break;
                    }
                    FileStream stream = File.OpenRead(path);
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    stream.Read(bytes, 0, bytes.Length);
                    stream.Close();
                    new Thread(async () =>
                    {
                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));
                        byte[] magicHeader = Encoding.UTF8.GetBytes("ASDC_AESGCM256");
                        bool encryptResult = false;
                        
                            string signedPath = await GenericSignFileAsync(path);
                            if (string.IsNullOrEmpty(signedPath))
                            {
                                MyMessageBox.ShowDialog("File signing failed.");
                                return;
                            }

                            string rsaKeyXml = textpassword.Password.ToString();

                            string Output = DownloadPath + "\\" + fi.Name + "_RSA_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + ".mil";

                            if (!string.IsNullOrWhiteSpace(rsaKeyXml))
                                encryptResult = Service1.EncryptFile(path, Output, rsaKeyXml, magicHeader,MacAddress);

                            if (encryptResult)
                            {
                                string zipPath = CreateZip(path, signedPath, Output);

                                try
                                {
                                    if (File.Exists(signedPath))
                                        File.Delete(signedPath);
                                    if (File.Exists(signedPath))
                                        File.Delete(signedPath);

                                    if (File.Exists(Output))
                                        File.Delete(Output);
                                }
                                catch (Exception ex)
                                {

                                    ErrorLog.LogErrorToFile(ex);
                                }
                            }
                           
                        

                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));

                        processedFiles++;

                        if (processedFiles == totalFiles)
                        {
                            if (!encryptResult)
                            {
                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Invalid Public Key!")));
                                return;
                            }
                            var result = this.Dispatcher.Invoke(new Func<string>(() =>
                            {

                                return MyMessageBox.ShowDialog("Congratulations!\n\nDocument is successfully Secured by digital sign and Encryption.\n" + DownloadPath, MyMessageBox.Buttons.OK_PathOpen);
                            }));

                            if (result == "2")
                            {
                                try
                                {
                                    Process.Start(DownloadPath);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("An error occurred: " + ex.Message);
                                }
                            }
                        }
                    }).Start();
                }
                else
                {
                    MyMessageBox.ShowDialog("File size should be less than 500 MB.");
                    break;
                }
            }



        }

        private void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            lblFileEncryption.Content = "Asymmetric (Public-Key) Encryption (One to One Sharing)";
            lblStep1.Content = "Step 1: Convey recipient to insert IACA token and click ‘Get Public Key’.";
            lblStep2.Content = "Step 2: Recipient to Share own public key with Sender on eOffice or ASIGMA.";
            lblStep3.Content = "Step 3: Paste the recipient’s public key in text box below and select the file (s) to be encrypted.";
            lblStep4.Content = "Step 4: Encrypted file (Originalfilena_me_RSA_date_milliseconds.mil) will be created at original file loc.";
            lblStep5.Content = "Step 5. Share file with .mil extn to the recipient.";
            lblNote.Content = "Note : Use Asymmetric encryption only for one-to-one file sharing as matching public-private Key pair\r\n           (IACA token) can only encrypt/ decrypt the file.";

            txtDefaultPass.Text = "Please fetch public key of inserted token :";
            txtDefaultPasswarnning.Content = " Please Enter Recipient's Public Key";
            txtDefaultPasswarnning.Visibility = Visibility.Visible;
            RArmyNo.Visibility = Visibility.Visible;
            btnGetPublicKey.Visibility = Visibility.Visible;
            textpassword.Visibility = Visibility.Visible;
            //  txtMacAddress.Visibility = Visibility.Visible;
            // lblMacAddress.Visibility = Visibility.Visible;
            secureFileLblGrid.Visibility = Visibility.Visible;
            secureFileTxtGrid.Visibility = Visibility.Visible;
            HintAssist.SetHint(textpassword, "Enter Recipient's Public Key");
            HintAssist.SetHint(txtMacAddress, "Enter MAC Address (e.g. 00-1A-2B-3C-4D-5E)");
            textpassword.MaxLength = 5000;
            textpassword.Password = "";
            RArmyNo.IsChecked = false;
            RName.IsChecked = false;
            RArmyNo.IsChecked = false;
            txtSearch.Visibility = Visibility.Hidden;
            txtSearch.Text = "";
            ShowSuggestions(false);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            lblFileEncryption.Content = "Export Secure File";
            lblStep1.Content = "Step 1: Select single or multiple files.";
            lblStep2.Content = "Step 2: System will calculate SHA-256 hash.";
            lblStep3.Content = "Step 3: Copy/verify hash output as required.";
            lblStep4.Content = "Step 4: Share hash for integrity verification.";
            lblStep5.Content = ""; 
            lblNote.Content = "Note : Export. It is used to verify file integrity (tamper detection).";
            
            Encrypt.IsChecked = false;
            secureFileLblGrid.Visibility = Visibility.Hidden;
            secureFileTxtGrid.Visibility = Visibility.Hidden;
            btnGetPublicKey.Visibility = Visibility.Hidden;
            RArmyNo.Visibility = Visibility.Hidden;
            RName.Visibility = Visibility.Hidden;
            //txtMacAddress.Visibility = Visibility.Hidden;
            txtSearch.Visibility = Visibility.Hidden;
            //lblMacAddress.Visibility= Visibility.Hidden;
            txtDefaultPasswarnning.Visibility = Visibility.Hidden;
            txtSearch.Text = "";
            ShowSuggestions(false);

            textpassword.Visibility = Visibility.Hidden;
            textpassword.Password = "";
        }


        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            textpassword.Password = "";
            lstSuggestions.ItemsSource = null;
            ShowSuggestions(false);

            if (RArmyNo.IsChecked == true)
            {
                lstSuggestions.Visibility = Visibility.Visible;
                txtDefaultPasswarnning.Content = "Please Search ArmyNo";
                HintAssist.SetHint(txtSearch, "Enter ArmyNo...");
                txtSearch.Visibility = Visibility.Visible;
                txtSearch.Focus();
                txtDefaultPasswarnning.Visibility = Visibility.Visible;
                textpassword.Visibility = Visibility.Visible;
            }
            else if (RName.IsChecked == true)
            {
                txtDefaultPass.Text = "Please Search Name:";
                txtSearch.Visibility = Visibility.Visible;
                lstSuggestions.Visibility = Visibility.Visible;
                txtDefaultPasswarnning.Content = "Please Search Name";
                textpassword.Visibility = Visibility.Hidden;
            }
        }
        public class SuggestionItem
        {
            public string Text { get; set; }
            public string Value { get; set; }  
            public override string ToString()
            {
                return Text; 
            }

        }
        public class DTOApiCall
        {
            public string ArmyNo { get; set; }
            public string Name { get; set; }
        }


        private void lstSuggestions_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (lstSuggestions.SelectedItem is SuggestionItem selectedItem)
            {
                ApplySuggestion(selectedItem);
            }
        }

        private async void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            textpassword.Password = "";

            string query = (txtSearch.Text ?? "").Trim().ToLower();

            if (string.IsNullOrWhiteSpace(query))
            {
                ShowSuggestions(false);
                return;
            }
            if (RArmyNo.IsChecked != true)
            {
                ShowSuggestions(false);
                return;
            }

            DTOApiCall datatopost = new DTOApiCall();

            if (RArmyNo.IsChecked == true)
                datatopost.ArmyNo = query;
            else
                datatopost.Name = query; 

            var filteredList = await new ApiClient().PostRequestAsync("api/transaction/search", datatopost);

            if (filteredList != null)
            {
                List<SuggestionItem> listdata = new List<SuggestionItem>();

                foreach (var item in filteredList)
                {
                    SuggestionItem data = new SuggestionItem();
                    data.Text = item.SerialNo;

                    byte[] bytes = Convert.FromBase64String(item.Public_Key);
                    data.Value = Encoding.UTF8.GetString(bytes);

                    listdata.Add(data);
                }

                if (listdata.Count > 0)
                {
                    lstSuggestions.ItemsSource = listdata;
                    ShowSuggestions(true);
                }
                else
                {
                    ShowSuggestions(false);
                }
            }
            else
            {
                ShowSuggestions(false);
                
            }
        }

        private async void btnGetPublicKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RArmyNo.IsChecked = false;
                RName.IsChecked = false;
                textpassword.Visibility = Visibility.Visible;
                textpassword.Password = "";
                txtSearch.Visibility = Visibility.Hidden;
                txtDefaultPasswarnning.Content = "Please Enter Recipient's Public Key";
                HintAssist.SetHint(textpassword, "Enter Recipient's Public Key");


                IService1 service1 = new Service1();
                var PublicKey = await service1.GetPublicKey();
                if (PublicKey.Status == "200" && PublicKey.TokenValid == true)
                {
                    // Define local file path


                    //string filePath = "PublicKeyData.xml";
                    string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string Appfolder = System.IO.Path.Combine(path, "DGIS");
                    Directory.CreateDirectory(Appfolder);
                    string filePath = System.IO.Path.Combine(Appfolder, "PublicKeyData.xml");
                    FileInfo fi = new FileInfo(filePath);
                    List<XmlDataForPublicKey> xmlDataForPublicKeys = new List<XmlDataForPublicKey>();
                    XmlDataForPublicKey xmlDataForPublicKey = new XmlDataForPublicKey();
                    //Extracting Personal No from unique token 

                    string[] SubjectSplit = PublicKey.subject.Split(',');
                    // string PersNo = SubjectSplit[1].ToString().Replace("SERIALNUMBER=", "").Trim();

                    for (int i = 0; i < SubjectSplit.Length; i++)
                    {
                        if (SubjectSplit[i].Contains("SERIALNUMBER"))
                            xmlDataForPublicKey.SerialNo = SubjectSplit[i].ToString().Replace("SERIALNUMBER=", "").Trim();
                    }
                    byte[] textBytes = Encoding.UTF8.GetBytes(PublicKey.Public_Key);
                    xmlDataForPublicKey.Public_Key = Convert.ToBase64String(textBytes);
                    xmlDataForPublicKey.TokenValid = PublicKey.TokenValid;
                    xmlDataForPublicKey.ValidFrom = PublicKey.ValidFrom;
                    xmlDataForPublicKey.ValidTo = PublicKey.ValidTo;

                    // xmlDataForPublicKey.Public_Key = PublicKey.Public_Key;
                    xmlDataForPublicKey.Status = false;



                    xmlDataForPublicKeys.Add(xmlDataForPublicKey);
                    // Read and deserialize XML data
                    if (File.Exists(filePath) && fi.Length > 5)
                    {
                        bool exists = CheckSerialNoExists(filePath, xmlDataForPublicKey.SerialNo);

                        if (exists != false)
                        {
                            SaveToXml(xmlDataForPublicKey, filePath);
                        }
                    }
                    else
                    {
                        SaveToXml(xmlDataForPublicKey, filePath);
                    }

                    // Serialize object to XML and save to file

                    //Clipboard.SetText(PublicKey.Public_Key);


                    textpassword.Password = PublicKey.Public_Key;
                    MyMessageBox.ShowDialog(PublicKey.Public_Key);

                }
                else
                {
                    MyMessageBox.ShowDialog(PublicKey.Remarks);
                }
            }
            catch (Exception ex)
            {
                MyMessageBox.ShowDialog("Something went wrong :" + ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }
        }
        static bool CheckSerialNoExists(string filePath, string serialNo)
        {
            if (!System.IO.File.Exists(filePath))
                return false;

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ns", "http://schemas.datacontract.org/2004/07/SignService");

            XmlNode node = doc.SelectSingleNode($"/PublicKeysData/ns:XmlDataForPublicKey[ns:SerialNo='{serialNo}']", nsmgr);
            if (node != null)
                return false;
            else
                return true;
        }
        static void SaveToXml(XmlDataForPublicKey data, string filePath)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(XmlDataForPublicKey));
            FileInfo fi = new FileInfo(filePath);
            try
            {
                if (!File.Exists(filePath))
                {
                    // Create new XML document with root and first entry
                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                    using (XmlWriter writer = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true }))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("PublicKeysData"); // Root Node
                        serializer.WriteObject(writer, data);
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                }
                else
                {
                    // Load existing XML, add new entry, and save back
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filePath);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (XmlWriter writer = XmlWriter.Create(ms, new XmlWriterSettings { Indent = true }))
                        {
                            serializer.WriteObject(writer, data);
                        }
                        ms.Position = 0;

                        XmlDocument tempDoc = new XmlDocument();
                        tempDoc.Load(ms);
                        XmlNode newNode = doc.ImportNode(tempDoc.DocumentElement, true);

                        doc.DocumentElement.AppendChild(newNode);
                    }

                    doc.Save(filePath);
                }
            }
            catch (Exception ex)
            {
                // Create new XML document with root and first entry
                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                using (XmlWriter writer = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true }))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("PublicKeysData"); // Root Node
                    serializer.WriteObject(writer, data);
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
        }
        private void btnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (textpassword.Password.ToString() == "")
                {
                    
                     if (Encrypt.IsChecked == true)
                    {
                        if (textpassword.Password == "" && RArmyNo.IsChecked == false && RName.IsChecked == false)
                            MyMessageBox.ShowDialog("Please Get PublicKey");

                        if (RArmyNo.IsChecked == true || RName.IsChecked == true)
                        {
                            if (txtSearch.Text == "")
                                MyMessageBox.ShowDialog("Please Search Name.ArmyNo");
                        }
                    }
                    return;
                }
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (Encrypt.IsChecked == true)
                {
                    string macAddress = txtMacAddress.Text.ToString();
                    if (macAddress == "")
                    {

                        MyMessageBox.ShowDialog("Please Enter Mac Address");
                        return;
                    }
                   
                    openFileDialog.Title = "Select File for Enryption";
                    openFileDialog.Multiselect = true;
                    //openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    if (ConfigurationManager.AppSettings["LastSelectedLocation"] == "")
                    {
                        openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    }
                    else
                    {
                        openFileDialog.InitialDirectory = ConfigurationManager.AppSettings["LastSelectedLocation"];
                    }
                    if (openFileDialog.ShowDialog() == true)
                    {

                        fileEncrypt(openFileDialog.FileNames, macAddress);

                    }
                }
                else if(Export.IsChecked == true)
                {

                    if (openFileDialog.ShowDialog() == true)
                    {
                        ExportSingleFileAsync(openFileDialog.FileNames);
                    }

                }
                else
                {
                    textpassword.Clear();
                    MyMessageBox.ShowDialog("Password Length should be between 4 to 16 Characters.");
                }


            }
            catch (Exception ex)
            {
                if (ex.Message == "Value cannot be null.\r\nParameter name: value")
                {
                    MyMessageBox.ShowDialog("Please Select Key Size");
                }
                else
                {
                    MyMessageBox.ShowDialog(ex.Message);
                }
                ErrorLog.LogErrorToFile(ex);
            }
        }
        private void ShowSuggestions(bool show)
        {
            // Popup may not exist in old view mode; safe guard
            if (popSuggestions != null)
                popSuggestions.IsOpen = show;
        }

        private void ApplySuggestion(SuggestionItem selectedItem)
        {
            if (selectedItem == null) return;

            // Avoid re-triggering TextChanged while setting text
            txtSearch.TextChanged -= txtSearch_TextChanged;
            txtSearch.Text = selectedItem.Text;
            txtSearch.CaretIndex = txtSearch.Text.Length;
            txtSearch.TextChanged += txtSearch_TextChanged;

            ShowSuggestions(false);

            // Put public key into password box
            textpassword.MaxLength = 5000;
            textpassword.Password = selectedItem.Value;

            // keep previous behavior: key goes to textpassword
            textpassword.Visibility = Visibility.Visible;
        }

        private void txtSearch_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (popSuggestions == null || !popSuggestions.IsOpen) return;

            if (e.Key == System.Windows.Input.Key.Down)
            {
                lstSuggestions.Focus();
                if (lstSuggestions.Items.Count > 0)
                    lstSuggestions.SelectedIndex = 0;
                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                ShowSuggestions(false);
                e.Handled = true;
            }
        }

        private void lstSuggestions_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                if (lstSuggestions.SelectedItem is SuggestionItem selectedItem)
                    ApplySuggestion(selectedItem);

                e.Handled = true;
            }
            else if (e.Key == System.Windows.Input.Key.Escape)
            {
                ShowSuggestions(false);
                txtSearch.Focus();
                e.Handled = true;
            }
        }

        private void txtSearch_LostKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            
            if (lstSuggestions == null || !lstSuggestions.IsKeyboardFocusWithin)
                ShowSuggestions(false);
        }
        private async System.Threading.Tasks.Task<string> GenericSignFileAsync(string filePath)

        {
            try
            {
                // ✅ Always read UI values on UI thread (because you might call from anywhere)
                string remark = "";
                bool checkCrlTick = false;
                //bool isAnyFileSigned = false;
                DTOSaveDigitalSignInfo saveDigitalSignInfo;
                var headers = WebOperationContext.Current?.IncomingRequest?.Headers;

                string origin = headers?["Origin"];   // can be null
                string referer = headers?["Referer"];  // can be null


                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    ShowMsg("Invalid file.");
                    return null;
                }

                string pattern = @"^[a-zA-Z0-9@, ._\-]+$";
                if (!Regex.IsMatch(remark, pattern) && remark != "")
                {
                    ShowMsg("Special Characters Not Allow ");
                    return null;
                }

                HelperCert helperCert = new HelperCert();
                var result = await helperCert.CheckSomethingAsync();

                if (result.Status == "0" || result.Status == "-1")
                {
                    ShowMsg(result.Remark);
                    return null;
                }

                CertThumbPrint = result.Remark;

                // CRL check (uses local checkCrlTick)
                if (checkCrlTick)
                {
                    if (crloscp == true && crlocspmsg == "Digital Cert of token cannot be verified with CA due to Network issues")
                    {
                        if (ShowMsg(
                                "Digital Cert of token cannot be verified with CA due to Network issues. Do you want to continue ?",
                                MyMessageBox.Buttons.Yes_No) != "1")
                        {
                            ShowMsg("No file signed!");
                            return null;
                        }
                    }
                    else if (crloscp == false)
                    {
                        ShowMsg(crlocspmsg == "Digital Cert of token cannot be verified with CA due to Network issues"
                            ? crlocspmsg
                            : "CRL Check Failed !");
                        return null;
                    }
                }

                // Load certificate
                X509Certificate2 cert;
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var found = store.Certificates.Find(X509FindType.FindByThumbprint, CertThumbPrint, false);

                    if (found == null || found.Count == 0)
                    {
                        ShowMsg("Certificate not found in store.");
                        return null;
                    }
                    cert = found[0];
                }

                if (DateTime.Now > cert.NotAfter)
                {
                    ShowMsg("Token is expired. Pl contact issuer !");
                    return null;
                }

                // ✅ Sign (background/parallel in service)
                string sigPath = await HugeFileSignatureService.SignPortableAsync(
                    filePath, cert, UpdateProgress, remark);
                if (!string.IsNullOrEmpty(sigPath))
                {
                    saveDigitalSignInfo = new DTOSaveDigitalSignInfo();
                    saveDigitalSignInfo.SignedDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");//for all signed document same date time in case of bulk sign
                    var PublicKey = await new Service1().GetPublicKey();
                    byte[] textBytes = Encoding.UTF8.GetBytes(PublicKey.Public_Key);
                    saveDigitalSignInfo.PublicKey = Convert.ToBase64String(textBytes);
                    saveDigitalSignInfo.ValidToken = PublicKey.TokenValid;
                    saveDigitalSignInfo.ValidFrom = PublicKey.ValidFrom;
                    saveDigitalSignInfo.ValidTo = PublicKey.ValidTo;
                    saveDigitalSignInfo.OriginForSign = origin;
                    saveDigitalSignInfo.RefererForSign = referer;
                    saveDigitalSignInfo.SerialNo = cert.Subject.Split(',')[1].Replace("SERIALNUMBER=", "").Trim();
                    saveDigitalSignInfo.DocumentName = Path.GetFileName(sigPath);

                }
                else
                {
                    saveDigitalSignInfo = null;
                }
                if (saveDigitalSignInfo != null)
                {
                    await new Service1().SaveDigitalSignedDataToAnalytics(saveDigitalSignInfo);
                }
                return sigPath;



            }
            catch (Exception ex)
            {
                ShowMsg(ex.Message);
                ErrorLog.LogErrorToFile(ex);
                return null;
            }
            finally
            {
                //await Dispatcher.InvokeAsync(() =>
                //{
                //    BusyBar.IsBusy = false;
                //    if (DropList != null) DropList.IsEnabled = true;

                //    if (progress != null)
                //    {
                //        progress.Value = 0;
                //        progress.Visibility = Visibility.Collapsed;
                //    }

                //});

            }
        }
        private System.Threading.Tasks.Task ExportSingleFileAsync(string[] files)

        {
            return System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {
                    string path = files[0];
                        ConfigurationManager.AppSettings["LastSelectedLocation"] = System.IO.Path.GetDirectoryName(path);
                        string DownloadPath = System.IO.Path.GetDirectoryName(path);
                        int ret1=0;

                        FileInfo fi = new FileInfo(path);
                        if (fi.Length <= 524288000)
                        {
                            if (fi.Extension == ".mil")
                            {


                                FileStream stream1 = File.OpenRead(path);
                                byte[] bytes1 = new byte[stream1.Length];
                                stream1.Read(bytes1, 0, bytes1.Length);

                                stream1.Close();

                                char dd = '_';
                                int levelOfEncryption = fi.FullName.Count(s => s == dd);
                                
                                    new Thread(async () =>
                                    {

                                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));
                                        string filePath = DownloadPath + "\\" + fi.Name.Split('.')[0];

                                        X509Certificate2Collection fcollection = await helper.GetCertificates();

                                        if (fcollection.Count == 0)
                                        {
                                            //return false;
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
                                            if (cert1 != null)
                                            {
                                                ret1 = Service1.DecryptFile(path, filePath, cert1);

                                            }
                                        }
                                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));

                                        if (ret1 == 0)
                                        {
                                            var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                            {
                                                return MyMessageBox.Show("Wrong Token Inserted Does Not Match Private Key");
                                            }));

                                        }
                                        else if (ret1 == 2)
                                        {
                                            var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                            {
                                                return MyMessageBox.Show("Unable to fetch system MAC address.");

                                            }));

                                        }
                                        else if (ret1 == 3)
                                        {
                                            var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                            {
                                                return MyMessageBox.Show("MAC address or Username mismatch or validity expired. File not allowed on this machine.");

                                            }));
                                            return;
                                        }
                                        else if (ret1 == 4)
                                        {
                                            bool ok = false;
                                            FileInfo fin = new FileInfo(filePath);
                                            string fileName = fin.Name;
                                            string file = DownloadPath + "\\" + fileName + ".pdf";
                                            var signatureFile = file + ".sig.json";

                                            if (File.Exists(signatureFile))
                                            {
                                                ok = await HugeFileSignatureService.VerifyPortableAsync(file, signatureFile, UpdateProgress);
                                            }

                                            if (ok)
                                            {
                                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                                {

                                                    return MyMessageBox.ShowDialog("Congratulations!\n\nDocument is successfully Decrypted.\n" + DownloadPath, MyMessageBox.Buttons.OK_PathOpen);
                                                }));

                                                if (result == "2")
                                                {
                                                    try
                                                    {
                                                        Process.Start(DownloadPath);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine("An error occurred: " + ex.Message);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (File.Exists(file))
                                                    File.Delete(file);
                                                var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                                {

                                                    return MyMessageBox.Show("This is a Secure File!\n Digital Signature verification failed or signature not found.");

                                                }));


                                            }
                                        }
                                      
                                    }).Start();
                                

                            }
                            else
                            {
                                MyMessageBox.ShowDialog("File format not supported. Please Select .mil file.");
                            }
                        }
                        else
                        {
                            MyMessageBox.ShowDialog("File size is too large! Max size is 500 MB");
                        }
                    
                }
                catch (Exception)
                {
                    MyMessageBox.ShowDialog("Invaild File....");
                }
            });
        }
        private string CreateZip(string originalFile, string signedFile, string encryptedFile)
        {


            string zipPath = Path.Combine(
                Path.GetDirectoryName(originalFile),
                Path.GetFileNameWithoutExtension(originalFile) + ".zip"
            );

            if (File.Exists(zipPath))
                File.Delete(zipPath);

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                zip.CreateEntryFromFile(signedFile, Path.GetFileName(signedFile));
                zip.CreateEntryFromFile(encryptedFile, Path.GetFileName(encryptedFile));
            }

            return zipPath;
        }


        private void UpdateProgress(double percent)
        {
            // This can be called from any thread
           // Dispatcher.Invoke(() => progress.Value = percent);
        }

        private string ShowMsg(string text, MyMessageBox.Buttons buttons = MyMessageBox.Buttons.OK)
        {
            if (Dispatcher.CheckAccess())
                return MyMessageBox.ShowDialog(text, buttons);

            return Dispatcher.Invoke(() => MyMessageBox.ShowDialog(text, buttons));
        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Card1.Width = 700;
            //pdfviewer.Visibility = Visibility.Hidden;
            this.BusyBar.IsBusy = false;
            this.DropList.IsEnabled = true;
        }

    }
     
}
