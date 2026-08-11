using Microsoft.Win32;
using SignService;
using SignService.Helpers;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using WinniesMessageBox;
using Xceed.Document.NET;

namespace DGISApp
{

    public partial class SymmetricDecryption : UserControl
    {
        string[] droppedFilePaths = null;
        string download = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads";

        Aes myAes = Aes.Create();
        int ret1 = 0;
        public SymmetricDecryption()
        {
            InitializeComponent();

        }




        private void DropList_DragEnter(object sender, DragEventArgs e)
        {

        }

        static bool ValidatePassword(string password)
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
        private static byte[] GenerateSalt()
        {
            var randomBytes = Encoding.ASCII.GetBytes("original");
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(randomBytes);
            }
            return randomBytes;
        }

        public static String betweenStrings(String text, String start, String end)
        {
            int p1 = text.IndexOf(start) + start.Length;
            int p2 = text.IndexOf(end, p1);

            if (end == "") return (text.Substring(p1));
            else return text.Substring(p1, p2 - p1) + "";
        }
        private void DropList_Drop(object sender, DragEventArgs e)
        {
            string DownloadPath = "";
            try
            {
                if (RDefault.IsChecked == true)
                {
                    if (textpassword.Password.ToString() == "")
                    {
                        MyMessageBox.ShowDialog("Please Enter the Password used during file Encrption.");
                        return;
                    }


              }

                if (RDefault.IsChecked == true)
                {
                    if (ValidatePassword(textpassword.Password.ToString()))
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

                    }
                    else
                    {
                        textpassword.Clear();
                        MyMessageBox.ShowDialog("Password Length should be between 4 to 16 Characters.");
                    }
                }

                if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                {
                    droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                    try
                    {
                        int processedFiles = 0;
                        int totalFiles = droppedFilePaths.Count();
                        foreach (var path in droppedFilePaths)
                        {
                            ConfigurationManager.AppSettings["LastSelectedLocation"] = System.IO.Path.GetDirectoryName(path);
                            DownloadPath = System.IO.Path.GetDirectoryName(path);

                            FileInfo fi = new FileInfo(path);
                            if (fi.Length <= 524288000)
                            {


                                if (fi.Extension != ".mil")

                                {
                                    MyMessageBox.ShowDialog("File Extesion Not Support.");
                                    break;
                                }
                                byte[] fullBytes = null;
                                int headerLength = 0;


                                if (fi.Extension == ".mil")
                                {


                                    FileStream stream1 = File.OpenRead(path);
                                    byte[] bytes1 = new byte[stream1.Length];
                                    stream1.Read(bytes1, 0, bytes1.Length);

                                    stream1.Close();

                                    char dd = '_';
                                    int levelOfEncryption = fi.FullName.Count(s => s == dd);
                                    if (RDefault.IsChecked == true)
                                    {

                                        new Thread(() =>
                                        {

                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));
                                           // string mac;
                                           // byte[] roundtrip = AesGcm256.SimpleDecryptWithPassword(bytes1, textpassword.Password.ToString(), out mac);
                                            byte[] roundtrip = AesGcm256.SimpleDecryptWithPassword(bytes1, textpassword.Password.ToString());
                                            if (roundtrip == null)
                                            {
                                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Password incorrect or File is Tempered !")));
                                                return;
                                            }
                                            else
                                            {
                                                string filePath = DownloadPath + "\\" + fi.Name.Split('.')[0] + "_DEC_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + "." + betweenStrings(fi.Name, ".", "_");


                                                using (Stream file = File.OpenWrite(filePath))
                                                {

                                                    file.Write(roundtrip, 0, roundtrip.Length);
                                                }

                                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));

                                            }

                                            processedFiles++;

                                            if (processedFiles == totalFiles)
                                            {
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
                                        }).Start();
                                    }
                                    else
                                    {
                                        new Thread(async () =>
                                        {

                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));
                                            string filePath = DownloadPath + "\\" + fi.Name.Split('.')[0] + "_DEC_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + "." + betweenStrings(fi.Name, ".", "_");

                                            X509Certificate2Collection fcollection = await helper.GetCertificates();

                                            if (fcollection.Count == 0)
                                            {

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
                                                    //ret1 = Service1.DecryptFile(path, filePath, cert1);

                                                }
                                            }
                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));


                                            processedFiles++;
                                            if (ret1 ==0)
                                            {
                                                var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                                {
                                                    return MyMessageBox.Show("Wrong Token Inserted Does Not Match Private Key");
                                                }));

                                                        }
                                            else if (processedFiles == totalFiles)
                                            {

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
                                        }).Start();
                                    }

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
                    }
                    catch (Exception)
                    {
                        MyMessageBox.ShowDialog("Invaild File....");
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


        private void Default_Click(object sender, RoutedEventArgs e)
        {
            lblFileDecryption.Content = "File Decryption (Single or Multiple Files)";
            lblStep1.Content = "Step 1: Enter Original Password Used for Encrypting file(s).";
            lblStep2.Content = "Step 2: Select single or Multiple files for Decryption (should have *.mil extn).";
            lblStep3.Content = "Step 3: Wait for few seconds for Decrypted file(s) (new file name :- originalfilena_me_DEC_date_milisecond).";
            lblStep4.Content = "Step 4: Click OK to acknowledge or Open Path to open folder containing Decrypted file(s).";
            lblNote.Content = "Note: Original encrypted file is not changed.";

            Encrypt.IsChecked = false;
            textpassword.IsEnabled = true;
            lblpassword.Text = "Please Enter Decryption Password :";
        }

        private void Encrypt_Click(object sender, RoutedEventArgs e)
        {
            lblFileDecryption.Content = "Asymmetric (Private-Key) Decryption";
            lblStep1.Content = "Step 1: Insert Recipient’s IACA token and select file with .mil extn. (Originalfilena_me_RSA_date_milliseconds.mil).";
            lblStep2.Content = "Step 2: Enter IACA token PIN.";
            lblStep3.Content = "Step 3: Decrypted file will be created at original file loc.";
            lblStep4.Content = "Step 4: Click OK to open file(s).";
            lblNote.Content = "Note: Use Asymmetric encryption only for one-to-one file sharing as matching public-private Key pair\r\n          (IACA token) can only encrypt/decrypt the file.";

            RDefault.IsChecked = false;
            textpassword.IsEnabled = false;
            lblpassword.Text = "Please Insert Token:";

        }
        private async void btnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            string DownloadPath = "";
            try
            {
                if (RDefault.IsChecked == true)
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
                }
                if (RDefault.IsChecked == true)
                {
                    if (textpassword.Password.ToString() == "")
                    {
                        MyMessageBox.ShowDialog("Please Enter Password.");
                        return;
                    }
                    if (ValidatePassword(textpassword.Password.ToString()))
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Multiselect = true;
                        openFileDialog.Title = "Select File To Decryption";
                        openFileDialog.Filter = "mil files (*.mil)|*.mil";
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
                            try
                            {
                                int processedFiles = 0;
                                int totalFiles = openFileDialog.FileNames.Count();

                                foreach (var path in openFileDialog.FileNames)
                                {
                                    ConfigurationManager.AppSettings["LastSelectedLocation"] = System.IO.Path.GetDirectoryName(path);
                                    DownloadPath = System.IO.Path.GetDirectoryName(path);

                                    FileInfo fi = new FileInfo(path);

                                    if (fi.Length <= 524288000)
                                    {
                                        FileStream stream1 = File.OpenRead(path);
                                        byte[] bytes1 = new byte[stream1.Length];
                                        stream1.Read(bytes1, 0, bytes1.Length);

                                        stream1.Close();


                                        new Thread(() =>
                                        {
                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));

                                            //string mac;
                                            //byte[] roundtrip = AesGcm256.SimpleDecryptWithPassword(bytes1, textpassword.Password.ToString(), out mac);

                                            byte[] roundtrip = AesGcm256.SimpleDecryptWithPassword(bytes1, textpassword.Password.ToString());

                                            if (roundtrip == null)
                                            {
                                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Password incorrect..")));
                                            }

                                            else
                                            {
                                                string filePath = DownloadPath + "\\" + fi.Name.Split('.')[0] + "_DEC_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + "." + betweenStrings(fi.Name, ".", "_");

                                                using (Stream file = File.OpenWrite(filePath))
                                                {
                                                    file.Write(roundtrip, 0, roundtrip.Length);
                                                }

                                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));

                                            }

                                            processedFiles++;

                                            if (processedFiles == totalFiles)
                                            {

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

                                        }).Start();
                                    }
                                    else
                                    {
                                        MyMessageBox.ShowDialog("File size is too large! Max size is 500 MB");
                                    }
                                }


                            }
                            catch (Exception)
                            {
                                MyMessageBox.ShowDialog("Invaild Details....");
                            }


                        }
                    }
                }
                else if (RDefault.IsChecked == false)
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog();
                    openFileDialog.Multiselect = true;
                    openFileDialog.Title = "Select File To Decryption";
                    openFileDialog.Filter = "mil files (*.mil)|*.mil";

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
                        int processedFiles = 0;
                        int totalFiles = openFileDialog.FileNames.Count();
                        X509Certificate2Collection fcollection = await helper.GetCertificates();
                        foreach (var path in openFileDialog.FileNames)
                        {
                            ConfigurationManager.AppSettings["LastSelectedLocation"] = System.IO.Path.GetDirectoryName(path);
                            DownloadPath = System.IO.Path.GetDirectoryName(path);

                            FileInfo fi = new FileInfo(path);
                            if (fi.Length <= 524288000)
                            {
                                FileStream stream1 = File.OpenRead(path);
                                byte[] bytes1 = new byte[stream1.Length];
                                stream1.Read(bytes1, 0, bytes1.Length);

                                stream1.Close();

                                new Thread(async () =>
                                {

                                    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));
                                    this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));
                                    string filePath = DownloadPath + "\\" + fi.Name.Split('.')[0] + "_DEC_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + "" + "" + "." + betweenStrings(fi.Name, ".", "_");



                                    if (fcollection.Count == 0)
                                    {

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
                                        if (DateTime.Now <= cert1.NotAfter)
                                        {
                                            string macDetails;   // declare variable first

                                            ret1 = Service1.DecryptFile(path, filePath, cert1, out macDetails);
                                            // ret1 = Service1.DecryptFile(path, filePath, cert1);
                                        }
                                        else
                                        {

                                            var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                            {
                                                return MyMessageBox.Show("Token is expired. Pl contact issuer!");
                                            }));

                                        }


                                    }
                                    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                    this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));


                                    processedFiles++;
                                    if (ret1==0)
                                    {
                                        var result = this.Dispatcher.Invoke(new Func<string>(() =>
                                        {
                                            return MyMessageBox.Show("Wrong Token Inserted Does Not Match Private Key Or Token is expired. Pl contact issuer!");
                                        }));

                                    }
                                    else if (processedFiles == totalFiles)
                                    {

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
                                }).Start();
                            }
                            else
                            {
                                MyMessageBox.ShowDialog("File size is too large! Max size is 500 MB");
                            }
                        }


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
    }
}
