using iText.Kernel.Crypto;
using iText.Kernel.Pdf;
using Microsoft.Office.Interop.Word;
using SignService.Enums;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using WinniesMessageBox;

namespace SignService.Helpers
{
   

    public static class FileValidationHelper
    {
        public static DocumentValidationResult ValidateDocumentFile(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return DocumentValidationResult.FileNotFound;
                }

                FileInfo fileInfo = new FileInfo(filePath);

                // 0 byte check
                if (fileInfo.Length == 0)
                {
                    return DocumentValidationResult.ZeroByte;
                }

                // 500 MB check
                if (fileInfo.Length > 524288000)
                {
                    return DocumentValidationResult.TooLarge;
                }

                string extension = fileInfo.Extension.ToLowerInvariant();

                switch (extension)
                {
                    case ".pdf":
                        return ValidatePdfFile(filePath);

                    case ".docx":
                        return ValidateDocxFile(filePath);

                    case ".doc":
                        return ValidateDocFile(filePath);

                    default:
                        return DocumentValidationResult.Unsupported;
                }
            }
            catch
            {
                return DocumentValidationResult.Corrupted;
            }
        }

        public static DocumentValidationResult ValidatePdfFile(string filePath)
        {
            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                {
                    using (PdfDocument pdfDocument = new PdfDocument(reader))
                    {
                        int pageCount = pdfDocument.GetNumberOfPages();

                        if (pageCount <= 0)
                        {
                            return DocumentValidationResult.Corrupted;
                        }
                    }
                }

                return DocumentValidationResult.Valid;
            }
            catch (BadPasswordException)
            {
                return DocumentValidationResult.PasswordProtected;
            }
            catch
            {
                return DocumentValidationResult.Corrupted;
            }
        }

        public static DocumentValidationResult ValidateDocxFile(string filePath)
        {
            try
            {
                byte[] header = new byte[8];

                using (FileStream fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    if (fs.Length < 8)
                    {
                        return DocumentValidationResult.Corrupted;
                    }

                    fs.Read(header, 0, header.Length);
                }

                // Password-protected Office Open XML files normally
                // use OLE Compound File format.
                byte[] oleHeader =
                {
                    0xD0, 0xCF, 0x11, 0xE0,
                    0xA1, 0xB1, 0x1A, 0xE1
                };

                bool isOleFile = true;

                for (int i = 0; i < oleHeader.Length; i++)
                {
                    if (header[i] != oleHeader[i])
                    {
                        isOleFile = false;
                        break;
                    }
                }

                if (isOleFile)
                {
                    return DocumentValidationResult.PasswordProtected;
                }

                // Normal DOCX must start as a ZIP package: PK
                if (header[0] != 0x50 || header[1] != 0x4B)
                {
                    return DocumentValidationResult.Corrupted;
                }

                using (FileStream fs = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read))
                {
                    using (ZipArchive archive =
                        new ZipArchive(fs, ZipArchiveMode.Read))
                    {
                        // Required DOCX files
                        ZipArchiveEntry contentTypes =
                            archive.GetEntry("[Content_Types].xml");

                        ZipArchiveEntry documentXml =
                            archive.GetEntry("word/document.xml");

                        if (contentTypes == null || documentXml == null)
                        {
                            return DocumentValidationResult.Corrupted;
                        }

                        // Force reading some data to detect damaged ZIP entries.
                        using (Stream stream = documentXml.Open())
                        {
                            byte[] buffer = new byte[1024];

                            while (stream.Read(buffer, 0, buffer.Length) > 0)
                            {
                                // Just reading to verify integrity
                            }
                        }
                    }
                }

                return DocumentValidationResult.Valid;
            }
            catch (InvalidDataException)
            {
                return DocumentValidationResult.Corrupted;
            }
            catch
            {
                return DocumentValidationResult.Corrupted;
            }
        }

        public static DocumentValidationResult ValidateDocFile(string filePath)
        {
            Microsoft.Office.Interop.Word.Application wordApp = null;
            Microsoft.Office.Interop.Word.Document document = null;

            try
            {
                wordApp =
                    new Microsoft.Office.Interop.Word.Application();

                wordApp.Visible = false;

                wordApp.DisplayAlerts =
                    WdAlertLevel.wdAlertsNone;

                /*
                 * Intentionally provide an invalid password.
                 *
                 * Normal document:
                 *     Opens successfully.
                 *
                 * Password-protected document:
                 *     Word throws password-related exception.
                 */

                document = wordApp.Documents.Open(
                    FileName: filePath,
                    ConfirmConversions: false,
                    ReadOnly: true,
                    AddToRecentFiles: false,
                    PasswordDocument: "__DGIS_INVALID_PASSWORD__",
                    Visible: false,
                    OpenAndRepair: false,
                    NoEncodingDialog: true);

                return DocumentValidationResult.Valid;
            }
            catch (COMException ex)
            {
                string errorMessage =
                    (ex.Message ?? string.Empty).ToLowerInvariant();

                if (errorMessage.Contains("password") ||
                    errorMessage.Contains("encrypted") ||
                    errorMessage.Contains("encryption"))
                {
                    return DocumentValidationResult.PasswordProtected;
                }

                return DocumentValidationResult.Corrupted;
            }
            catch
            {
                return DocumentValidationResult.Corrupted;
            }
            finally
            {
                if (document != null)
                {
                    try
                    {
                        document.Close(
                            WdSaveOptions.wdDoNotSaveChanges);
                    }
                    catch
                    {
                    }

                    try
                    {
                        Marshal.FinalReleaseComObject(document);
                    }
                    catch
                    {
                    }

                    document = null;
                }

                if (wordApp != null)
                {
                    try
                    {
                        wordApp.Quit(
                            WdSaveOptions.wdDoNotSaveChanges);
                    }
                    catch
                    {
                    }

                    try
                    {
                        Marshal.FinalReleaseComObject(wordApp);
                    }
                    catch
                    {
                    }

                    wordApp = null;
                }
            }
        }

        public static void ShowFileValidationMessage(DocumentValidationResult result, string filePath)
        {
            string fileName = Path.GetFileName(filePath);

            switch (result)
            {
                case DocumentValidationResult.FileNotFound:

                    MyMessageBox.ShowDialog($"File not found.\n\nFile: {fileName}");

                    break;

                case DocumentValidationResult.ZeroByte:

                    MyMessageBox.ShowDialog($"The selected file is empty (0 byte).\n\nFile: {fileName}\n\nPlease select a valid document.");

                    break;

                case DocumentValidationResult.TooLarge:

                    MyMessageBox.ShowDialog($"File size is too large.\n\nFile: {fileName}\n\nPlease select a file less than 500 MB.");

                    break;

                case DocumentValidationResult.PasswordProtected:

                    MyMessageBox.ShowDialog($"The selected document is password protected or encrypted.\n\nFile: {fileName}\n\nPlease remove the password/encryption and try again.");

                    break;

                case DocumentValidationResult.Corrupted:

                    MyMessageBox.ShowDialog($"The selected document is corrupted or unreadable.\n\nFile: {fileName}\n\nPlease select a valid PDF or Word document.");

                    break;

                case DocumentValidationResult.Unsupported:

                    MyMessageBox.ShowDialog($"Unsupported file type.\n\nFile: {fileName}\n\nOnly PDF, DOC and DOCX files are allowed.");

                    break;
            }
        }
    }
}