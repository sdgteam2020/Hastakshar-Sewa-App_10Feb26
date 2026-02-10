
using iText.IO.Font;

using iText.IO.Font.Constants;

using iText.Kernel.Colors;

using iText.Kernel.Font;

using iText.Kernel.Pdf;

using iText.Kernel.Pdf.Canvas;

using iText.Kernel.Pdf.Extgstate;

using Microsoft.Office.Interop.Word;

using Microsoft.Win32;
using SignService;
using SignService.Helpers;
using System;

using System.Configuration;

using System.Diagnostics;

using System.IO;

using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Windows;

using System.Windows.Controls;

using WinniesMessageBox;



namespace DGISApp

{

    /// <summary>

    /// Interação lógica para UserControlEscolha.xam

    /// </summary>

    public partial class Watermark : UserControl

    {



        string[] droppedFilePaths = null;

        string download = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads";



        public Watermark()

        {

            InitializeComponent();

        }



        void check(string filename, double pr)

        {



        }
        private string GetLocalIPAddress()
        {
            string IpAddrress = "";
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
                foreach (var network in networkInterfaces)
                {
                    if (network.OperationalStatus != OperationalStatus.Up)
                        continue;
                    if (network.Description.Contains("adapter") || network.Description.Contains("VirtualBox"))
                        continue;

                    var properties = network.GetIPProperties();
                    if (properties == null)
                        continue;

                    foreach (var address in properties.UnicastAddresses)
                    {
                        if (address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(address.Address))
                        {
                            IpAddrress = address.Address.ToString();
                        }
                    }
                }
                return IpAddrress;
            }
            catch (Exception ex)
            {
                return "";
            }

        }


        private void DropList_DragEnter(object sender, DragEventArgs e)

        {

        }



        private void DropList_Drop(object sender, DragEventArgs e)
        {

            try

            {
                string email = textBoxEmail.Text;
                string pattern = @"^[a-zA-Z0-9 ,]+$";
                if (Regex.IsMatch(email, pattern) || textBoxEmail.Text == "")
                {
                    if (datetime.IsChecked == true || ipaddress.IsChecked == true || textBoxEmail.Text != "")

                    {

                        if (e.Data.GetDataPresent(DataFormats.FileDrop, true))

                        {

                            DropList.IsEnabled = false;
                            BusyBar.IsBusy = true;
                            droppedFilePaths = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                            this.upload();

                            DropList.IsEnabled = true;
                            BusyBar.IsBusy = false;
                        }

                    }

                    else

                    {

                        MyMessageBox.ShowDialog("Please select atleast one option for Watermarking");

                    }
                }
                else
                {
                    MyMessageBox.ShowDialog("Special Characters Not Allow ");

                }


            }

            catch (Exception ex)

            {

                MyMessageBox.ShowDialog(ex.Message);

                DropList.IsEnabled = true;
                BusyBar.IsBusy = false;
                ErrorLog.LogErrorToFile(ex);
            }

        }



        void upload()

