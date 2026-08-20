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
using Newtonsoft.Json.Linq;
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
using System.Globalization;
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
using System.Windows.Input;
using System.Xml;
using WinniesMessageBox;
using static DGISApp.DigitalSign;
using static iText.Signatures.PdfSigner;
using static ValidateCertificate.ValidateCert;
using Brushes = System.Windows.Media.Brushes;
using Console = System.Console;
using System.Text.RegularExpressions;
using System.Windows.Media;
using iText.Kernel.Pdf;
using iText.Kernel;

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
        bool IsLocalToken = bool.Parse(ConfigurationManager.AppSettings["IsLocalToken"]);

        Aes myAes = Aes.Create();



        public SFTU()
        {
            InitializeComponent();
            dpValidity.DisplayDateStart = DateTime.Today;
        }
        private void PasswordBox_PreviewExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Copy ||
                e.Command == ApplicationCommands.Cut ||
                e.Command == ApplicationCommands.Paste)
            {
                e.Handled = true;
            }
        }
        private void TextPassword_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Block normal space and Shift + Space
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void TextPassword_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                DataObject.AddPastingHandler(
                    passwordBox,
                    TextPassword_Pasting
                );
            }
        }

        private void TextPassword_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            string pastedText = e.DataObject.GetData(DataFormats.Text) as string ?? string.Empty;

            // Do not allow pasted text containing spaces or whitespace
            if (pastedText.Any(char.IsWhiteSpace))
            {
                e.CancelCommand();
            }
        }
        private void txtMacAddress_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow only hex characters
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9a-fA-F]+$");
        }
        private void txtMacAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string input = textBox.Text;

            // 🔹 Step 1: Remove everything except hex
            string clean = Regex.Replace(input.ToUpper(), @"[^0-9A-F]", "");

            // 🔹 Step 2: Limit to 12 chars
            if (clean.Length > 12)
                clean = clean.Substring(0, 12);

            // 🔹 Step 3: Format with ONLY :
            string formatted = "";
            for (int i = 0; i < clean.Length; i++)
            {
                if (i > 0 && i % 2 == 0)
                    formatted += ":";

                formatted += clean[i];
            }

            // 🔹 Prevent infinite loop
            textBox.TextChanged -= txtMacAddress_TextChanged;
            textBox.Text = formatted;
            textBox.SelectionStart = textBox.Text.Length;
            textBox.TextChanged += txtMacAddress_TextChanged;

            // 🔹 Validation
            if (Regex.IsMatch(formatted, @"^([0-9A-F]{2}:){5}([0-9A-F]{2})$"))
            {
                textBox.BorderBrush = Brushes.Green;
            }
            else
            {
                textBox.BorderBrush = Brushes.Red;
            }
        }
        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            string input = textBox.Text;

            // Allow only A-Z, a-z, 0-9 and underscore (_)
            string clean = Regex.Replace(input, @"[^A-Za-z0-9_]", "");

            // Prevent TextChanged from firing again
            textBox.TextChanged -= txtUsername_TextChanged;

            textBox.Text = clean;

            // Keep cursor at the end
            textBox.SelectionStart = textBox.Text.Length;

            textBox.TextChanged += txtUsername_TextChanged;
        }
        private void DropList_DragEnter(object sender, DragEventArgs e)
        {
        }

        public bool IsPdfPasswordProtected(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException(filePath);

            if (!string.Equals(Path.GetExtension(filePath), ".pdf", StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument pdf = new PdfDocument(reader))
                {
                    return false;
                }
            }
            catch (PdfException ex)
            {
                if (ex.Message.IndexOf("password", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    ex.Message.IndexOf("encrypted", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                throw;
            }
        }

        private void DropList_Drop(object sender, DragEventArgs e)
        {

            try
            {
                if (!CheckValidation())
                    return;
                if (Encrypt.IsChecked == true)
                {

                    if (Encrypt_pass.IsChecked == true)
                    {
                        if (Service1.ValidatePassword(textpassword.Password.ToString()))
                        {

                            string Password = textpassword.Password.ToString();

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

                                string macAddress = GetCombinedValue();
                                droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                                foreach (var path in droppedFilePaths)
                                {
                                    if (IsPdfPasswordProtected(path))
                                    {
                                        MyMessageBox.ShowDialog("Password-protected files are not supported");
                                        return;
                                    }
                                }

                                fileEncrypt(droppedFilePaths, macAddress);

                            }
                        }
                        else
                        {
                            textpassword.Clear();
                            MyMessageBox.ShowDialog("Password Length should be between 4 to 16 Characters.");
                            return;
                        }

                    }
                    else
                    {
                        droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                        string macAddress = GetCombinedValue();

                        if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                        {
                            foreach (var path in droppedFilePaths)
                            {
                                if (IsPdfPasswordProtected(path))
                                {
                                    MyMessageBox.ShowDialog("Password-protected files are not supported");
                                    return;
                                }
                            }
                            fileEncrypt(droppedFilePaths, macAddress);
                        }
                    }
                }
                else
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                    {

                        droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
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
        private string GetCombinedValue()
        {
            string macAddress = txtMacAddress.Text.Trim();
            string username = txtUsername.Text.Trim();
            string dateValidity = dpValidity.Text.Trim();
            bool isDigitalSign = chkDigitalSign.IsChecked.Value;
            return $"{macAddress}|{username}|{dateValidity}|{isDigitalSign}";
        }
        private bool CheckValidation()
        {
            if (Encrypt.IsChecked == true)
            {
                if (Encrypt_pass.IsChecked == true)
                {
                    if (textpassword.Password == "")
                    {
                        MyMessageBox.ShowDialog("Please Enter Password");
                        return false;
                    }
                }

                if (Encrypt_Asymetric.IsChecked == true)
                {
                    if (textpassword.Password == "" && RArmyNo.IsChecked == false && RName.IsChecked == false)
                    {
                        MyMessageBox.ShowDialog("Please Get PublicKey");
                        return false;
                    }

                    if (RArmyNo.IsChecked == true || RName.IsChecked == true)
                    {
                        if (txtSearch.Text == "")
                        {
                            MyMessageBox.ShowDialog("Please Search Name/ArmyNo");
                            return false;
                        }
                    }
                }

                if (chkMacAddress.IsChecked == true)
                {
                    string macAddress = txtMacAddress.Text.Trim();

                    if (string.IsNullOrEmpty(macAddress))
                    {
                        MyMessageBox.ShowDialog("Please enter MAC Address.");
                        return false;
                    }

                    if (!Regex.IsMatch(macAddress, @"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$"))
                    {
                        MyMessageBox.ShowDialog("Please enter a valid MAC Address (e.g. D8-BB-C1-E4-EF-90).");
                        return false;
                    }
                }
                else
                {
                    txtMacAddress.Text = null;
                }

                if (chkUsername.IsChecked == true)
                {
                    if (txtUsername.Text == "")
                    {
                        MyMessageBox.ShowDialog("Please Enter Username");
                        return false;
                    }

                }
                else
                {
                    txtUsername.Text = null;
                }

                if (chkValidity.IsChecked == true)
                {
                    if (dpValidity.SelectedDate == null)
                    {
                        MyMessageBox.ShowDialog("Please Enter Validity");
                        return false;
                    }

                }
                else
                {
                    dpValidity.SelectedDate = null;
                }
            }
            else
            {
                if (Export_pass.IsChecked == true && textpassword.Password == "")
                {
                    MyMessageBox.ShowDialog("Please Enter Password");
                    return false;
                }
            }

            return true; // everything is valid
        }
        private void resetAllFields()
        {

            txtDefaultPass.Text = "Please Enter Password for Encryption :";

            textpassword.Password = "";
            chkDigitalSign.IsChecked = false;
            chkMacAddress.IsChecked = false;
            txtMacAddress.Text = "";
            chkUsername.IsChecked = false;
            txtUsername.Text = "";
            chkValidity.IsChecked = false;
            dpValidity.Text = "";

        }
        public async void fileEncrypt(string[] files, string macAddress)
        {

            string DownloadPath = "";
            int totalFiles = files.Count();
            int processedFiles = 0;

            foreach (var path in files)
            {
                string MacAddress = null;
                if (!string.IsNullOrEmpty(macAddress))
                {
                    MacAddress = macAddress;
                }
                ConfigurationManager.AppSettings["LastSelectedLocation"] = System.IO.Path.GetDirectoryName(path);
                DownloadPath = System.IO.Path.GetDirectoryName(path);

                FileInfo fi = new FileInfo(path);
                if (fi.Length < 524288000)
                {
                    byte[] expectedHeader = System.Text.Encoding.UTF8.GetBytes("ASDC_AESGCM256");
                    byte[] fileHeader = new byte[expectedHeader.Length];

                    using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        fs.Read(fileHeader, 0, fileHeader.Length);
                    }

                    var IsFileEncrypted = expectedHeader.SequenceEqual(fileHeader);
                    if (fi.Extension == ".mil" || fi.Extension == ".MIL")
                    {
                        MyMessageBox.ShowDialog("Files with the .mil extension are not allowed");
                        break;
                    }
                    if (IsFileEncrypted)
                    {
                        MyMessageBox.ShowDialog("This File is Encrypted.");
                        break;
                    }
                    byte[] fileData = File.ReadAllBytes(path);
                    FileStream stream = File.OpenRead(path);
                    byte[] bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    stream.Read(bytes, 0, bytes.Length);
                    stream.Close();
                    bool EncryptPass = false;
                    if (Encrypt_pass.IsChecked == true)
                    {
                        EncryptPass = true;
                    }
                    await System.Threading.Tasks.Task.Run(async () =>
                    {
                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));
                        byte[] magicHeader = Encoding.UTF8.GetBytes("ASDC_AESGCM256");
                        bool encryptResult = false;
                        string signedPath = null;
                        string Output = null;

                        bool isDigitalSign = Convert.ToBoolean(MacAddress.Split('|')[3]);
                        if (isDigitalSign)
                        {
                            var (sigPath, hash) = await GenericSignFileAsyncForSecureFile(path);
                            signedPath = sigPath;

                            //signedPath = await GenericSignFileAsyncForSecureFile(path);

                            if (string.IsNullOrEmpty(signedPath))
                            {

                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                return;
                            }
                            MacAddress = MacAddress + "|" + hash.ToString();
                        }

                        if (EncryptPass)
                        {
                            string password = textpassword.Password.ToString();

                            if (!string.IsNullOrWhiteSpace(password))
                            {
                                byte[] encrypted = AesGcm256.SimpleEncryptWithPasswordForSecureFile(bytes, password, MacAddress);

                                Output = Path.Combine(
                                    DownloadPath,
                                    fi.Name + "_AES_" +
                                    DateTime.Now.ToString("ddMMM") + "_" +
                                    DateTime.Now.Millisecond + ".mil"
                                );

                                File.WriteAllBytes(Output, encrypted);

                                encryptResult = true;   // ✅ Make sure result is set
                            }
                            else
                            {
                                encryptResult = false;
                            }
                        }
                        else
                        {
                            string rsaKeyXml = textpassword.Password.ToString();

                            Output = DownloadPath + "\\" + fi.Name + "_RSA_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + ".mil";

                            if (!string.IsNullOrWhiteSpace(rsaKeyXml))
                                encryptResult = Service1.EncryptFile(path, Output, rsaKeyXml, magicHeader, MacAddress);
                        }



                        if (encryptResult)
                        {
                            string zipPath = CreateZip(path, signedPath, Output);

                            try
                            {

                                if (!string.IsNullOrWhiteSpace(signedPath) && File.Exists(signedPath))
                                    File.Delete(signedPath);

                                if (!string.IsNullOrWhiteSpace(Output) && File.Exists(Output))
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

                                resetAllFields();
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
                    });
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
            lblFileEncryption.Content = "Password Encryption - Quick Steps";
            lblStep1.Content = "Step 1: Enter Password – Provide a strong password for encryption.";
            lblStep2.Content = "Step 2: Choose Security Options – Select any required options (Digital Signature, MAC Address, Username, Validity).";
            lblStep3.Content = "Step 3: Upload File(s) – Drag & drop files or click Select File(s).";
            lblStep4.Content = "Step 4: Encryption Complete – The encrypted file is saved in the same location as the original with .Zip extension.";
            lblStep5.Content = "Step 5. Share Securely – Send the encrypted file and communicate the password through a separate secure channel.";
            lblNote.Content = "Note : Use Asymmetric encryption only for one-to-one file sharing as matching public-private Key pair\r\n           (IACA token) can only encrypt/ decrypt the file.";

            Encrypt.IsChecked = true;
            Encrypt_pass.IsChecked = true;
            txtDefaultPass.Text = "Please Enter Password for Encryption :";

            EncryptionOptionGrid.Visibility = Visibility.Visible;
            EncryptionSecureGrid.Visibility = Visibility.Visible;
            txtDefaultPass.Visibility = Visibility.Visible;
            ExportSecureGrid.Visibility = Visibility.Collapsed;
            txtSearch.Visibility = Visibility.Hidden;
            RArmyNo.Visibility = Visibility.Hidden;
            btnGetPublicKey.Visibility = Visibility.Hidden;
            textpassword.Visibility = Visibility.Visible;
            txtDefaultPasswarnning.Visibility = Visibility.Hidden;
            textpassword.MaxLength = 5000;
            textpassword.Password = "";
            HintAssist.SetHint(textpassword, "Please Enter Password");
            Export_pass.IsChecked = false;
            Export_Asymetric.IsChecked = false;
            EncryptionWrapPanal.Visibility = Visibility.Visible;
            ExportWrapPanal.Visibility = Visibility.Collapsed;

            chkDigitalSign.IsChecked = false;
            chkMacAddress.IsChecked = false;
            txtMacAddress.Text = "";
            chkUsername.IsChecked = false;
            txtUsername.Text = "";
            chkValidity.IsChecked = false;
            dpValidity.Text = "";

        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            lblFileEncryption.Content = "Export Secures File";
            lblStep1.Content = "Step 1: Enter the password that was used to secure the file.";
            lblStep2.Content = "Step 2: Select or drag & drop the encrypted .mil file.";
            lblStep3.Content = "Step 3: The application will verify the password and decrypt the file.";
            lblStep4.Content = "Step 4: If verification is successful, the extracted file will be saved in the original file location.";
            lblStep5.Content = "";
            lblNote.Content = "Note : Extraction will fail if the password is incorrect or the secure file is invalid.";

            Encrypt.IsChecked = false;
            EncryptionWrapPanal.Visibility = Visibility.Hidden;
            EncryptionOptionGrid.Visibility = Visibility.Hidden;
            EncryptionSecureGrid.Visibility = Visibility.Hidden;
            ExportSecureGrid.Visibility = Visibility.Visible;
            RArmyNo.Visibility = Visibility.Hidden;
            btnGetPublicKey.Visibility = Visibility.Hidden;

            Export_pass.IsChecked = true;
            Export_Asymetric.IsChecked = false;
            EncryptionWrapPanal.Visibility = Visibility.Collapsed;
            ExportWrapPanal.Visibility = Visibility.Visible;
            textpassword.Visibility = Visibility.Visible;
            textpassword.Password = "";
            HintAssist.SetHint(textpassword, "Please Enter Password");

            txtDefaultPass.Text = "Please Enter Password for Encryption :";
            EncryptionOptionGrid.Visibility = Visibility.Visible;

            txtSearch.Visibility = Visibility.Hidden;
            txtDefaultPasswarnning.Visibility = Visibility.Hidden;
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
                textpassword.Visibility = Visibility.Hidden;
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
        private void Encrypt_Click_pass(object sender, RoutedEventArgs e)
        {
            lblFileEncryption.Content = "Password Encryption - Quick Steps";
            lblStep1.Content = "Step 1: Enter Password – Provide a strong password for encryption.";
            lblStep2.Content = "Step 2: Choose Security Options – Select any required options (Digital Signature, MAC Address, Username, Validity).";
            lblStep3.Content = "Step 3: Upload File(s) – Drag & drop files or click Select File(s).";
            lblStep4.Content = "Step 4: Encryption Complete – The encrypted file is saved in the same location as the original.";
            lblStep5.Content = "Step 5. Share Securely – Send the encrypted file and communicate the password through a separate secure channel.";
            lblNote.Content = "Note : Use Asymmetric encryption only for one-to-one file sharing as matching public-private Key pair\r\n           (IACA token) can only encrypt/ decrypt the file.";



            Encrypt.IsChecked = true;
            Encrypt_pass.IsChecked = true;
            RArmyNo.IsChecked = false;
            txtDefaultPass.Text = "Please Enter Password for Encryption :";
            RArmyNo.Visibility = Visibility.Hidden;
            btnGetPublicKey.Visibility = Visibility.Hidden;
            textpassword.Visibility = Visibility.Visible;
            txtDefaultPasswarnning.Visibility = Visibility.Hidden;
            textpassword.MaxLength = 5000;
            textpassword.Password = "";
            HintAssist.SetHint(textpassword, "Please Enter Password");
            txtSearch.Visibility = Visibility.Hidden;


        }

        private void Encrypt_Click_Asymetric(object sender, RoutedEventArgs e)
        {


            lblFileEncryption.Content = "Asymmetric (Public-Key) Encryption (One to One Sharing)";
            lblStep1.Content = "Step 1: Convey recipient to insert IACA token and click ‘Get Public Key’/ Army No’.";
            lblStep2.Content = "Step 2: Choose Security Options – Select any required options (Digital Signature, MAC Address, Username, Validity).";
            lblStep3.Content = "Step 3: Paste the recipient’s public key in text box below and Upload File(s) – Drag & drop files or click Select File(s).";
            lblStep4.Content = "Step 4: Encryption Complete – The encrypted file is saved in the same location as the original with .Zip extension.";
            lblStep5.Content = "Step 5. Share Securely – Send the encrypted file and communicate the password through a separate secure channel.";
            lblNote.Content = "Note : Use Asymmetric encryption only for one-to-one file sharing as matching public-private Key pair\r\n           (IACA token) can only encrypt/ decrypt the file.";


            Encrypt.IsChecked = true;
            Encrypt_pass.IsChecked = false;
            Encrypt_Asymetric.IsChecked = true;
            RArmyNo.IsChecked = false;
            txtSearch.Visibility = Visibility.Hidden;
            txtDefaultPass.Visibility = Visibility.Visible;
            txtDefaultPass.Text = "Please fetch public key of inserted token :";
            RArmyNo.Visibility = Visibility.Visible;
            btnGetPublicKey.Visibility = Visibility.Visible;
            textpassword.Visibility = Visibility.Hidden;
            txtDefaultPasswarnning.Visibility = Visibility.Hidden;
            textpassword.MaxLength = 5000;
            textpassword.Password = "";
            EncryptionWrapPanal.Visibility = Visibility.Visible;
            EncryptionOptionGrid.Visibility = Visibility.Visible;
            EncryptionSecureGrid.Visibility = Visibility.Visible;
            ExportWrapPanal.Visibility = Visibility.Collapsed;
            ExportSecureGrid.Visibility = Visibility.Collapsed;


        }

        private void Export_Click_pass(object sender, RoutedEventArgs e)
        {
            lblFileEncryption.Content = "Export Secures File";
            lblStep1.Content = "Step 1: Enter the password that was used to secure the file.";
            lblStep2.Content = "Step 2: Select or drag & drop the encrypted .mil file.";
            lblStep3.Content = "Step 3: The application will verify the password and decrypt the file.";
            lblStep4.Content = "Step 4: If verification is successful, the extracted file will be saved in the original file location.";
            lblStep5.Content = "";
            lblNote.Content = "Note : Extraction will fail if the password is incorrect or the secure file is invalid.";


            Encrypt.IsChecked = false;
            Export_pass.IsChecked = true;
            Export_Asymetric.IsChecked = false;
            txtDefaultPass.Visibility = Visibility.Visible;
            txtDefaultPass.Text = "Please Enter Password for Encryption :";
            EncryptionOptionGrid.Visibility = Visibility.Visible;
            // EncryptionWrapPanal.Visibility = Visibility.Collapsed;
            //ExportWrapPanal.Visibility=Visibility.Visible;
            // EncryptionOptionGrid.Visibility = Visibility.Hidden;
            // EncryptionSecureGrid.Visibility = Visibility.Hidden;
            // txtDefaultPass.Visibility = Visibility.Hidden;
            ExportSecureGrid.Visibility = Visibility.Visible;
            RArmyNo.Visibility = Visibility.Hidden;
            btnGetPublicKey.Visibility = Visibility.Hidden;
            textpassword.Visibility = Visibility.Visible;
            txtDefaultPasswarnning.Visibility = Visibility.Hidden;
            textpassword.MaxLength = 5000;
            textpassword.Password = "";
            HintAssist.SetHint(textpassword, "Please Enter Password");



        }
        private void Export_Click_Asymetric(object sender, RoutedEventArgs e)
        {


            lblFileEncryption.Content = "Asymmetric (Public-Key) Extraction";
            lblStep1.Content = "Step 1: Convey recipient to insert IACA token in PC";
            lblStep2.Content = "Step 2: Select or drag & drop the encrypted .mil file.";
            lblStep3.Content = "Step 3: The application will verify the password and decrypt the file.";
            lblStep4.Content = "Step 4: If verification is successful, the extracted file will be saved in the original file location..";
            lblStep5.Content = "";
            lblNote.Content = "Note : Extraction will fail if the IACA token invalid is incorrect or the secure file is invalid.";


            Encrypt.IsChecked = false;
            EncryptionOptionGrid.Visibility = Visibility.Hidden;
            Export_pass.IsChecked = false;
            Export_Asymetric.IsChecked = true;
            EncryptionWrapPanal.Visibility = Visibility.Collapsed;
            ExportWrapPanal.Visibility = Visibility.Visible;
            EncryptionOptionGrid.Visibility = Visibility.Hidden;
            EncryptionSecureGrid.Visibility = Visibility.Hidden;
            txtDefaultPass.Visibility = Visibility.Hidden;
            ExportSecureGrid.Visibility = Visibility.Visible;
            RArmyNo.Visibility = Visibility.Hidden;
            btnGetPublicKey.Visibility = Visibility.Hidden;
            textpassword.Visibility = Visibility.Hidden;
            txtDefaultPasswarnning.Visibility = Visibility.Hidden;
            textpassword.MaxLength = 5000;
            textpassword.Password = "";


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
                textpassword.Visibility = Visibility.Hidden;
                textpassword.Password = "";
                txtDefaultPasswarnning.Visibility = Visibility.Hidden;
                txtSearch.Visibility = Visibility.Hidden;
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
                    textpassword.Visibility = Visibility.Visible;
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
                if (!CheckValidation())
                    return;
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (Encrypt.IsChecked == true)
                {

                    if (Encrypt_pass.IsChecked == true)
                    {
                        if (Service1.ValidatePassword(textpassword.Password.ToString()))
                        {

                            string Password = textpassword.Password.ToString();

                            byte[] Mykey = null;

                            if (string.IsNullOrWhiteSpace(Password) || Password.Length < AesGcm256.MinPasswordLength)
                                throw new ArgumentException(String.Format("Please enter password with atleast {0} characters as per ACSP-2017.", AesGcm256.MinPasswordLength));

                            byte[] Hashbytes = Encoding.Unicode.GetBytes(Password);
                            SHA256Managed hashstring = new SHA256Managed();
                            Mykey = hashstring.ComputeHash(Hashbytes);
                            byte[] MyIV = Encoding.ASCII.GetBytes(Password.PadRight(16, ' '));

                            myAes.Key = Mykey;
                            myAes.IV = MyIV;

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
                                string macAddress = GetCombinedValue();
                                foreach (var path in openFileDialog.FileNames)
                                {
                                    if (IsPdfPasswordProtected(path))
                                    {
                                        MyMessageBox.ShowDialog("This file is password protected, please select not protected file");
                                        return;
                                    }
                                }
                                fileEncrypt(openFileDialog.FileNames, macAddress);

                            }
                        }
                        else
                        {
                            textpassword.Clear();
                            MyMessageBox.ShowDialog("Password Length should be between 4 to 16 Characters.");
                            return;
                        }

                    }
                    else
                    {
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
                            string macAddress = GetCombinedValue();
                            foreach (var path in openFileDialog.FileNames)
                            {
                                if (IsPdfPasswordProtected(path))
                                {
                                    MyMessageBox.ShowDialog("This file is password protected, please select not protected file");
                                    return;
                                }
                            }
                            fileEncrypt(openFileDialog.FileNames, macAddress);

                        }
                    }
                }
                else
                {
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
                        ExportSingleFileAsync(openFileDialog.FileNames);


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
        private void ShowSuggestions(bool show)
        {
            // Popup may not exist in old view mode; safe guard
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
        private async Task<(string SigPath, string sign)> GenericSignFileAsyncForSecureFile(string filePath)

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
                    return (null, null);
                }

                string pattern = @"^[a-zA-Z0-9@, ._\-]+$";
                if (!Regex.IsMatch(remark, pattern) && remark != "")
                {
                    ShowMsg("Special Characters Not Allow ");
                    return (null, null);
                }

                HelperCert helperCert = new HelperCert();
                var result = await helperCert.CheckSomethingAsync();

                if (result.Status == "0" || result.Status == "-1")
                {
                    ShowMsg(result.Remark);
                    return (null, null);
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
                            return (null, null);
                        }
                    }
                    else if (crloscp == false)
                    {
                        ShowMsg(crlocspmsg == "Digital Cert of token cannot be verified with CA due to Network issues"
                            ? crlocspmsg
                            : "CRL Check Failed !");
                        return (null, null);
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
                        ShowMsg("No valid certificate was found on the inserted token");
                        return (null, null);
                    }
                    cert = found[0];
                }

                if (DateTime.Now > cert.NotAfter && !IsLocalToken)
                {
                    ShowMsg("The certificate on the inserted token has expired. Please use a token with a valid certificate and try again !");
                    return (null, null);
                }

                // ✅ Sign (background/parallel in service)
                var (sigPath, hash) = await HugeFileSignatureService.SignPortableAsync(
                     filePath, cert, UpdateProgress, remark);
                //string sigPath = await HugeFileSignatureService.SignPortableAsync(
                //    filePath, cert, UpdateProgress, remark);
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
                    // await new Service1().SaveDigitalSignedDataToAnalytics(saveDigitalSignInfo);
                }
                return (sigPath, hash);



            }
            catch (Exception ex)
            {
                ShowMsg(ex.Message);
                ErrorLog.LogErrorToFile(ex);
                return (null, null);
            }
            finally
            {

            }
        }
        private System.Threading.Tasks.Task ExportSingleFileAsync(string[] files)
        {
            bool ExportPass = false;
            if (Export_pass.IsChecked == true)
            {
                ExportPass = true;
            }
            return System.Threading.Tasks.Task.Run(async () =>
            {
                try
                {

                    string path = files[0];
                    ConfigurationManager.AppSettings["LastSelectedLocation"] = System.IO.Path.GetDirectoryName(path);
                    string DownloadPath = System.IO.Path.GetDirectoryName(path);
                    int ret1 = 0;

                    FileInfo fi = new FileInfo(path);
                    if (fi.Length <= 524288000)
                    {
                        if (fi.Extension == ".mil" || fi.Extension == ".MIL")
                        {

                            string gmacDetails = "";
                            FileStream stream1 = File.OpenRead(path);
                            byte[] bytes1 = new byte[stream1.Length];
                            stream1.Read(bytes1, 0, bytes1.Length);

                            stream1.Close();

                            char dd = '_';
                            int levelOfEncryption = fi.FullName.Count(s => s == dd);

                            string filePath = DownloadPath + "\\" + fi.Name.Split('.')[0];

                            new Thread(async () =>
                            {

                                if (ExportPass)
                                {
                                    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                                    this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));
                                    string macAddress = null;
                                    string useraname = null;
                                    DateTime? validityDate = null;
                                    string mac;
                                    byte[] roundtrip = AesGcm256.SimpleDecryptWithPasswordForSecureFile(bytes1, textpassword.Password.ToString(), out mac);
                                    if (roundtrip == null)
                                    {
                                        ret1 = 0;
                                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                        this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Password incorrect or File is Tempered !")));
                                        return;
                                    }
                                    else
                                    {
                                        string fileName = fi.Name.Split('_')[0];
                                        filePath = DownloadPath + "\\" + fileName;

                                        using (Stream file = File.OpenWrite(filePath))
                                        {

                                            file.Write(roundtrip, 0, roundtrip.Length);
                                        }
                                        bool res = await HandleReturnCodeAsync(ret1, path, mac);

                                        if (res)
                                        {
                                            await System.Threading.Tasks.Task.Delay(5000);
                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                            var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                            {
                                                resetAllFields();

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
                                            if (File.Exists(filePath))
                                                File.Delete(filePath);
                                            await System.Threading.Tasks.Task.Delay(5000);
                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                            return;
                                        }

                                    }
                                }
                                else
                                {
                                    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                                    this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));

                                    filePath = DownloadPath + "\\" + fi.Name.Split('_')[0];
                                    X509Certificate2Collection fcollection = await helper.GetCertificates();

                                    if (fcollection.Count == 0)
                                    {
                                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                        this.Dispatcher.Invoke(() =>
                                        {
                                            MyMessageBox.ShowDialog(
                                                "Token not detected. Please insert the IACA token and try again !");
                                        });
                                        return;
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
                                            string macDetails;   // declare variable first

                                            ret1 = Service1.DecryptFile(path, filePath, cert1, out macDetails);
                                            // ret1 = Service1.DecryptFile(path, filePath, cert1);
                                            if (!string.IsNullOrWhiteSpace(macDetails))
                                                gmacDetails = macDetails;
                                        }
                                    }

                                    if (ret1 == 0)
                                    {
                                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                        var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                        {
                                            return MyMessageBox.Show("Verification failed.\n The provided token is invalid, or the file has been modified.\n Please use the correct token and the original, unmodified file.");
                                        }));

                                    }
                                    else
                                    {
                                        bool res = await HandleReturnCodeAsync(ret1, path, gmacDetails);

                                        if (res)
                                        {
                                            await System.Threading.Tasks.Task.Delay(5000);
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
                                            string fileName = fi.Name.Split('_')[0];
                                            filePath = DownloadPath + "\\" + fileName;
                                            if (File.Exists(filePath))
                                                File.Delete(filePath);
                                            await System.Threading.Tasks.Task.Delay(5000);
                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                            return;
                                        }
                                    }

                                }

                            }).Start();

                        }
                        else
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                MyMessageBox.ShowDialog(
                                    "File format not supported. Please Select .mil file.");
                            });

                        }
                    }
                    else
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            MyMessageBox.ShowDialog(
                                "File size is too large! Max size is 500 MB.");
                        });

                    }

                }
                catch (Exception)
                {
                    MyMessageBox.ShowDialog("Invaild File....");
                }
            });
        }
        private async System.Threading.Tasks.Task<bool> HandleReturnCodeAsync(int ret1, string path, string macDetails)
        {
            try
            {
                var failures = new List<string>();
                var steps = new List<(string message, bool success)>();
                string macAddress = macDetails.Split('|')[0];
                string useraname = macDetails.Split('|')[1];

                var dateString = macDetails.Split('|')[2];
                DateTime? validityDate = null;


                string[] formats =
                {
                "dd-MMM-yy",
                "dd-MMM-yyyy",
                "dd-MM-yy",
                "dd-MM-yyyy",
                "yyyy-MM-dd"
            };

                if (!string.IsNullOrWhiteSpace(dateString) &&
                    DateTime.TryParseExact(dateString,
                                           formats,
                                           CultureInfo.InvariantCulture,
                                           DateTimeStyles.None,
                                           out DateTime parsedDate))
                {
                    validityDate = parsedDate.Date;
                }
                bool isDigitalSign = Convert.ToBoolean(macDetails.Split('|')[3]);

                if (!string.IsNullOrEmpty(macDetails))
                {

                    var service = new Service1();
                    var macResponse = service.GetMacAddress().GetAwaiter().GetResult();

                    if (isDigitalSign)
                    {
                        string hash = macDetails.Split('|')[4];
                        string DownloadPath = System.IO.Path.GetDirectoryName(path);
                        var ok = (false, "");
                        FileInfo fin = new FileInfo(path);
                        string fileName = fin.Name.Split('_')[0];
                        string file = DownloadPath + "\\" + fileName;
                        var signatureFile = file + ".sig.json";

                        if (File.Exists(signatureFile))
                        {

                            ok = await HugeFileSignatureService.VerifyPortableAsync(file, signatureFile, UpdateProgress, hash);
                        }

                        if (ok.Item1)
                        {
                            steps.Add(("Digital Sign Verified", true));
                        }
                        else
                        {
                            string error = "";
                            if (ok.Item2 == "")
                            {
                                error = "Digital sign not verified";
                            }
                            else
                            {
                                error = ok.Item2;
                            }
                            failures.Add(error);
                            steps.Add((error, false));
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(macAddress))
                    {
                        string NormalizeMac(string mac)
                        {
                            return Regex.Replace(mac, "[^0-9A-Fa-f]", "").ToUpper();
                        }
                        var m1 = NormalizeMac(macResponse.MacAddress);
                        var m2 = NormalizeMac(macAddress);
                        if (m1 != m2)
                        {
                            string error = "Destination Mac Address Not Matched " + "\n" + macAddress;
                            failures.Add(error);
                            steps.Add((error, false));
                        }
                        else
                        {
                            steps.Add(("Destination Mac Address Matched", true));
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(useraname))
                    {
                        if (macResponse.WindowsUserName.ToUpper().Trim() != useraname.ToUpper().Trim())
                        {
                            string error = "Destination Username Not Matched " + "\n" + useraname;
                            failures.Add(error);
                            steps.Add((error, false));
                        }
                        else
                        {
                            steps.Add(("Destination Username Matched", true));
                        }
                    }
                    if (validityDate.HasValue)
                    {
                        if (DateTime.Now.Date > validityDate.Value)
                        {
                            string error = "Validity " + validityDate.Value.ToString("dd-MM-yyyy");
                            failures.Add(error);
                            steps.Add((error, false));
                        }
                        else
                        {
                            steps.Add(("Validity", true));
                        }
                    }
                    if (steps.Count > 0)
                    {
                        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
                        {
                            var popup = new PopupWin();
                            popup.Show();

                            foreach (var s in steps)
                            {
                                await popup.RunProcessStep(s.message, s.success);
                            }
                        });
                    }

                    if (failures.Count > 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }



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
                if (!string.IsNullOrWhiteSpace(signedFile) && File.Exists(signedFile))
                {
                    zip.CreateEntryFromFile(signedFile, Path.GetFileName(signedFile));
                }
                if (!string.IsNullOrWhiteSpace(encryptedFile) && File.Exists(encryptedFile))
                {
                    zip.CreateEntryFromFile(encryptedFile, Path.GetFileName(encryptedFile));
                }
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
