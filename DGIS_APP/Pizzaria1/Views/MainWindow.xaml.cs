using SignService;
using SignService.HttpClients;
using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;
using WinniesMessageBox;

namespace DGISApp
{      
    public partial class MainWindow : Window
    {
        public Boolean CheckStatus;
        private readonly ApiClient _api = new ApiClient();

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded; 
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                 
                await Task.Yield();
                 
                _ = RunStartupCallsAsync();   
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
            }
        }

        private async Task RunStartupCallsAsync()
        {
            try
            {
                await Task.WhenAll(
                    SaveInstallationAsync(),
                    SaveDailyRunAsync(),
                    XMLAPICallAsync()
                );
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
            }
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            if (CheckStatus==false)
            { 
                
                CheckStatus = true;
            }
        }

        public class PCDetail
        {            
            public string domainId { get; set; }
            public string ipAddress { get; set; }
            public string version { get; set; }
        }
        private async Task XMLAPICallAsync()
        {
            try
            {
                XmlDataForPublicKey xmlDataForPublicKey = new XmlDataForPublicKey();
               
                string path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string Appfolder = System.IO.Path.Combine(path, "DGIS");
                Directory.CreateDirectory(Appfolder);
                string filePath = System.IO.Path.Combine(Appfolder, "PublicKeyData.xml");

                if (File.Exists(filePath))  
                {
                    FileInfo fi = new FileInfo(filePath);
                   
                    XmlDocument doc = new XmlDocument();
                    doc.Load(filePath);

                    XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
                    nsmgr.AddNamespace("ns", "http://schemas.datacontract.org/2004/07/SignService");

                    XmlNodeList nodes = doc.SelectNodes($"/PublicKeysData/ns:XmlDataForPublicKey", nsmgr);
                    if (File.Exists(filePath))  
                    {
                        File.Delete(filePath);  
                        Console.WriteLine("File deleted successfully.");
                    }
                    if (nodes != null)
                    {
                        foreach (XmlNode node in nodes)
                        { 
                            xmlDataForPublicKey.SerialNo = node.SelectSingleNode("ns:SerialNo", nsmgr)?.InnerText;
                            xmlDataForPublicKey.Public_Key = node.SelectSingleNode("ns:Public_Key", nsmgr)?.InnerText;
                            xmlDataForPublicKey.Status = Convert.ToBoolean(node.SelectSingleNode("ns:Status", nsmgr)?.InnerText);                           
                            xmlDataForPublicKey.TokenValid = Convert.ToBoolean(node.SelectSingleNode("ns:Status", nsmgr)?.InnerText);
                            xmlDataForPublicKey.ValidFrom = node.SelectSingleNode("ns:ValidFrom", nsmgr)?.InnerText;
                            xmlDataForPublicKey.ValidTo = node.SelectSingleNode("ns:ValidTo", nsmgr)?.InnerText;


                            if (xmlDataForPublicKey.Status == false)
                            {

                                bool ret = await SaveUserDataAsync(xmlDataForPublicKey);

                               if(ret) xmlDataForPublicKey.Status = true;
                            }
                            SaveToXml(xmlDataForPublicKey, filePath);


                        }

                    }
                }
            }
            catch (Exception ex) {

                ErrorLog.LogErrorToFile(ex);
            }
           
        }
        static void SaveToXml(XmlDataForPublicKey data, string filePath)
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(XmlDataForPublicKey));

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
        public async Task<bool> SaveUserDataAsync(XmlDataForPublicKey xmlDataForPublicKey)
        {
           return await _api.PostRequestAsync<bool>("/api/transaction/SaveUserData", xmlDataForPublicKey);      
        }

        public PCDetail GetPCDetail()
        {
            String NVersion = "";
            IPAddress[] a = Dns.GetHostByName(Dns.GetHostName()).AddressList;

            Assembly assembly = Assembly.GetEntryAssembly();
            NVersion = assembly.GetName().Version.ToString(); 
            string ip = a[0].ToString();
            string hostName = Dns.GetHostName();
            string version = NVersion;
            var PCDetail = new PCDetail
            {
                ipAddress = ip,
                domainId = hostName,
                version = version
            };
            return PCDetail;
        }

        private Task<bool> SaveInstallationAsync()
        {
            return _api.PostRequestAsync<bool>("/api/transaction/SaveInstallationAsync", GetPCDetail());
        }

        private Task<bool> SaveDailyRunAsync()
        {
            return _api.PostRequestAsync<bool>("/api/transaction/SaveDailyRunAsync", GetPCDetail());
        }

        private void ButtonFechar_Click(object sender, RoutedEventArgs e)
        { 
            this.Visibility = Visibility.Hidden;
        }

        private void ButtonMinimize_Click(object sender, RoutedEventArgs e)
        { 
            this.Visibility = Visibility.Hidden;
        }

        private void ButtonFechar_Click1(object sender, RoutedEventArgs e)
        {
            if (ConfigurationManager.AppSettings.Get("loginStatus") == "0")
            {
                MyMessageBox.ShowDialog("you are already logged out.");
            }
            else
            {
                ConfigurationManager.AppSettings.Set("loginStatus", "0");
                GridPrincipal.Children.Clear();
                GridPrincipal.Children.Add(new Home());
                ListViewMenu.SelectedIndex = 0;
            }
        }

        private void ListViewMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = ListViewMenu.SelectedIndex;
            MoveCursorMenu(index);

            switch (index)
            {
                case 0:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Home());
                    break;
                case 1:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Home());
                    break;
                case 2:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Home());
                    break;
                case 3:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Home());
                    break;
                case 4:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Home());
                    break;
                case 5:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new Watermark());
                    break;

                case 6:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new DigitalSign());
                    break;
                case 7:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new VerifyDigitalSign());
                    break;
                case 8:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new SymmetricEncrypt());
                    break;
                case 9:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new SymmetricDecryption());
                    break;
                
                case 10:
                    GridPrincipal.Children.Clear();
                    GridPrincipal.Children.Add(new About());
                    break;
                case 11:

                    string FileName = System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DGIS_Help.pdf";

                    FileInfo fi = new FileInfo(FileName);
                    if (fi.Exists)
                    {
                        Process.Start(FileName);
                    }
                    break;
                default:
                    break;
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            this.DragMove();
        }
        private void MoveCursorMenu(int index)
        {
        }
    }
}