        {

            string DownloadPath = "";

            String NewFileName = "";

            string WatermarkedPDFFileName = "";

            string WaterMarkingText = this.textBoxEmail.Text.ToString();



            // Split the input string by comma and store the result in a string array

            string[] stringArray = this.textBoxEmail.Text.ToString().Split(',');



            int j = 0;
            int pagecharerror = 0;
            foreach (var path in droppedFilePaths)

            {

            nextfile:

                string fileforloop = path;

                ConfigurationManager.AppSettings["LastSelectedLocation"] = Path.GetDirectoryName(path);

                DownloadPath = Path.GetDirectoryName(path);



                if (NewFileName != "")

                {

                    fileforloop = NewFileName;

                }

                else

                {

                    fileforloop = path;

                }

                FileInfo fi = new FileInfo(fileforloop);
                if(fi.Length<= 524288000)
                {
                    if (fi.Extension == ".pdf")
                    {



                        foreach (string item in stringArray)
                        {

                            WaterMarkingText = item;
                            if(WaterMarkingText.Length>=20)
                            {
                                pagecharerror = 1;
                                MyMessageBox.ShowDialog("Ensure the watermark text per document is no more than 20 characters");
                                break;
                            }

                            WatermarkedPDFFileName = DownloadPath + "\\" + fi.Name.Substring(0, fi.Name.Length - fi.Extension.Length) + "_WM_" + WaterMarkingText + "_" + DateTime.Now.ToString("ddMMM") + "_" + DateTime.Now.Millisecond + ".pdf";

                            PdfDocument pdfDoc = new PdfDocument(new PdfReader(fi.FullName), new PdfWriter(WatermarkedPDFFileName));

                            PdfCanvas under = new PdfCanvas(pdfDoc.GetFirstPage().NewContentStreamBefore(), new PdfResources(), pdfDoc);

                            PdfFont font = PdfFontFactory.CreateFont(FontProgramFactory.CreateFont(StandardFonts.TIMES_ROMAN));



                            iText.Layout.Element.Paragraph paragraph = new iText.Layout.Element.Paragraph("This watermark is added UNDER the existing content")

                                    .SetFont(font)

                                    .SetBold()

                                    .SetFontColor(ColorConstants.RED)

                                    .SetFontSize(48);



                            // Print each element of the string array







                            for (int i = 1; i <= pdfDoc.GetNumberOfPages(); i++)

                            {

                                PdfCanvas over = new PdfCanvas(pdfDoc.GetPage(i));
                                PdfPage page = pdfDoc.GetPage(i);
                                PdfCanvas over1 = new PdfCanvas(page);
                                // Get actual page size (width and height)
                                iText.Kernel.Geom.Rectangle pageSize = page.GetPageSize();
                                float pageWidth = pageSize.GetWidth();
                                float pageHeight = pageSize.GetHeight();
                                // Dynamically calculate font size (e.g., 10% of page width)
                                float dynamicFontSize = pageWidth * 0.06f; // You can adjust the multiplier (e.g., 0.05f to 0.12f)

                                if (this.Dispatcher.Invoke(new Func<bool?>(() => this.datetime.IsChecked)) == true && this.Dispatcher.Invoke(new Func<bool?>(() => this.ipaddress.IsChecked)) == false)

                                {

                                    paragraph = new iText.Layout.Element.Paragraph(DateTime.Now.ToString() + "\n" + this.Dispatcher.Invoke(new Func<string>(() => WaterMarkingText)))

                                        .SetFont(font)

                                      .SetFontColor(ColorConstants.RED)

                                      .SetFontSize(dynamicFontSize);

                                    over.SaveState();

                                    PdfExtGState gs3 = new PdfExtGState();

                                    gs3.SetFillOpacity(0.5f);

                                    over.SetExtGState(gs3);

                                    iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize())

                                            .ShowTextAligned(paragraph, pageWidth / 2, pageHeight / 2, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45);

                                    canvasWatermark.Close();

                                }



                                else if (this.Dispatcher.Invoke(new Func<bool?>(() => this.ipaddress.IsChecked)) == true && this.Dispatcher.Invoke(new Func<bool?>(() => this.datetime.IsChecked)) == false)

                                {

                                    //IPAddress[] a = Dns.GetHostByName(Dns.GetHostName()).AddressList;

                                    string ip = GetLocalIPAddress();//a[0].ToString();

                                    paragraph = new iText.Layout.Element.Paragraph(ip + "\n" + this.Dispatcher.Invoke(new Func<string>(() => WaterMarkingText)))

                                       .SetFont(font)

                                      .SetFontColor(ColorConstants.RED)

                                      .SetFontSize(dynamicFontSize);

                                    over.SaveState();

                                    PdfExtGState gs3 = new PdfExtGState();

                                    gs3.SetFillOpacity(0.5f);

                                    over.SetExtGState(gs3);

                                    iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize())

                                            .ShowTextAligned(paragraph, pageWidth / 2, pageHeight / 2, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45);

                                    canvasWatermark.Close();

                                }



                                else if (this.Dispatcher.Invoke(new Func<bool?>(() => this.datetime.IsChecked)) == true && this.Dispatcher.Invoke(new Func<bool?>(() => this.ipaddress.IsChecked)) == true)

                                {

                                    //IPAddress[] a = Dns.GetHostByName(Dns.GetHostName()).AddressList;

                                    string ip = GetLocalIPAddress();//a[0].ToString();

                                    paragraph = new iText.Layout.Element.Paragraph(DateTime.Now.ToString() + "\n" + ip + "\n" + this.Dispatcher.Invoke(new Func<string>(() => WaterMarkingText)))

                                      .SetFont(font)

                                      .SetFontColor(ColorConstants.RED)

                                      .SetFontSize(dynamicFontSize);

                                    over.SaveState();

                                    PdfExtGState gs3 = new PdfExtGState();

                                    gs3.SetFillOpacity(0.5f);

                                    over.SetExtGState(gs3);

                                    iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize())

                                            .ShowTextAligned(paragraph, pageWidth / 2, pageHeight / 2, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45);

                                    canvasWatermark.Close();

                                }

                                else

                                {

                                    paragraph = new iText.Layout.Element.Paragraph(this.Dispatcher.Invoke(new Func<string>(() => WaterMarkingText)))

                                          .SetFont(font)

                                          .SetFontColor(ColorConstants.RED)

                                          .SetFontSize(dynamicFontSize);

                                    over.SaveState();

                                    PdfExtGState gs3 = new PdfExtGState();

                                    gs3.SetFillOpacity(0.5f);

                                    over.SetExtGState(gs3);

                                    iText.Layout.Canvas canvasWatermark = new iText.Layout.Canvas(over, pdfDoc.GetDefaultPageSize())

                                            .ShowTextAligned(paragraph, pageWidth / 2, pageHeight / 2, 1, iText.Layout.Properties.TextAlignment.CENTER, iText.Layout.Properties.VerticalAlignment.TOP, 45);

                                    canvasWatermark.Close();

                                }

                                over.RestoreState();

                            }

                            pdfDoc.Close();

                            NewFileName = "";

                        }





                        j = j + 1;

                        //MyMessageBox.ShowDialog("Congratulations ! \n\n Document is successfully WaterMarked.\n" + download);

                    }

