using SignService;
using SignService.Security;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Windows;
using WinniesMessageBox;
namespace DGISApp
{

    //[PrincipalPermissionAttribute(SecurityAction.Demand, Role = @"BUILTIN\Administrators")]
    public partial class App : Application
    {
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip;

        ServiceHost host = null;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                MainWindow wnd = new MainWindow();
                bool isNewInstance = true;
                // MyMessageBox.ShowDialog("Error");

                // Check if another instance of the application is already running
                Process currentProcess = Process.GetCurrentProcess();
                Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (Process process in processes)
                {
                    if (process.Id != currentProcess.Id)
                    {
                        process.Kill();
                        process.WaitForExit();
                        isNewInstance = true;
                        //isNewInstance = false;
                        break;
                    }
                }
                if (isNewInstance)
                {
                    // Create device.dat on first run, reuse if already exists
                    var creds = DeviceCredentialStore.GetOrCreate();

                    this.host = new ServiceHost(typeof(SignService.Service1));
                    foreach (ServiceEndpoint EP in host.Description.Endpoints)
                        EP.Behaviors.Add(new BehaviorAttribute());

                    host.Open();

                    string programsFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.StartMenu) + "//Programs//DGIS//1//DGIS App.appref-ms";
                    string startupFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.Startup);


                    System.Windows.Forms.NotifyIcon DGISIcon = new System.Windows.Forms.NotifyIcon();
                    DGISIcon.Icon = new Icon(System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "") + "\\DigiSign.ico");
                    DGISIcon.Visible = true;
                    contextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
                    contextMenuStrip.Items.Add("Open", null, Open_Click);
                    contextMenuStrip.Items.Add("Close", null, Close_Click);
                    DGISIcon.ContextMenuStrip = contextMenuStrip;
                    DGISIcon.DoubleClick += Open_Click;

                    wnd.Show();

                }

                if (Convert.ToInt32(ConfigurationManager.AppSettings["IsOldDGISExits"]) <= 10)
                {
                    string clickOncePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Apps\2.0";
                    string appName = "dgis"; // Replace with your application's name

                    int IsDeleteOldDgisApp = 0;
                    // Search for the application directory
                    foreach (var directory in Directory.GetDirectories(clickOncePath, "*", SearchOption.AllDirectories))
                    {
                        if (directory.Contains(appName))
                        {
                            Directory.Delete(directory, true);
                            IsDeleteOldDgisApp = 1;
                        }
                    }
                    if (IsDeleteOldDgisApp == 0)
                    {
                        ConfigurationManager.AppSettings["IsOldDGISExits"] = 1 + ConfigurationManager.AppSettings["IsOldDGISExits"];
                    }
                    RemoveOldDGISStartMenu();
                    RemoveOldDGISStartMenu1();
                }

            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                ErrorLog.LogErrorToFile(ex);
            }

        }
        private void RemoveOldDGISStartMenu1()
        {
            //C: \Users\asdc\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, @"Microsoft\Windows\Start Menu\Programs\Startup\DGIS App.appref-ms");
            //MessageBox.Show(filePath);
            if (File.Exists(filePath))
            {

                try
                {
                    // Command to delete the file using CMD
                    string command = $"/C del \"{filePath}\"";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = command,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = new Process { StartInfo = psi })
                    {
                        process.Start();
                        process.WaitForExit();

                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        if (!string.IsNullOrEmpty(error))
                        {
                            Console.WriteLine("Error: " + error);
                        }
                        else
                        {
                            Console.WriteLine("File deleted successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("File not found.");
            }
        }
        private void RemoveOldDGISStartMenu()
        {

            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string filePath = Path.Combine(appDataPath, @"Microsoft\Windows\Start Menu\Programs\DGIS\1\DGIS App.appref-ms");
            //MessageBox.Show(filePath);
            if (File.Exists(filePath))
            {

                try
                {
                    // Command to delete the file using CMD
                    string command = $"/C del \"{filePath}\"";

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = command,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = new Process { StartInfo = psi })
                    {
                        process.Start();
                        process.WaitForExit();

                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();

                        if (!string.IsNullOrEmpty(error))
                        {
                            Console.WriteLine("Error: " + error);
                        }
                        else
                        {
                            Console.WriteLine("File deleted successfully.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("File not found.");
            }
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
            catch (Exception ex)
            {
                MyMessageBox.ShowDialog("Error while executing Download Cert. Reason:- " + ex.Message);
                ErrorLog.LogErrorToFile(ex);
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

        private void CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            string destPath = Path.Combine(destDir, "DGIS App.appref-ms");

            File.Copy(sourceDir, destPath, true);
        }


        void Open_Click(object sender, EventArgs e)
        {
            MainWindow.Visibility = Visibility.Visible;
            MainWindow.WindowState = WindowState.Normal;
        }

        void Close_Click(object sender, EventArgs e)
        {
            string processName = "DGISApp";
            Process[] processes = Process.GetProcessesByName(processName);

            foreach (Process process in processes)
            {
                try
                {
                    if (process.Id != Process.GetCurrentProcess().Id)
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error while terminating process: {ex.Message}");
                    ErrorLog.LogErrorToFile(ex);
                }
            }
            Process.GetCurrentProcess().Kill();
        }


        public void installCert(string subjectName, string url)
        {
            X509Store store = new X509Store(StoreName.Root, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadWrite);
            X509Certificate2Collection collection = (X509Certificate2Collection)store.Certificates;
            X509Certificate2Collection fcollection = (X509Certificate2Collection)collection.Find(X509FindType.FindBySubjectName, subjectName, false);


            if (fcollection.Count == 0)
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
                    store.Add(cert);
                    store.Close();
                }
                catch (WebException ex)
                {
                    MyMessageBox.ShowDialog(ex.Message);
                    ErrorLog.LogErrorToFile(ex);
                }

            }
        }
    }

    public class BehaviorAttribute : Attribute, IEndpointBehavior,
                                IOperationBehavior
    {
        public void Validate(ServiceEndpoint endpoint) { }

        public void AddBindingParameters(ServiceEndpoint endpoint,
                                 BindingParameterCollection bindingParameters)
        { }

        /// <summary>
        /// This service modify or extend the service across an endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint that exposes the contract.</param>
        /// <param name="endpointDispatcher">The endpoint dispatcher to be
        /// modified or extended.</param>
        public void ApplyDispatchBehavior(ServiceEndpoint endpoint,
                                          EndpointDispatcher endpointDispatcher)
        {
            // add inspector which detects cross origin requests
            endpointDispatcher.DispatchRuntime.MessageInspectors.Add(
                                                   new MessageInspector(endpoint));
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint,
                                        ClientRuntime clientRuntime)
        { }

        public void Validate(OperationDescription operationDescription) { }

        public void ApplyDispatchBehavior(OperationDescription operationDescription,
                                          DispatchOperation dispatchOperation)
        { }

        public void ApplyClientBehavior(OperationDescription operationDescription,
                                        ClientOperation clientOperation)
        { }

        public void AddBindingParameters(OperationDescription operationDescription,
                                  BindingParameterCollection bindingParameters)
        { }

    }

    public class MessageInspector : IDispatchMessageInspector
    {
        private ServiceEndpoint _serviceEndpoint;

        public MessageInspector(ServiceEndpoint serviceEndpoint)
        {
            _serviceEndpoint = serviceEndpoint;
        }

        /// <summary>
        /// Called when an inbound message been received
        /// </summary>
        /// <param name="request">The request message.</param>
        /// <param name="channel">The incoming channel.</param>
        /// <param name="instanceContext">The current service instance.</param>
        /// <returns>
        /// The object used to correlate stateMsg. 
        /// This object is passed back in the method.
        /// </returns>
        public object AfterReceiveRequest(ref Message request,
                                              IClientChannel channel,
                                              InstanceContext instanceContext)
        {
            StateMessage stateMsg = null;
            HttpRequestMessageProperty requestProperty = null;
            if (request.Properties.ContainsKey(HttpRequestMessageProperty.Name))
            {
                requestProperty = request.Properties[HttpRequestMessageProperty.Name]
                                  as HttpRequestMessageProperty;
            }

            if (requestProperty != null)
            {
                var origin = requestProperty.Headers["Origin"];
                if (!string.IsNullOrEmpty(origin))
                {
                    stateMsg = new StateMessage();
                    // if a cors options request (preflight) is detected, 
                    // we create our own reply message and don't invoke any 
                    // operation at all.
                    if (requestProperty.Method == "OPTIONS")
                    {
                        stateMsg.Message = Message.CreateMessage(request.Version, null);
                    }
                    request.Properties.Add("CrossOriginResourceSharingState", stateMsg);
                }
            }

            return stateMsg;
        }

        /// <summary>
        /// Called after the operation has returned but before the reply message
        /// is sent.
        /// </summary>
        /// <param name="reply">The reply message. This value is null if the 
        /// operation is one way.</param>
        /// <param name="correlationState">The correlation object returned from
        ///  the method.</param>
        public void BeforeSendReply(ref Message reply, object correlationState)
        {
            var stateMsg = correlationState as StateMessage;

            if (stateMsg != null)
            {
                if (stateMsg.Message != null)
                {
                    reply = stateMsg.Message;
                }

                HttpResponseMessageProperty responseProperty = null;

                if (reply.Properties.ContainsKey(HttpResponseMessageProperty.Name))
                {
                    responseProperty = reply.Properties[HttpResponseMessageProperty.Name]
                                       as HttpResponseMessageProperty;
                }

                if (responseProperty == null)
                {
                    responseProperty = new HttpResponseMessageProperty();
                    reply.Properties.Add(HttpResponseMessageProperty.Name,
                                         responseProperty);
                }

                // Access-Control-Allow-Origin should be added for all cors responses
                responseProperty.Headers.Set("Access-Control-Allow-Origin", "*");

                if (stateMsg.Message != null)
                {
                    // the following headers should only be added for OPTIONS requests
                    responseProperty.Headers.Set("Access-Control-Allow-Methods",
                                                 "POST, OPTIONS, GET");
                    responseProperty.Headers.Set("Access-Control-Allow-Headers",
                              "Content-Type, Accept, Authorization, x-requested-with");
                }
            }
        }



    }

    class StateMessage
    {
        public Message Message;
    }
}