using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Signatures;
using Microsoft.Office.Interop.Word;
using Microsoft.Win32;
using MyApp;
using Newtonsoft.Json;
using SignService;
using SignService.Helpers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel.Web;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using WinniesMessageBox;
using static iText.Signatures.PdfSigner;
using static ValidateCertificate.ValidateCert;
using Brushes = System.Windows.Media.Brushes;
using Console = System.Console;

namespace DGISApp
{

    public partial class DigitalSign : UserControl
    {
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int Description, int ReservedValue);
        string[] droppedFilePaths = null;
        string message = null;
        string fileName = null;
        string DownloadPath = "";
        public string download = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads";
        float pixelWidth = 0;
        float pixelHeight = 0;
        int PageWidth = 0;
        int PageHeight = 0;
        bool crloscp = false;
        string crlocspmsg = "";
        string CertThumbPrint = "";
        string UrlApi = ConfigurationManager.AppSettings["UrlApi"].ToString();
        bool IsLocalToken= bool.Parse(ConfigurationManager.AppSettings["IsLocalToken"]);
        public DigitalSign()
        {
            InitializeComponent();
            LoadDataAsync();
        }

        
        private async void LoadDataAsync()
        {
            HelperCert helperCert = new HelperCert();
            var result = await helperCert.CheckSomethingAsync();
            if (result.Status == "0")
                MyMessageBox.ShowDialog(result.Remark);
            else if (result.Status == "-1")
                MyMessageBox.ShowDialog(result.Remark);
            else if (result.Status == "1")
                CertThumbPrint = result.Remark;
            else if (result.Status == "2")
                CertThumbPrint = result.Remark;

        }
         

