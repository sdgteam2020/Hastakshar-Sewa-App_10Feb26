using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace WinniesMessageBox
{
   
    public partial class WebView : Window
    {
        public string ReturnString { get; set; }
        public WebView(string PdfFile, MyMessageBox.Buttons buttons)
        {
            InitializeComponent();
            CustomSignCordinate.X = 0;
            CustomSignCordinate.Y = 0;
            CustomSignCordinate.PageNo = 1;
            InitializeWebView(PdfFile);
        }

        private async void InitializeWebView(string PdfFile)
        {
            
            CustomSignCordinate.PdfFile = PdfFile;
            if(webView.IsInitialized)
            {
                await webView.EnsureCoreWebView2Async();
            }
            else
            {
                var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", ""));
                await webView.EnsureCoreWebView2Async(environment);
            }
            webView.CoreWebView2.Settings.IsScriptEnabled = true;
            //string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string PDFViewerWithCordinates=  System.Reflection.Assembly.GetEntryAssembly().Location.ToString().Replace("\\DGISAPP.exe", "");
            webView.CoreWebView2.SetVirtualHostNameToFolderMapping("local", PDFViewerWithCordinates+"\\PDFViewerWithCordinates\\", CoreWebView2HostResourceAccessKind.Allow);

            //webView.CoreWebView2.SetVirtualHostNameToFolderMapping("local", PDFViewerWithCordinates, CoreWebView2HostResourceAccessKind.Allow);
            webView.Source = new Uri("http://local/index.html");
            // webView.Source = new Uri("https://www.google.com/");
            // Wait for the page to load fully before executing JavaScript
            // Attach event handler
            webView.CoreWebView2.WebMessageReceived += async (sender, e) => await WebView_WebMessageReceivedAsync(sender, e);
            webView.CoreWebView2.NavigationCompleted += (sender, e) =>
            {
                if (e.IsSuccess)
                {
                    // Run the script to get the initial value from the div after the page is loaded
                    GetDivValueAsync();
                }
            };
           

        }
        // This is the method that retrieves the value from the DIV asynchronously
        private async Task GetDivValueAsync()
        {
            string script = "document.getElementById('myDiv').innerText;";
            string divValue = await webView.CoreWebView2.ExecuteScriptAsync(script);
            divValue = divValue.Trim('"');  // Clean the result (remove quotes)

           // MessageBox.Show("DIV Value: " + divValue);
        }
        DoubleAnimation anim;
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Closing -= Window_Closing;
            e.Cancel = true;
            anim = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(0.3));
            anim.Completed += (s, _) => this.Close();
            this.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private async Task WebView_WebMessageReceivedAsync(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.WebMessageAsJson; // Message received from JavaScript

                if (webView.CoreWebView2 != null)
                {
                    // Fetch the coordinates and page number from the JavaScript document
                    string script = "document.getElementById('coordinates').innerText;";
                    string pageNoScript = "document.getElementById('page-num').innerText;";

                    string divValue = await webView.CoreWebView2.ExecuteScriptAsync(script);
                    string pageNoGet = await webView.CoreWebView2.ExecuteScriptAsync(pageNoScript);

                    // Trim unnecessary characters (JSON strings return values wrapped in quotes)
                    divValue = divValue.Trim('"');
                    pageNoGet = pageNoGet.Trim('"');

                    if (!string.IsNullOrEmpty(divValue) && !string.IsNullOrEmpty(pageNoGet))
                    {
                        string[] coordinates = divValue.Split(',');

                        // Ensure coordinate array contains valid numeric values
                        if (coordinates.Length == 2 &&
                            int.TryParse(coordinates[0].Trim(), out int x) &&
                            int.TryParse(coordinates[1].Trim(), out int y) &&
                            int.TryParse(pageNoGet.Trim(), out int pageNo))
                        {
                            if (x > 0 && pageNo > 0)
                            {
                                CustomSignCordinate.X = x;
                                CustomSignCordinate.Y = y;
                                CustomSignCordinate.PageNo = pageNo;

                                ReturnString = "1"; // Some return value indicating success
                            }
                            else
                            {
                                ReturnString = "-2";
                            }
                        }
                        else
                        {
                            ReturnString = "-2"; // Invalid coordinate format
                        }
                    }
                    else
                    {
                        ReturnString = "-2"; // Empty values
                    }
                }
            }
            catch (Exception ex)
            {
                ReturnString = "-2";
                MessageBox.Show("Error: " + ex.Message);
            }
            finally
            {
                Close();
            }
        }


    }
}