                    else if (Path.GetExtension(path) == ".docx" || Path.GetExtension(path) == ".doc")

                    {

                        String DocfileName = Path.GetFileNameWithoutExtension(path);

                        NewFileName = System.IO.Path.GetTempPath() + "\\" + DocfileName + ".pdf";

                        if (NewFileName.Length > 255)
                        {
                            MyMessageBox.ShowDialog("FileName too long!");
                            goto nextfile;

                        }
                        else
                        {
                            helper.ConvertPDF(path, NewFileName, WdSaveFormat.wdFormatPDF);
                        }



                        goto nextfile;

                    }

                    else

                    {

                        MyMessageBox.ShowDialog("Please select only PDF/Doc document for WaterMarking.");

                    }
                }
                else
                {
                    MyMessageBox.ShowDialog("File Size is too large, please select file less than 500 MB");
                    break;
                }

            }

            if (j == droppedFilePaths.Length && pagecharerror==0)

            {

                string Result = "0";



             


                Result = MyMessageBox.ShowDialog("Congratulations ! \n\n Document is successfully WaterMarked.\n" + DownloadPath, MyMessageBox.Buttons.OK_OpenFile);



                if (Result == "2")

                {

                    string FilePath = Path.GetDirectoryName(WatermarkedPDFFileName);

                    Process.Start(FilePath);

                }

                else if (Result == "3")

                {

                    try

                    {

                        Process.Start(WatermarkedPDFFileName);

                    }

                    catch (Exception ex)

                    {

                        Console.WriteLine("An error occurred: " + ex.Message);

                    }

                }



            }

            else if(pagecharerror == 0)

            {

                MyMessageBox.ShowDialog("some document not successfully WaterMarked.\n" + DownloadPath);

            }

        }



        private void Button_Click(object sender, RoutedEventArgs e)

        {



        }



        private void btnOpenFiles_Click(object sender, RoutedEventArgs e)

        {

            try

            {
                string email = textBoxEmail.Text;
                string pattern = @"^[a-zA-Z0-9 ,]+$";
                if (Regex.IsMatch(email, pattern) || textBoxEmail.Text=="")
                {
                    if (datetime.IsChecked == true || ipaddress.IsChecked == true || textBoxEmail.Text != "")
                    {

                        string value = datetime.IsChecked.ToString();

                        OpenFileDialog openFileDialog = new OpenFileDialog();

                        openFileDialog.Multiselect = true;

                        openFileDialog.Filter = "files (*.pdf;*.PDF;*.docx;*.DOCX;*.doc;*.DOC)|*.pdf;*.PDF;*.docx;*.DOCX,*.doc; *.DOC";

                        ///openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);



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

                            DropList.IsEnabled = false;
                            BusyBar.IsBusy = true;
                            droppedFilePaths = openFileDialog.FileNames;

                            this.upload();

                            DropList.IsEnabled = true;
                            BusyBar.IsBusy = false;

                        }

                    }

                    else

                    {

                        MyMessageBox.ShowDialog("Please select atleast one option for Watermarking");

                    }
                }
                else
                {
                    MyMessageBox.ShowDialog("Special Characters Not Allow ");
                }

            }

            catch (Exception ex)

            {

                MyMessageBox.ShowDialog(ex.Message);

                DropList.IsEnabled = true;
                BusyBar.IsBusy = false;
                ErrorLog.LogErrorToFile(ex);
            }

        }



   

    }



}