        public async Task<bool> IsConnectedToInternet()
        {

            if (ChkCrl.IsChecked == true)
            {

                var hasInternetTask = await helper.HasInternetConnectionAsyncTest();
                if (hasInternetTask == true)
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

        private void DropList_DragEnter(object sender, DragEventArgs e)
        {


        }

        private async void DropList_Drop(object sender, DragEventArgs e)
        {
            try
            {
                string email = textRemark.Text;
                string pattern = @"^[a-zA-Z0-9@, ._\-]+$";
                if (!Regex.IsMatch(email, pattern) && textRemark.Text != "")
                {
                    MyMessageBox.ShowDialog("Special Characters Not Allow ");
                    this.DropList.IsEnabled = true;
                    this.BusyBar.IsBusy = false;
                    return;
                }



                HelperCert helperCert = new HelperCert();
                var result = await helperCert.CheckSomethingAsync();
                if (result.Status == "0")
                    MyMessageBox.ShowDialog(result.Remark);
                else if (result.Status == "-1")
                    MyMessageBox.ShowDialog(result.Remark);
                else if (result.Status == "1")
                    CertThumbPrint = result.Remark;
                else if (result.Status == "2")
                    CertThumbPrint = result.Remark;

                if (result.Status == "1")
                {
                    if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                    {
                        droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                        this.DropList.IsEnabled = false;
                        this.BusyBar.IsBusy = true;


                        if (RDefault.IsChecked == true)
                        {
                            if (ChkBulkSign.IsChecked == true)
                            {
                                if (System.IO.File.Exists(droppedFilePaths[0]))
                                {
                                    MyMessageBox.ShowDialog("Please select folder!");
                                    this.DropList.IsEnabled = true;
                                    this.BusyBar.IsBusy = false;
                                    return;
                                }
                                BulkDigitalSig(droppedFilePaths[0]);
                            }
                            else
                            {
                                if (droppedFilePaths.Length > 1)
                                {
                                    MyMessageBox.ShowDialog("You can select only one file for sign !");
                                    this.DropList.IsEnabled = true;
                                    this.BusyBar.IsBusy = false;
                                    return;
                                }
                                onlineDigitalSig(droppedFilePaths);
                            }
                        }
                        else
                        { 
                            OpenCustomCordinateSelecter(droppedFilePaths);
                             
                        }



                    }
                }
            }
            catch (Exception ex)
            {
                MyMessageBox.ShowDialog(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }
        }

        private CancellationTokenSource cancellationTokenSource;

        private async void BulkDigitalSig(string Directory, int x = 0, int y = 0)
        {
            try
            {
                int Pagenumber = 1;

                if (this.CPage.IsChecked == true)
                {
                    if (this.TxtCPage.Text == "")
                    {
                        MyMessageBox.ShowDialog("Please Enter Page Number");
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                        return;
                    }
                    else
                    {
                        Pagenumber = Convert.ToInt32(TxtCPage.Text);
                        if (Pagenumber <= 0)
                        {
                            MyMessageBox.ShowDialog("Please Enter Valid Page Number");
                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                            return;
                        }
                    }
                }

                string apiUrl = UrlApi + "/DigitalSignBulkAsync";

                List<DigitalSignData> senddataList = new List<DigitalSignData>();


                DigitalSignData senddata = new DigitalSignData();
                senddata.Thumbprint = CertThumbPrint;
                senddata.FolderLoc = Directory;
                senddata.OutputFolderLoc = Directory;
                senddata.XCoordinate = x;
                senddata.YCoordinate = y;
                senddata.Page = Pagenumber;
                senddataList.Add(senddata);


                string SendJaon = Newtonsoft.Json.JsonConvert.SerializeObject(senddataList.ToArray());
                var content = new StringContent(SendJaon, Encoding.UTF8, "application/json");
                var client = new HttpClient();
                 
                IService1 service1 = new Service1();
                var apiResponse = await service1.DigitalSignBulkAsync(senddataList);

                if (apiResponse != null)
                {

                    string resultstring = "";
                    int count = 0;
                    int Signed = 0;
                    if (apiResponse.ResponseMessage != null)
                    {
                        if (apiResponse.ResponseMessage.Valid == true)
                        {
                            resultstring = "Congratulations!\n\nDocument is successfully Signed.\n";
                        }
                        else
                        {
                            resultstring = "Opps!\n\nDocument is Not Signed.\n";
                        }
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
                        this.DropList.IsEnabled = true;
                        this.BusyBar.IsBusy = false;
                        var result = this.Dispatcher.Invoke(new Func<string>(() =>
                        {
                            if (Signed > 0)
                            {
                                return MyMessageBox.ShowDialog(resultstring + "\n" + Directory, MyMessageBox.Buttons.OK_PathOpen);
                            }
                            else
                            {
                                return MyMessageBox.ShowDialog(resultstring, MyMessageBox.Buttons.OK_PathOpen);
                            }
                        }));

                        if (result == "2")
                        {
                            try
                            {
                                Process.Start(Directory);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("An error occurred: " + ex.Message);
                            }
                        }
                    }
                    else
                    {
                        if (apiResponse.ResponseMessage != null)
                        {
                            MyMessageBox.Show($"Error: {apiResponse.ResponseMessage.Message}");
                            BusyBar.IsBusy = false;
                            DropList.IsEnabled = true;
                        }
                    }
                }

                else
                {
                    MyMessageBox.Show($"Error: {apiResponse.ResponseMessage.Message}");
                    BusyBar.IsBusy = false;
                    DropList.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                BusyBar.IsBusy = false;
                DropList.IsEnabled = true;
                ErrorLog.LogErrorToFile(ex);
            }
        }


        private void onlineDigitalSig(string[] files, int x = 0, int y = 0, int pageNumber = 0)
        {
            bool CheckCrl = false;
            String NewFileName = "";
           
            int pagecount = 0;
            int IntPrintPageNo = 1;
            Boolean custom = false;
            try
            {
                foreach (string filename in files)
                {
                nextfile:
                    string fileforloop = filename;
                    FileInfo fi = new FileInfo(fileforloop);
                    if (fi.Length <= 524288000)
                    {
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




                            Spire.Pdf.PdfDocument document = new Spire.Pdf.PdfDocument();
                            document.LoadFromFile(fileforloop);
                            pagecount = document.Pages.Count;
                            document.Close();



                            if (LPage.IsChecked == true)
                            {
                                IntPrintPageNo = pagecount;
                            }

                            if (RCustom.IsChecked == true)
                            {
                                if (pageNumber == 0)
                                {
                                    MyMessageBox.ShowDialog("Page No Zero Not Allow!");
                                    break;
                                }
                                custom = true;
                                IntPrintPageNo = pageNumber;
                                DownloadPath = Path.GetDirectoryName(filename);
                            }
                            else
                            {
                                DownloadPath = Path.GetDirectoryName(filename);
                                ConfigurationManager.AppSettings["LastSelectedLocation"] = Path.GetDirectoryName(filename);
                            }

                            if (ChkCrl.IsChecked == true)
                            {
                                CheckCrl = true;
                            }

                            fileName = Path.GetFileNameWithoutExtension(fileforloop);

                            BusyBar.IsBusy = true;

                            cancellationTokenSource = new CancellationTokenSource();
                            new Thread(() => SignDocument(DownloadPath, fileforloop, IntPrintPageNo, x, y, custom, CheckCrl, cancellationTokenSource.Token)).Start();
                           


                        }

                        else if (Path.GetExtension(filename) == ".docx" || Path.GetExtension(filename) == ".doc" || Path.GetExtension(filename) == ".DOCX")
                        {

                            DropList.IsEnabled = false;
                            BusyBar.IsBusy = true;
                            String DocfileName = Path.GetFileNameWithoutExtension(filename);
                            NewFileName = System.IO.Path.GetTempPath() + "\\" + DocfileName + ".pdf";
                            if (NewFileName.Length > 255)
                            {
                                MyMessageBox.ShowDialog("FileName too long!");
                                goto nextfile;

                            }
                            else
                            {
                                helper.ConvertPDF(filename, NewFileName, WdSaveFormat.wdFormatPDF);
                            }


                            goto nextfile;
                        }
                        else
                        {

                            MyMessageBox.ShowDialog("Support only .pdf/.doc/.docx");
                            NewFileName = "";
                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                        }
                    }
                    else
                    {
                        MyMessageBox.ShowDialog("File size is too large! Max size is 500 MB");
                        NewFileName = "";
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                    }
                }
                 
            }
            catch (Exception ex)
            {

                MyMessageBox.ShowDialog("No Docu signed! Reason2:-  " + ex.Message);
                NewFileName = "";
                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                ErrorLog.LogErrorToFile(ex);
            }
        }


        public static byte[] Sign(byte[] data, X509Certificate2 certificate)
        {
            using (var key = certificate.GetRSAPrivateKey())
            {
                return key.SignData(data,
                  HashAlgorithmName.SHA256,
                  RSASignaturePadding.Pkcs1);

            }
        }


        private async void btnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            listitem.Items.Clear();

            string email = textRemark.Text;
            string pattern = @"^[a-zA-Z0-9@, ._\-]+$";
            if (!Regex.IsMatch(email, pattern) && textRemark.Text != "")
            {
                MyMessageBox.ShowDialog("Special Characters Not Allow ");
                this.DropList.IsEnabled = true;
                this.BusyBar.IsBusy = false;
                return;
            }
            
            try
            {
                HelperCert helperCert = new HelperCert();
                var result = await helperCert.CheckSomethingAsync();
                if (result.Status == "0")
                    MyMessageBox.ShowDialog(result.Remark);
                else if (result.Status == "-1")
                    MyMessageBox.ShowDialog(result.Remark);
                else if (result.Status == "1")
                    CertThumbPrint = result.Remark;
                else if (result.Status == "2")
                    CertThumbPrint = result.Remark;

                if (result.Status == "1")
                {
                    if (ChkBulkSign.IsChecked == true)
                    {
                        
                        System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
                        folderBrowserDialog.Description = "Select a folder containing PDF or DOC files";

                        if (ConfigurationManager.AppSettings["LastSelectedLocation"] == "")
                        {
                            folderBrowserDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                        }
                        else
                        {
                            folderBrowserDialog.SelectedPath = ConfigurationManager.AppSettings["LastSelectedLocation"];
                        }

                        if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            string selectedDirectory = folderBrowserDialog.SelectedPath;

                            this.DropList.IsEnabled = false;
                            this.BusyBar.IsBusy = true;

                            string[] fileNames = Directory.GetFiles(selectedDirectory, "*.*", SearchOption.TopDirectoryOnly);

                            if (RDefault.IsChecked == true)
                            {
                                BulkDigitalSig(selectedDirectory);
                            }
                            else
                            {
                                OpenCustomCordinateSelecter(droppedFilePaths); 
                            }
                        }
                    }
                    else
                    {
                        OpenFileDialog openFileDialog = new OpenFileDialog();
                        openFileDialog.Filter = "files (*.pdf;*.PDF;*.docx;*.DOCX;*.doc;*.DOC)|*.pdf;*.PDF;*.docx;*.DOCX,*.doc; *.DOC";

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
                            string selectedFile = openFileDialog.FileName;

                            this.DropList.IsEnabled = false;
                            this.BusyBar.IsBusy = true;

                            if (RDefault.IsChecked == true)
                            {
                                onlineDigitalSig(new[] { selectedFile });
                            }
                            else
                            {

                                OpenCustomCordinateSelecter(new[] { selectedFile });
                                 
                            }
                        }
                    }
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {
                ErrorLog.LogErrorToFile(ex);
            }
            catch (Exception ex)
            {
                MyMessageBox.ShowDialog(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }


        }
        public async Task<int> ExecuteTaskAsync()
        {
            CustomSignCordinate.UpdatedOn = DateTime.Now;
            while (true)
            {
                DateTime currentTime = DateTime.Now;
                if (CustomSignCordinate.X > 0 && CustomSignCordinate.Y > 0)
                {

                    return 1; 

                }

                else if ((currentTime - CustomSignCordinate.UpdatedOn).TotalSeconds > 3 && CustomSignCordinate.UpdatedOn.Year > 2024)
                {
                    return -1; 
                }



                await System.Threading.Tasks.Task.Delay(3000);  
            }

        }
        private bool IsBrowserInstalled(string browserExe)
        {
            string registryPath = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{browserExe}";

            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(registryPath))
            {
                return key != null;
            }
        }
        public async void OpenBrowserForCordinate(string[] file, string filename)
        {
            CustomSignCordinate.X = 0;
            CustomSignCordinate.Y = 0;
            CustomSignCordinate.PageNo = 1;
            CustomSignCordinate.PdfFile = filename;
            FileInfo fi = new FileInfo(filename);
            if (fi.Length > 209715200)
            {
                MyMessageBox.ShowDialog("File Size Too Large Please Select less then 200Mb!");
                this.BusyBar.IsBusy = false;
                this.DropList.IsEnabled = true;

            }
            else
            { 
                string appDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                 
                string htmlFilePath = Path.Combine(appDirectory, "PDFViewerWithCordinates", "index.html");
                 
                string url = $"file:///{htmlFilePath.Replace("\\", "/").Replace(" ", "%20")}";
                 
                int width = 1200;   
                int height = 700; 


                 
                int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

                 
                int posX = (screenWidth - width) / 2;
                int posY = (screenHeight - height) / 2;
                 
                var keychrome = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe");
                var keyfirefox = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\firefox.exe");
                var keymsedge = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe");

                if (keychrome != null)
                {
                    Process.Start("chrome.exe", $"--new-window \"{url}\" --window-size={width},{height} --window-position={posX},{posY}");
                }
                else if (keyfirefox != null)
                {

                    Process.Start("firefox.exe", $"--new-window \"{url}\" --window-size={width},{height} --window-position={posX},{posY}");

                }
                else
                {

                    Process.Start("msedge.exe", $"--new-window \"{url}\" --window-size={width},{height} --window-position={posX},{posY}");

                }

                int ret = await ExecuteTaskAsync();

                if (ret == 1)
                {

                    int x = CustomSignCordinate.X;
                    this.BusyBar.IsBusy = false;
                    this.DropList.IsEnabled = true;

                    onlineDigitalSig(file, CustomSignCordinate.X, CustomSignCordinate.Y, CustomSignCordinate.PageNo);


                }
                else if (ret == -1)
                {
                    MyMessageBox.ShowDialog("Oops! You closed the browser !");
                    this.BusyBar.IsBusy = false;
                    this.DropList.IsEnabled = true;
                }
                else if (ret == -2)
                {
                    MyMessageBox.ShowDialog("Invalid Cordinate Select !");
                    this.BusyBar.IsBusy = false;
                    this.DropList.IsEnabled = true;
                }
                else
                {
                    this.BusyBar.IsBusy = false;
                    this.DropList.IsEnabled = true;
                }
            }
        }
        public async void OpenCustomCordinateSelecter(string[] file)
        {

            foreach (string filename in file)
            {
                DownloadPath = Path.GetDirectoryName(filename);
                ConfigurationManager.AppSettings["LastSelectedLocation"] = Path.GetDirectoryName(filename);

                if (Path.GetExtension(filename) == ".pdf" || Path.GetExtension(filename) == ".PDF")
                {

                    OpenBrowserForCordinate(file, filename);
                     
                }
                else if (Path.GetExtension(filename) == ".docx" || Path.GetExtension(filename) == ".doc" || Path.GetExtension(filename) == ".DOCX")
                {
                    String NewFileName = "";
                    String DocfileName = Path.GetFileNameWithoutExtension(filename);
                    NewFileName = System.IO.Path.GetTempPath() + "\\" + DocfileName + ".pdf";

                    FileInfo f1 = new FileInfo(filename);


                    if (f1.Length > 0)
                    {
                        if (NewFileName.Length > 255)
                        {
                            MyMessageBox.ShowDialog("FileName too long!");
                            pdfviewer.Visibility = Visibility.Hidden;
                            this.BusyBar.IsBusy = false;
                            this.DropList.IsEnabled = true;
                            return;

                        }
                        else
                        {
                            helper.ConvertPDF(filename, NewFileName, WdSaveFormat.wdFormatPDF);
                        }


                        OpenBrowserForCordinate(file, NewFileName);
 
                    }
                    else
                    {


                        Card1.Width = 700;
                        pdfviewer.Visibility = Visibility.Hidden;
                        this.BusyBar.IsBusy = false;
                        this.DropList.IsEnabled = true;

                    }
                }
                else
                {
                    MyMessageBox.Show("Support only PDF !");
                    Card1.Width = 700;
                    pdfviewer.Visibility = Visibility.Hidden;
                    this.BusyBar.IsBusy = false;
                    this.DropList.IsEnabled = true;
                    return;
                }

            }



        }

        public class CertificateData
        {
            public string API { get; set; }
            public bool CRL_OCSPCheck { get; set; }
            public String CRL_OCSPMsg { get; set; }
            public string Remarks { get; set; }
            public string Status { get; set; }
            public string Thumbprint { get; set; }
            public string ValidFrom { get; set; }
            public string ValidTo { get; set; }
            public string issuer { get; set; }
            public string subject { get; set; }
            public Boolean TokenValid { get; set; }
        }

        /// <summary>
        /// Pdf signature default and custom
        /// </summary>

        public async void SignDocument(string downloadfilePath, string filename, int PageNum, int X = 0, int Y = 0, Boolean custom = false, Boolean BlnCheckCrl = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool ValidToken = false;
            string TokenRemarks = "";
            String FileFullName = "";
            Boolean ErrorEncountered = false;
            PdfSigner signer1 = null;
            FileStream fileStream = null;
            CertificateData certificateData = null;
            X509Certificate2 cert1 = null;
            bool isAnyFileSigned = false;
            DTOSaveDigitalSignInfo saveDigitalSignInfo = new DTOSaveDigitalSignInfo();

            var headers = WebOperationContext.Current?.IncomingRequest?.Headers;

            string origin = headers?["Origin"];    
            string referer = headers?["Referer"];  
            try
            {

                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = false));
                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = true));

