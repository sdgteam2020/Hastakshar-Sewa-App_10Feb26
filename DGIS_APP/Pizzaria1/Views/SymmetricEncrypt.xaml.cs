 
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using SignService;
using SignService.Helpers;
using SignService.HttpClients;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using WinniesMessageBox;

namespace DGISApp
{
    public partial class SymmetricEncrypt : UserControl
    {
        string[] droppedFilePaths = null;
        string download = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads";

        Aes myAes = Aes.Create();



        public SymmetricEncrypt()
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
                if (Hash.IsChecked == true)
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                    {
                        droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                        GenerateHashForFiles(droppedFilePaths);
                    }
                    return;
                }


                if (textpassword.Password.ToString() == "")
                {
                    if (RDefault.IsChecked == true)
                        MyMessageBox.ShowDialog("Please Enter Password for Encryption.");
                    else if (Encrypt.IsChecked == true)
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

                string Password = textpassword.Password.ToString();
                if (RDefault.IsChecked == false)
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                    {
                        droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];


                        fileEncrypt(droppedFilePaths);


                    }
                }
                else if (Service1.ValidatePassword(textpassword.Password.ToString()))
                {
                    byte[] Mykey = null;

                    if (string.IsNullOrWhiteSpace(Password) || Password.Length < AesGcm256.MinPasswordLength)
                        throw new ArgumentException(String.Format("Please enter password with atleast {0} characters as per ACSP-2017.", AesGcm256.MinPasswordLength));


                    byte[] Hashbytes = Encoding.Unicode.GetBytes(Password);
                    SHA256Managed hashstring = new SHA256Managed();
                    Mykey = hashstring.ComputeHash(Hashbytes);


                    byte[] MyIV = Encoding.ASCII.GetBytes(Password.PadRight(16, ' '));

                    myAes.Key = Mykey;
                    myAes.IV = MyIV;



                    if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                    {
                        droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];


                        fileEncrypt(droppedFilePaths);


                    }
                }
                else
                {
                    if (RDefault.IsChecked == false)
                    {
                        textpassword.Clear();
                        MyMessageBox.ShowDialog("Please Enter PublicKey.");
                    }
                    else
                    {
                        textpassword.Clear();
                        MyMessageBox.ShowDialog("Password Length should be between 4 to 16 Characters.");
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


        static byte[] encryptdata(byte[] bytearraytoencrypt, byte[] Key, byte[] IV, int KeySize)
        {
            AesCryptoServiceProvider dataencrypt = new AesCryptoServiceProvider();
            dataencrypt.BlockSize = 128;
            dataencrypt.KeySize = KeySize;
            dataencrypt.Key = Key;
            dataencrypt.IV = IV;
            dataencrypt.Padding = PaddingMode.PKCS7;
            dataencrypt.Mode = CipherMode.CBC;
            ICryptoTransform crypto1 = dataencrypt.CreateEncryptor(dataencrypt.Key, dataencrypt.IV);
            byte[] encrypteddata = crypto1.TransformFinalBlock(bytearraytoencrypt, 0, bytearraytoencrypt.Length);
            crypto1.Dispose();
            return encrypteddata;
        }

        public void fileEncrypt(string[] files)
        {

            string DownloadPath = "";
            int totalFiles = files.Count();
            int processedFiles = 0;

            foreach (var path in files)
            {

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
                    bool R_Default = false;
                    if (RDefault.IsChecked == true)
                    {
                        R_Default = true;
                    }
                    new Thread(() =>
                    {
                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));

                        byte[] magicHeader = Encoding.UTF8.GetBytes("ASDC_AESGCM256");
                        bool encryptResult = false;
                        if (R_Default)
                        {


                            byte[] encrypted = AesGcm256.SimpleEncryptWithPassword(bytes, textpassword.Password.ToString());

                            using (Stream file = File.OpenWrite(DownloadPath + "\\" + fi.Name + "_AES_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + ".mil"))
                            {
                                file.Write(encrypted, 0, encrypted.Length);
                            }
                        }
                        else
                        {
                            string rsaKeyXml = textpassword.Password.ToString();

                            string Output = DownloadPath + "\\" + fi.Name + "_RSA_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + ".mil";

                            if (!string.IsNullOrWhiteSpace(rsaKeyXml))
                                encryptResult = Service1.EncryptFile(path, Output, rsaKeyXml, magicHeader);
                        }

                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));

                        processedFiles++;

                        if (processedFiles == totalFiles)
                        {
                            if (!encryptResult && !R_Default)
                            {
                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Invalid Public Key!")));
                                return;
                            }

                            var result = this.Dispatcher.Invoke(new Func<string>(() =>
                            {

                                return MyMessageBox.ShowDialog("Congratulations!\n\nDocument is successfully Encrypted.\n" + DownloadPath, MyMessageBox.Buttons.OK_PathOpen);
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

        private void Default_Click(object sender, RoutedEventArgs e)
        {
            lblFileEncryption.Content = "File Encryption (Single or Multiple Files)";
            lblStep1.Content = "Step 1: Enter Password for Encrypting file(s).";
            lblStep2.Content = "Step 2: Select single or Multiple files for Encryption and wait for few seconds.";
            lblStep3.Content = "Step 3: AES 256 Encrypted file (Originalfilena_me_AES_date_milisecond.mil) at original location.";
            lblStep4.Content = "Step 4: Click OK to acknowledge or Open Path to open folder containing encrypted file(s).";
            lblStep5.Content = "Step 5. Share file with .mil extn to the recipient.";
            lblNote.Content = "Note : File encrypted using Hastakshar SEWA can be decrypted by this App only and Original file is not changed.";

            txtDefaultPasswarnning.Visibility = Visibility.Visible;
            txtDefaultPasswarnning.Content = "Password Length should be between 4 to 16 Characters";
            txtDefaultPass.Visibility = Visibility.Visible;
            txtDefaultPass.Text = "Please Enter Password for Encryption :";
            Encrypt.IsChecked = false;

            txtSearch.Visibility = Visibility.Hidden;
            lstSuggestions.Visibility = Visibility.Hidden;

            RArmyNo.Visibility = Visibility.Hidden;
            RName.Visibility = Visibility.Hidden;
            textpassword.Visibility = Visibility.Visible;
            btnGetPublicKey.Visibility = Visibility.Hidden;
            textpassword.Password = "";
            HintAssist.SetHint(textpassword, "Enter Password");
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
            txtDefaultPasswarnning.Visibility = Visibility.Hidden;
            txtDefaultPasswarnning.Content = "Please Enter Recipient's Public Key";
            RDefault.IsChecked = false;
            RArmyNo.Visibility = Visibility.Visible;

            btnGetPublicKey.Visibility = Visibility.Visible;
            textpassword.Visibility = Visibility.Visible;
            HintAssist.SetHint(textpassword, "Enter Recipient's Public Key");
            textpassword.MaxLength = 5000;
            textpassword.Password = "";
            RArmyNo.IsChecked = false;
            RName.IsChecked = false;

            RArmyNo.IsChecked = false;
            txtSearch.Visibility = Visibility.Hidden;
            txtSearch.Text = "";
            ShowSuggestions(false);
        }

        private void Hash_Click(object sender, RoutedEventArgs e)
        {
            lblFileEncryption.Content = "Generate Hash Value (SHA-256)";
            lblStep1.Content = "Step 1: Select single or multiple files.";
            lblStep2.Content = "Step 2: System will calculate SHA-256 hash.";
            lblStep3.Content = "Step 3: Copy/verify hash output as required.";
            lblStep4.Content = "Step 4: Share hash for integrity verification.";
            lblStep5.Content = "";
            lblNote.Content = "Note : Hash is one-way. It is used to verify file integrity (tamper detection).";


            txtDefaultPass.Text = "Select file(s) to generate SHA-256 hash :";
            txtDefaultPasswarnning.Content = "";
            txtDefaultPasswarnning.Visibility = Visibility.Collapsed;

            Encrypt.IsChecked = false;
            RDefault.IsChecked = false;

            btnGetPublicKey.Visibility = Visibility.Hidden;
            RArmyNo.Visibility = Visibility.Hidden;
            RName.Visibility = Visibility.Hidden;

            txtSearch.Visibility = Visibility.Hidden;
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

                    string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                    string Appfolder = System.IO.Path.Combine(path, "DGIS");
                    Directory.CreateDirectory(Appfolder);
                    string filePath = System.IO.Path.Combine(Appfolder, "PublicKeyData.xml");
                    FileInfo fi = new FileInfo(filePath);
                    List<XmlDataForPublicKey> xmlDataForPublicKeys = new List<XmlDataForPublicKey>();
                    XmlDataForPublicKey xmlDataForPublicKey = new XmlDataForPublicKey();

                    string[] SubjectSplit = PublicKey.subject.Split(',');

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

                    xmlDataForPublicKey.Status = false;



                    xmlDataForPublicKeys.Add(xmlDataForPublicKey);

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

                    using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                    using (XmlWriter writer = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true }))
                    {
                        writer.WriteStartDocument();
                        writer.WriteStartElement("PublicKeysData");
                        serializer.WriteObject(writer, data);
                        writer.WriteEndElement();
                        writer.WriteEndDocument();
                    }
                }
                else
                {

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

                using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
                using (XmlWriter writer = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true }))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement("PublicKeysData");
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

                if (Hash.IsChecked == true)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Title = "Select File(s) for Hash";
                    openFileDialog.Multiselect = true;

                    if (ConfigurationManager.AppSettings["LastSelectedLocation"] == "")
                        openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                    else
                        openFileDialog.InitialDirectory = ConfigurationManager.AppSettings["LastSelectedLocation"];

                    if (openFileDialog.ShowDialog() == true)
                    {
                        GenerateHashForFiles(openFileDialog.FileNames);
                    }
                    return;
                }

                if (textpassword.Password.ToString() == "")
                {
                    if (RDefault.IsChecked == true)
                        MyMessageBox.ShowDialog("Please Enter Password for Encryption.");
                    else if (Encrypt.IsChecked == true)
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


                string Password = textpassword.Password.ToString();
                if (RDefault.IsChecked == false)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Title = "Select File for Enryption";
                    openFileDialog.Multiselect = true;

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

                        fileEncrypt(openFileDialog.FileNames);

                    }
                }
                else if (Service1.ValidatePassword(textpassword.Password.ToString()))
                {
                    byte[] Mykey = null;

                    if (RDefault.IsChecked == true)
                        if (string.IsNullOrWhiteSpace(Password) || Password.Length < AesGcm256.MinPasswordLength)
                            throw new ArgumentException(String.Format("Please enter password with atleast { 0 } characters as per ACSP - 2017.", AesGcm256.MinPasswordLength));



                    byte[] Hashbytes = Encoding.Unicode.GetBytes(Password);
                    SHA256Managed hashstring = new SHA256Managed();
                    Mykey = hashstring.ComputeHash(Hashbytes);

                    byte[] MyIV = Encoding.ASCII.GetBytes(Password.PadRight(16, ' '));

                    myAes.Key = Mykey;
                    myAes.IV = MyIV;




                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Title = "Select File for Enryption";
                    openFileDialog.Multiselect = true;

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


                        fileEncrypt(openFileDialog.FileNames);

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

            if (popSuggestions != null)
                popSuggestions.IsOpen = show;
        }

        private void ApplySuggestion(SuggestionItem selectedItem)
        {
            if (selectedItem == null) return;

            txtSearch.TextChanged -= txtSearch_TextChanged;
            txtSearch.Text = selectedItem.Text;
            txtSearch.CaretIndex = txtSearch.Text.Length;
            txtSearch.TextChanged += txtSearch_TextChanged;

            ShowSuggestions(false);

            textpassword.MaxLength = 5000;
            textpassword.Password = selectedItem.Value;

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

        private string ComputeFileHash(string filePath, string algorithm = "SHA256")
        {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                HashAlgorithm hasher;

                algorithm = (algorithm ?? "SHA256").ToUpper();

                if (algorithm == "MD5")
                    hasher = MD5.Create();
                else if (algorithm == "SHA1")
                    hasher = SHA1.Create();
                else if (algorithm == "SHA384")
                    hasher = SHA384.Create();
                else if (algorithm == "SHA512")
                    hasher = SHA512.Create();
                else
                    hasher = SHA256.Create();

                using (hasher)
                {
                    byte[] hash = hasher.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
        }

        private void GenerateHashForFiles(string[] files)
        {
            if (files == null || files.Length == 0) return;

            int totalFiles = files.Length;
            int processedFiles = 0;


            this.Dispatcher.Invoke(() => BusyBar.IsBusy = true);
            this.Dispatcher.Invoke(() => DropList.IsEnabled = false);

            new Thread(() =>
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.AppendLine("Hash Algorithm : SHA-256");
                    sb.AppendLine("========================================");
                    sb.AppendLine();

                    foreach (var path in files)
                    {
                        if (!File.Exists(path))
                            continue;

                        FileInfo fi = new FileInfo(path);


                        string hash = ComputeFileHash(path, "SHA256");

                        sb.AppendLine($"File : {fi.FullName}");
                        sb.AppendLine($"SHA256: {hash}");
                        sb.AppendLine("----------------------------------------");
                        sb.AppendLine();

                        string outPath = Path.Combine(fi.DirectoryName, fi.Name + $"_{DateTime.Now.ToString("ddMMM")}_{DateTime.Now.Millisecond}" + ".hash.txt");
                        File.WriteAllText(outPath, $"SHA256:{hash}{Environment.NewLine}{fi.FullName}");

                        processedFiles++;
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        BusyBar.IsBusy = false;
                        DropList.IsEnabled = true;

                        if (processedFiles == 0)
                        {
                            MyMessageBox.ShowDialog("No valid file found to generate hash.");
                            return;
                        }

                        MyMessageBox.ShowDialog(sb.ToString());
                    });
                }
                catch (Exception ex)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        BusyBar.IsBusy = false;
                        DropList.IsEnabled = true;
                        MyMessageBox.ShowDialog("Hash generation failed: " + ex.Message);
                    });

                    ErrorLog.LogErrorToFile(ex);
                }
            }).Start();
        }

    }
}