                bool CheckCrlTick = this.Dispatcher.Invoke(new Func<bool>(() => this.ChkCrl.IsChecked == true));


                if (CertThumbPrint == null || CertThumbPrint == "")
                {
                    string response = await GetTokenDetail(false, CertThumbPrint);
                    List<CertificateData> certificates = JsonConvert.DeserializeObject<List<CertificateData>>(response);
                    certificateData = certificates[0];
                    ValidToken = certificates[0].TokenValid;
                    TokenRemarks = certificates[0].Remarks;


                    if (ValidToken == false)
                    {
                        this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(TokenRemarks)));
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                        return;
                    }
                    else
                    {
                        bool crloscp1 = certificateData.CRL_OCSPCheck;
                        string crlocspmsg1 = certificateData.CRL_OCSPMsg;

                        if (CheckCrlTick == true)
                        {
                            if (crloscp1 == true && crlocspmsg1 == "Digital Cert of token cannot be verified with CA due to Network issues")
                            {
                                bool CloseThread1 = false;
                                this.Dispatcher.Invoke(() =>
                                {
                                    if (MyMessageBox.ShowDialog("Digital Cert of token cannot be verified with CA due to Network issues. Do you want to continue ?", MyMessageBox.Buttons.Yes_No) != "1")
                                    {
                                        CloseThread1 = true;
                                    }
                                });

                                if (CloseThread1 == true)
                                {
                                    this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Signed !")));
                                    this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                    this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                    return;
                                }
                            }
                            else if (crloscp1 == false)
                            {
                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("CRL Check Failed !")));
                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                return;
                            }
                        }

                        X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                        store.Open(OpenFlags.ReadOnly);
                        X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, certificateData.Thumbprint, false);
                        store.Close();
                        cert1 = certCollection[0];
                    }
                }
                else
                {
                    X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                    store.Open(OpenFlags.ReadOnly);
                    X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, CertThumbPrint, false);
                    store.Close();

                    cert1 = certCollection[0];

                    if (DateTime.Now > cert1.NotAfter && !IsLocalToken)
                    {
                        this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("Token is expired. Pl contact issuer !")));
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                        return;
                    }

                    saveDigitalSignInfo.SignedDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss");
                    IService1 service1 = new Service1();
                    var PublicKey = await service1.GetPublicKey();
                    byte[] textBytes = Encoding.UTF8.GetBytes(PublicKey.Public_Key);
                    saveDigitalSignInfo.PublicKey = Convert.ToBase64String(textBytes);
                    saveDigitalSignInfo.ValidToken = PublicKey.TokenValid;
                    saveDigitalSignInfo.ValidFrom = PublicKey.ValidFrom;
                    saveDigitalSignInfo.ValidTo = PublicKey.ValidTo;
                    saveDigitalSignInfo.OriginForSign = origin;
                    saveDigitalSignInfo.RefererForSign = referer;

                    if (CheckCrlTick == true)
                    {
                        if (crloscp == true && crlocspmsg == "Digital Cert of token cannot be verified with CA due to Network issues")
                        {
                            bool CloseThread = false;
                            this.Dispatcher.Invoke(() =>
                            {
                                if (MyMessageBox.ShowDialog("Digital Cert of token cannot be verified with CA due to Network issues. Do you want to continue ?", MyMessageBox.Buttons.Yes_No) != "1")
                                {
                                    CloseThread = true;
                                }
                            });

                            if (CloseThread == true)
                            {
                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Signed !")));
                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                return;
                            }
                        }
                        else if (crloscp == false)
                        {
                            if (crlocspmsg == "Digital Cert of token cannot be verified with CA due to Network issues")
                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(crlocspmsg)));
                            else
                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("CRL Check Failed !")));
                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                            return;
                        }
                    }
                }


                String StrRemark = this.Dispatcher.Invoke(new Func<string>(() => this.textRemark.Text.ToString()));

                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                PdfReader reader = new PdfReader(filename);
                reader.SetUnethicalReading(true);
                Thread t = new Thread((ThreadStart)(async () =>
                {
                    try
                    {
                        IExternalSignature es = new X509Certificate2Signature(cert1, "SHA-1", ref message);
                        if (message != null)
                        {
                            this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(message)));
                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                            return;
                        }
                        else
                        {
                            if (filename != "")
                            {
                                try
                                {  
                                    download = downloadfilePath + @"\";

                                    if (es.GetEncryptionAlgorithm() != null)
                                    {
                                        Org.BouncyCastle.X509.X509CertificateParser cp1 = new Org.BouncyCastle.X509.X509CertificateParser();
                                        Org.BouncyCastle.X509.X509Certificate[] chain3 = new[] { cp1.ReadCertificate(cert1.RawData) };
                                        try
                                        {
                                            StampingProperties stampProp = new StampingProperties();
                                            stampProp.PreserveEncryption();
                                            ImageData imageData = null;

                                            if (StrRemark != "")
                                            {
                                                using (StreamReader sr = new StreamReader(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + @"\DigitalSign.png"))
                                                {
                                                    imageData = ImageDataFactory.Create(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DigitalSign.png");
                                                }
                                            }
                                            else
                                            {
                                                using (StreamReader sr = new StreamReader(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + @"\DigitalSignWT.png"))
                                                {
                                                    imageData = ImageDataFactory.Create(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DigitalSignWT.png");
                                                }
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
                                            iText.Kernel.Pdf.PdfDocument pdfDocument = new iText.Kernel.Pdf.PdfDocument(new PdfReader(filename));
                                            SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
                                            IList<string> sigNames = signatureUtil.GetSignatureNames();
                                            pdfDocument.Close();

                                            FileFullName = downloadfilePath + "\\" + fileName + "_DS_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + ".pdf";

                                            saveDigitalSignInfo.DocumentName = Path.GetFileName(FileFullName);

                                            iText.Kernel.Font.PdfFont font = PdfFontFactory.CreateFont(FontProgramFactory.CreateFont(StandardFonts.TIMES_BOLD));

                                            String StrSignature = "";
                                            if (StrRemark != "")
                                            {
                                                StrSignature = StrRemark + "\n\n Digitally Signed by \n " + StrRank + " " + StrName + " \n Date : " + saveDigitalSignInfo.SignedDateTime + " \n © Hastakshar SEWA, DGIS";
                                            }
                                            else
                                            {
                                                StrSignature = "Digitally Signed by \n " + StrRank + " " + StrName + " \n Date : " + saveDigitalSignInfo.SignedDateTime + " \n © Hastakshar SEWA, DGIS";
                                            }

                                            if (custom == false)
                                            {
                                                if (sigNames.Count == 0)
                                                {

                                                    try
                                                    {
                                                        fileStream = new FileStream(FileFullName, FileMode.Create); 
                                                        signer1 = new PdfSigner(reader, fileStream, new StampingProperties());
                                                    }
                                                    catch (Exception)
                                                    { 
                                                    }

                                                    PdfSignatureAppearance appearance = signer1.GetSignatureAppearance()

                                                           .SetLayer2Text(StrSignature)
                                                           .SetImage(imageData).SetImageScale(-50)
                                                           .SetReuseAppearance(false);
                                                    iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(220, 15, 180, 80);

                                                    if (StrRemark == "")
                                                    {
                                                        rect = new iText.Kernel.Geom.Rectangle(220, 15, 180, 50);
                                                    }
                                                    else
                                                    {
                                                        rect = new iText.Kernel.Geom.Rectangle(220, 15, 180, 80);
                                                    }
                                                    appearance
                                                            .SetPageRect(rect)
                                                            .SetPageNumber(PageNum);
                                                    signer1.SetFieldName(signer1.GetNewSigFieldName());
                                                     
                                                    try
                                                    {
                                                        signer1.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                                    }
                                                    catch
                                                    {
                                                        ErrorEncountered = true;
                                                        
                                                        this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Sign !")));
                                                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));

                                                    }
                                                }
                                                else
                                                {
                                                    fileStream = new FileStream(FileFullName, FileMode.Create);
                                                    PdfSigner signer = new PdfSigner(reader, fileStream, stampProp.UseAppendMode());
                                                    PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                                                         .SetLayer2Text(StrSignature)
                                                         .SetImage(imageData).SetImageScale(-50)
                                                         .SetReuseAppearance(false);


                                                    HelperCert helperCert = new HelperCert();
                                                    var getXYaxis = helperCert.GetSignatureCordinate(downloadfilePath + "\\" + fileName + ".pdf");
                                                    int Xaxis = 0;
                                                    int Yaxis = 0;
                                                    if (getXYaxis != null)
                                                    {
                                                        if (sigNames.Count % 2 == 0)
                                                        {
                                                            Yaxis = getXYaxis[sigNames.Count - 1].YCoordinate;
                                                            Xaxis = getXYaxis[0].XCoordinate;
                                                        }
                                                        else
                                                        {
                                                            Yaxis = getXYaxis[sigNames.Count - 1].YCoordinate;
                                                            Xaxis = getXYaxis[sigNames.Count - 1].XCoordinate + 200;
                                                            if (Xaxis > 400)
                                                            {
                                                                Yaxis = getXYaxis[sigNames.Count - 1].YCoordinate + 50;
                                                                Xaxis = 15; 
                                                            }
                                                        }
                                                    }
                                                   
                                                    iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(Xaxis, Yaxis, 180, 50);


                                                    appearance
                                                            .SetPageRect(rect)
                                                            .SetPageNumber(PageNum);
                                                    signer.SetFieldName(signer.GetNewSigFieldName());

                                                    try
                                                    {
                                                        signer.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                                    }
                                                    catch (Exception ex)
                                                    {

                                                        ErrorEncountered = true;
                                                        this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Sign !")));
                                                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                        ErrorLog.LogErrorToFile(ex);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (sigNames.Count == 0)
                                                {
                                                    fileStream = new FileStream(FileFullName, FileMode.Create);
                                                    PdfSigner signer = new PdfSigner(reader, fileStream, new StampingProperties());

                                                    PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                                                         .SetLayer2Text(StrSignature)
                                                         .SetImage(imageData).SetImageScale(-50)
                                                         .SetReuseAppearance(false);
                                                    iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 80);

                                                    if (StrRemark == "")
                                                    {
                                                        rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 50);
                                                    }
                                                    else
                                                    {
                                                        rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 80);
                                                    }
                                                    appearance
                                                            .SetPageRect(rect)
                                                            .SetPageNumber(PageNum);
                                                    signer.SetFieldName(signer.GetNewSigFieldName());


                                                    try
                                                    {
                                                        signer.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        ErrorEncountered = true;
                                                        this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Sign !")));
                                                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                        ErrorLog.LogErrorToFile(ex);
                                                    }
                                                }
                                                else
                                                {
                                                    fileStream = new FileStream(FileFullName, FileMode.Create);
                                                    PdfSigner signer = new PdfSigner(reader, fileStream, stampProp.UseAppendMode());
                                                    PdfSignatureAppearance appearance = signer.GetSignatureAppearance()
                                                         .SetLayer2Text(StrSignature)
                                                         .SetImage(imageData).SetImageScale(-50)
                                                         .SetReuseAppearance(false);
                                                    iText.Kernel.Geom.Rectangle rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 80);

                                                    if (StrRemark == "")
                                                    {
                                                        rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 50);
                                                    }
                                                    else
                                                    {
                                                        rect = new iText.Kernel.Geom.Rectangle(X, Y, 180, 80);
                                                    }
                                                    appearance
                                                           .SetPageRect(rect)
                                                           .SetPageNumber(PageNum);
                                                    signer.SetFieldName(signer.GetNewSigFieldName());

                                                    try
                                                    {
                                                        signer.SignDetached(es, chain3, null, null, null, 0, CryptoStandard.CMS);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        ErrorEncountered = true;
                                                        this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Sign !")));
                                                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                                        ErrorLog.LogErrorToFile(ex);
                                                    }
                                                }
                                            }
                                            reader.Close();
                                            if (ErrorEncountered == false)
                                            {
                                                string Result = "0";
                                                this.Dispatcher.Invoke(() =>
                                                {
                                                    Result = MyMessageBox.ShowDialog("Congratulations ! \n\n Document is Digitally Signed. \n " + download, MyMessageBox.Buttons.OK_OpenFile);
                                                    isAnyFileSigned = true;
                                                });
                                                if (Result == "2")
                                                {
                                                    string FilePath = Path.GetDirectoryName(FileFullName);
                                                    Process.Start(FilePath);
                                                }
                                                else if (Result == "3")
                                                {
                                                    try
                                                    {
                                                        Process.Start(FileFullName);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Console.WriteLine("An error occurred: " + ex.Message);
                                                    }
                                                }
                                                if (isAnyFileSigned) await new Service1().SaveDigitalSignedDataToAnalytics(saveDigitalSignInfo);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            reader.Close();
                                            this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(ex.Message)));
                                            this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                            this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                                            ErrorLog.LogErrorToFile(ex);
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    reader.Close();
                                    this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(ex.Message)));
                                    ErrorLog.LogErrorToFile(ex);
                                }
                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                            }
                            else
                            {
                                reader.Close();
                                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog("No Docu Sign !")));
                                this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(ex.Message)));
                        this.Dispatcher.Invoke(new Action(() => DropList.IsEnabled = true));
                        this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                        ErrorLog.LogErrorToFile(ex);
                    }
                    finally
                    {
                        reader.Close();
                    }
                }));
                 
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
            }
            catch (ArgumentOutOfRangeException ex)
            {
                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                Console.ReadLine();
                ErrorLog.LogErrorToFile(ex);
            }
            catch (CryptographicException ex)
            {
                MyMessageBox.ShowDialog(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }
            catch (iText.Kernel.PdfException ex)
            {
                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(ex.Message)));
                ErrorLog.LogErrorToFile(ex);
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(new Action(() => BusyBar.IsBusy = false));
                this.Dispatcher.Invoke(new Action(() => MyMessageBox.ShowDialog(ex.Message)));
                ErrorLog.LogErrorToFile(ex);
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Close();
                } 
            }

            if (FileFullName != "")
            {
                FileInfo fi = new FileInfo(FileFullName);
                if (fi.Length == 0)
                {
                    File.Delete(FileFullName);

                    String DocfileName = Path.GetFileNameWithoutExtension(filename);
                    string originWordFile = System.IO.Path.GetTempPath() + "\\" + DocfileName + ".pdf";
                    FileInfo f2 = new FileInfo(originWordFile);
                    if (f2.Exists)
                    {
                        try
                        {
                            File.Delete(originWordFile);
                        }
                        catch
                        { }
                    }
                }
            }
        }

        public static ICollection<byte[]> ProcessCrl(System.Security.Cryptography.X509Certificates.X509Certificate cert, ICollection<ICrlClient> crlList)
        {
            if (crlList == null)
                return null;
            List<byte[]> crlBytes = new List<byte[]>();
            foreach (ICrlClient cc in crlList)
            {
                if (cc == null)
                    continue;
                ICollection<byte[]> b = cc.GetEncoded(cert, null);
                if (b == null)
                    continue;
                crlBytes.AddRange(b);
            }
            if (crlBytes.Count == 0)
                return null;
            else
                return crlBytes;
        }

        private void FPage_Click(object sender, RoutedEventArgs e)
        {
            TxtCPage.Visibility = Visibility.Hidden;
            CPage.Visibility = Visibility.Hidden;
            if (ChkBulkSign.IsChecked == true)
            {
                CPage.Visibility = Visibility.Visible;
                TxtCPage.Text = "";
                TxtCPage.Visibility = Visibility.Visible;
            }
        }


        private void Default_Click(object sender, RoutedEventArgs e)
        {
            LPage.IsEnabled = true;
            FPage.IsEnabled = true;
            FPage.IsChecked = true;
            LPage.Visibility = Visibility.Visible;
            ChkBulkSign.IsEnabled = true;
            btnOpenFile.Content = "Select Document";
        }

        private void Custom_Click(object sender, RoutedEventArgs e)
        {
            LPage.IsEnabled = false;
            FPage.IsEnabled = false;
            ChkBulkSign.IsChecked = false;
            ChkBulkSign.IsEnabled = false;
            TxtCPage.Text = "";
            TxtCPage.Visibility = Visibility.Hidden;
            CPage.Visibility = Visibility.Hidden;
            btnOpenFile.Content = "Select Document";

        }

        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            Card1.Width = 700;
            pdfviewer.Visibility = Visibility.Hidden;
            this.BusyBar.IsBusy = false;
            this.DropList.IsEnabled = true;
        }


        private async void ChkCrl_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ChkCrl.IsChecked == true)
                {
                    bool isConnected = await helper.HasInternetConnectionAsyncTest();
                    if (isConnected)
                    {            
                        ChkCrl.Background = Brushes.Green; 

                        string Certificate = await GetTokenDetail(true, CertThumbPrint);

                        if (Certificate != "")
                        {
                            List<CertificateData> certificates = JsonConvert.DeserializeObject<List<CertificateData>>(Certificate);
                            CertificateData certificateData = certificates[0];

                            bool ValidToken = certificates[0].TokenValid;
                            string TokenRemarks = certificates[0].Remarks;
                            if (ValidToken == false)
                            {
                                crloscp = certificateData.CRL_OCSPCheck;
                                crlocspmsg = certificateData.CRL_OCSPMsg;
                                ChkCrl.Background = Brushes.Red; 
                                MyMessageBox.ShowDialog(TokenRemarks);
                                return;
                            }
                            else
                            {

                                crloscp = certificateData.CRL_OCSPCheck;
                                crlocspmsg = certificateData.CRL_OCSPMsg;

                                X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                                store.Open(OpenFlags.ReadOnly);
                                X509Certificate2Collection certCollection = store.Certificates.Find(X509FindType.FindByThumbprint, certificateData.Thumbprint, false);
                                store.Close();

                                X509Certificate2 cert1 = certCollection[0];
                            }
                        } 

                    }
                    else

                    {
                        ChkCrl.Background = Brushes.Red; 
                        crloscp = true;
                        crlocspmsg = ""; 
                    }
                }
                else
                {
                    crloscp = true;
                    crlocspmsg = "";
                }
            }
            catch (Exception ex)
            { 
                ErrorLog.LogErrorToFile(ex);
            }
        }

        private async Task<string> GetTokenDetail(bool IsCheckCrl, string Thumb)
        {
            try
            {
                string response = await GetRequest(UrlApi + "/FetchTokenOCSPCrlDetails?IsCheckCrl=" + IsCheckCrl + "&ThumbPrint=" + Thumb + "");
                return response;
            }
            catch (HttpRequestException)
            {
                return "";
            }
        }



        private void ChkBulkSign_Click(object sender, RoutedEventArgs e)
        {
            if (ChkBulkSign.IsChecked == true)
            {
                btnOpenFile.Content = "Select Directory";
                LPage.Visibility = Visibility.Hidden;
                TxtCPage.IsEnabled = false;
                CPage.Visibility = Visibility.Visible;
                TxtCPage.Visibility = Visibility.Visible;
                FPage.IsChecked = true;
            }
            else
            {
                btnOpenFile.Content = "Select Document";
                LPage.Visibility = Visibility.Visible;
                CPage.Visibility = Visibility.Hidden;
                TxtCPage.Visibility = Visibility.Hidden;
                FPage.IsChecked = true;
                TxtCPage.Text = "";
            }
        }

        private void CPage_Click(object sender, RoutedEventArgs e)
        {
            TxtCPage.IsEnabled = true;
        }

        private void TxtCPage_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!int.TryParse(textBox.Text, out _))
                {
                    textBox.Text = textBox.Text.Length > 0 ? textBox.Text.Substring(0, textBox.Text.Length - 1) : "";
                }
            }
        }
         
        private void RBModePdfWord_Checked(object sender, RoutedEventArgs e)
        {
            lblDigitalSigningMode.Content = "Digital Signing (Single or Bulk PDF/Word Docus)";
            lblStep2.Content = "Step 2. Select loc on Docu for Digital Signature.";
            lblStep3.Content = "Step 3. Select docu (Only Word/PDF) or directory (Bulk Sign) to be digitally signed.";
            lblStep4.Content = "Step 4. Click OK to create digitally signed file(s) (new file name :- originalfilena_me_DS_date_milisecond.pdf).";

            if (PanelPdfWordMode == null || PanelAnyFileMode == null) return;
            PanelPdfWordMode.Visibility = Visibility.Visible;
            PanelAnyFileMode.Visibility = Visibility.Collapsed;
        }

        private void RBModeAnyFile_Checked(object sender, RoutedEventArgs e)
        {
            lblDigitalSigningMode.Content = "Digital Signing Any File Formates";
            lblStep2.Content = "Step 2. Select a File for Digital Signature.";
            lblStep3.Content = "Step 3. Click OK to Generate a new .sig.json File For Signer Metadata.";
            lblStep4.Content = "Step 4. New MetaData File name :- ReadMe_Originalfilename_DS_date_milisecond.sig.json.";
            if (PanelPdfWordMode == null || PanelAnyFileMode == null) return;
            PanelPdfWordMode.Visibility = Visibility.Collapsed;
            PanelAnyFileMode.Visibility = Visibility.Visible;
        }

        private async void btnSelectAnyFile_Click(object sender, RoutedEventArgs e)
        {
            CertificateData certificateData = null;
            X509Certificate2 cert1 = null;
            bool isAnyFileSigned = false;
            DTOSaveDigitalSignInfo saveDigitalSignInfo = new DTOSaveDigitalSignInfo();

            var headers = WebOperationContext.Current?.IncomingRequest?.Headers;

            string origin = headers?["Origin"];    
            string referer = headers?["Referer"];  
            try
            {

                var dlg = new OpenFileDialog
                {
                    Title = "Select any file",
                    Filter = "All Files (*.*)|*.*",
                    Multiselect = false
                };

                if (dlg.ShowDialog() == true)
                {
                    lblAnyFilePath.Content = dlg.FileName;
                     
                    await GenericSignFileAsync(dlg.FileName);
                }
            }
            catch (Exception ex)
            {
                MyMessageBox.ShowDialog(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }
        }


        private void DropListAny_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void DropListAny_Drop(object sender, DragEventArgs e)
        {
            CertificateData certificateData = null;
            X509Certificate2 cert1 = null;
            bool isAnyFileSigned = false;
            DTOSaveDigitalSignInfo saveDigitalSignInfo = new DTOSaveDigitalSignInfo();

            var headers = WebOperationContext.Current?.IncomingRequest?.Headers;

            string origin = headers?["Origin"];    
            string referer = headers?["Referer"];  
            try
            {
                if (!e.Data.GetDataPresent(DataFormats.FileDrop, true)) return;

                var files = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                if (files == null || files.Length == 0) return;

                if (files.Length > 1)
                {
                    MyMessageBox.ShowDialog("Please drop only one file.");
                    return;
                }

                string selectedFile = files[0];
                if (!File.Exists(selectedFile))
                {
                    MyMessageBox.ShowDialog("Invalid file.");
                    return;
                }

                await GenericSignFileAsync(selectedFile);
            }
            catch (Exception ex)
            {
                 
                ErrorLog.LogErrorToFile(ex);
            }
        }
 
        private async System.Threading.Tasks.Task GenericSignFileAsync(string filePath)
        {
            try
            {
                
                string remark = "";
                bool checkCrlTick = false;
                 
                DTOSaveDigitalSignInfo saveDigitalSignInfo;
                var headers = WebOperationContext.Current?.IncomingRequest?.Headers;

                string origin = headers?["Origin"];    
                string referer = headers?["Referer"];  

                await Dispatcher.InvokeAsync(() =>
                {
                    remark = (textRemarkAny?.Text ?? "").Trim();
                    checkCrlTick = (ChkCrl?.IsChecked == true);
                });

                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    ShowMsg("Invalid file.");
                    return;
                }

                string pattern = @"^[a-zA-Z0-9@, ._\-]+$";
                if (!Regex.IsMatch(remark, pattern) && remark != "")
                {
                    ShowMsg("Special Characters Not Allow ");
                    return;
                }
                 
                await Dispatcher.InvokeAsync(() =>
                {
                    if (DropListAny != null) DropListAny.IsEnabled = false;

                    if (progress != null)
                    {
                        progress.Visibility = Visibility.Hidden;
                        progress.Value = 0;
                    }

                    BusyBar.IsBusy = true;
                });
                 
                HelperCert helperCert = new HelperCert();
                var result = await helperCert.CheckSomethingAsync();

                if (result.Status == "0" || result.Status == "-1")
                {
                    ShowMsg(result.Remark);
                    return;
                }

                CertThumbPrint = result.Remark;
                 
                if (checkCrlTick)
                {
                    if (crloscp == true && crlocspmsg == "Digital Cert of token cannot be verified with CA due to Network issues")
                    {
                        if (ShowMsg(
                                "Digital Cert of token cannot be verified with CA due to Network issues. Do you want to continue ?",
                                MyMessageBox.Buttons.Yes_No) != "1")
                        {
                            ShowMsg("No file signed!");
                            return;
                        }
                    }
                    else if (crloscp == false)
                    {
                        ShowMsg(crlocspmsg == "Digital Cert of token cannot be verified with CA due to Network issues"
                            ? crlocspmsg
                            : "CRL Check Failed !");
                        return;
                    }
                }
                 
                X509Certificate2 cert;
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly);
                    var found = store.Certificates.Find(X509FindType.FindByThumbprint, CertThumbPrint, false);

                    if (found == null || found.Count == 0)
                    {
                        ShowMsg("Certificate not found in store.");
                        return;
                    }
                    cert = found[0];
                }

                if (DateTime.Now > cert.NotAfter && !IsLocalToken)
                {
                    ShowMsg("Token is expired. Pl contact issuer !");
                    return;
                } 
                string sigPath = await HugeFileSignatureService.SignPortableAsync(
                    filePath, cert, UpdateProgress, remark);
                if (!string.IsNullOrEmpty(sigPath))
                {
                    saveDigitalSignInfo = new DTOSaveDigitalSignInfo();
                    saveDigitalSignInfo.SignedDateTime = DateTime.Now.ToString("dd-MMM-yyyy HH:mm:ss"); 
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
                string res = ShowMsg(
                    "Congratulations!\n\nFile is digitally signed.\n\nSignature:\n" + sigPath,
                    MyMessageBox.Buttons.OK_OpenFile);

                if (res == "3")
                {
                    try { Process.Start(sigPath); } catch { }
                }
                else if (res == "2")
                {
                    try { Process.Start(Path.GetDirectoryName(sigPath)); } catch { }
                }
                if (saveDigitalSignInfo != null)
                {
                    await new Service1().SaveDigitalSignedDataToAnalytics(saveDigitalSignInfo);
                }
            }
            catch (Exception ex)
            {
                ShowMsg(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }
            finally
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    BusyBar.IsBusy = false;
                    if (DropListAny != null) DropListAny.IsEnabled = true;

                    if (progress != null)
                    {
                        progress.Value = 0;
                        progress.Visibility = Visibility.Collapsed;
                    }
                });
            }
        }


        private void UpdateProgress(double percent)
        { 
            Dispatcher.Invoke(() => progress.Value = percent);
        }

        private string ShowMsg(string text, MyMessageBox.Buttons buttons = MyMessageBox.Buttons.OK)
        {
            if (Dispatcher.CheckAccess())
                return MyMessageBox.ShowDialog(text, buttons);

            return Dispatcher.Invoke(() => MyMessageBox.ShowDialog(text, buttons));
        }

    }
}
