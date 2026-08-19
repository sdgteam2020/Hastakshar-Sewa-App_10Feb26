using Newtonsoft.Json;
using SignService;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Xml;
using WinniesMessageBox;
namespace DGISAPP.NamedPipe
{
    public sealed class DgisNamedPipeServer : IDisposable
    {
        public const string PipeName = "DGISSignerPipeV1";

        private volatile bool _stopping;
        private Task _serverTask;
        private readonly Service1 _signService = new Service1();

        public void Start()
        {
            if (_serverTask != null) return;
            _stopping = false;
            _serverTask = Task.Factory.StartNew(
                ServerLoop,
                System.Threading.CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        private void ServerLoop()
        {
            while (!_stopping)
            {
                try
                {
                    using (var pipe = new NamedPipeServerStream(
                        PipeName, PipeDirection.InOut, 1,
                        PipeTransmissionMode.Byte, PipeOptions.None))
                    {
                        pipe.WaitForConnection();
                        if (_stopping) return;
                        HandleClient(pipe);
                    }
                }
                catch (Exception ex)
                {
                    if (!_stopping) TryLog(ex);
                }
            }
        }

        private void HandleClient(NamedPipeServerStream pipe)
        {
            using (var reader = new StreamReader(pipe, new UTF8Encoding(false), false, 4096, true))
            using (var writer = new StreamWriter(pipe, new UTF8Encoding(false), 4096, true))
            {
                writer.AutoFlush = true;
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) return;

                PipeRequest request = null;
                PipeResponse response;
                try
                {
                    request = JsonConvert.DeserializeObject<PipeRequest>(line);
                    if (request == null || string.IsNullOrWhiteSpace(request.Action))
                        throw new InvalidOperationException("Named Pipe request is missing Action.");
                    response = Execute(request);
                }
                catch (Exception ex)
                {
                    TryLog(ex);
                    response = new PipeResponse
                    {
                        Id = request == null ? string.Empty : request.Id,
                        Success = false,
                        Error = ex.Message
                    };
                }
                writer.WriteLine(JsonConvert.SerializeObject(response));
            }
        }

        private PipeResponse Execute(PipeRequest request)
        {
            switch (request.Action)
            {
                case "Ping":
                    return Ok(request.Id, new
                    {
                        status = "OK",
                        phase = 4,
                        process = "DGISApp.exe",
                        transport = "Windows Named Pipe",
                        pipe = PipeName,
                        signService = "existing SignService.Service1",
                        wcfHttpHost = false,
                        allLegacyApiGroupsEnabled = true
                    });

                // Token APIs (Phase 2/3 regression compatibility)
                case "FetchPersID":
                    return Ok(request.Id, _signService.FetchPersID().GetAwaiter().GetResult());
                case "FetchTokenDetails":
                    return Ok(request.Id, _signService.FetchTokenDetails().GetAwaiter().GetResult());
                case "FetchUniqueTokenDetails":
                    return Ok(request.Id, _signService.FetchUniqueTokenDetails().GetAwaiter().GetResult());
                case "GetPublicKey":
                    return Ok(request.Id, _signService.GetPublicKey().GetAwaiter().GetResult());
                case "ValidatePersID":
                    return Ok(request.Id, _signService.ValidatePersID(RequiredStringPayload(request, "inputPersID")).GetAwaiter().GetResult());
                case "ValidatePersID2FA":
                    return Ok(request.Id, _signService.ValidatePersID2FA(RequiredStringPayload(request, "inputPersID")).GetAwaiter().GetResult());
                case "FetchTokenOCSPCrlDetails":
                    {
                        var data = RequiredPayload<CrlOcspRequest>(request, "CRL/OCSP request");
                        return Ok(request.Id, _signService.FetchTokenOCSPCrlDetails(data.IsCheckCrl, data.ThumbPrint ?? string.Empty).GetAwaiter().GetResult());
                    }

                case "FetchTokenOCSPDetails":
                    {
                        var data = RequiredPayload<CrlOcspRequest>(request, "CRL/OCSP request");
                        return Ok(request.Id, _signService.FetchTokenOCSPDetailsAsync(data.ThumbPrint ?? string.Empty).GetAwaiter().GetResult());
                    }
                   
                case "FetchTokenCrlDetails":
                    {
                        var data = RequiredPayload<CrlOcspRequest>(request, "CRL/OCSP request");
                        return Ok(request.Id, _signService.FetchTokenCrlDetailsAsync(data.ThumbPrint ?? string.Empty).GetAwaiter().GetResult());
                    }
                // XML / hash APIs
                case "SignXml":
                    {
                        var xml = ParseXml(RequiredStringPayload(request, "xml"));
                        var result = _signService.SignXml(xml.DocumentElement).GetAwaiter().GetResult();
                        return Ok(request.Id, result == null ? null : result.OuterXml);
                    }
                case "VerifySignXml":
                    {
                        var xml = ParseXml(RequiredStringPayload(request, "xml"));
                        return Ok(request.Id, _signService.VerifySignXml(xml.DocumentElement));
                    }
                case "SignHash":
                    return Ok(request.Id, _signService.SignHash(RequiredStringPayload(request, "rData")));

                // PDF signing / verification APIs
                case "DigitalSignAsync":
                    return Ok(request.Id, _signService.DigitalSignAsync(RequiredPayload<List<DigitalSignData>>(request, "DigitalSignData list")).GetAwaiter().GetResult());
                case "DigitalSignBulkAsync":
                    return Ok(request.Id, _signService.DigitalSignBulkAsync(RequiredPayload<List<DigitalSignData>>(request, "DigitalSignData list")).GetAwaiter().GetResult());
                case "ByteDigitalSignAsync":
                    return Ok(request.Id, _signService.ByteDigitalSignAsync(RequiredPayload<List<DigitalSignData>>(request, "DigitalSignData list")).GetAwaiter().GetResult());
                case "DigitalSignVerifyAsync":
                    return Ok(request.Id, _signService.DigitalSignVerifyAsync(RequiredPayload<DigitalSignData>(request, "DigitalSignData")));
                case "Getpdffile":
                    return Ok(request.Id, _signService.Getpdffile());
                case "PdfCordinatefile":
                    return Ok(request.Id, _signService.PdfCordinatefile(RequiredPayload<DTOCustomSignCordinate>(request, "PDF coordinate request")));

                // Encryption / decryption / watermarking
                case "AsymmetricEncryption":
                    return Ok(request.Id, _signService.AsymmetricEncryption(RequiredPayload<List<AsymmetricEncryptionData>>(request, "asymmetric encryption list")).GetAwaiter().GetResult());
                case "AsymmetricDencryption":
                    return Ok(request.Id, _signService.AsymmetricDencryption(RequiredPayload<List<AsymmetricEncryptionData>>(request, "asymmetric decryption list")).GetAwaiter().GetResult());
                case "SymmetricEncryption":
                    return Ok(request.Id, _signService.SymmetricEncryption(RequiredPayload<SymmetricEncryptionData>(request, "symmetric encryption request")).GetAwaiter().GetResult());
                case "SymmetricDencryption":
                    return Ok(request.Id, _signService.SymmetricDencryption(RequiredPayload<SymmetricEncryptionData>(request, "symmetric decryption request")).GetAwaiter().GetResult());
                case "AddWaterMarks":
                    return Ok(request.Id, _signService.AddWaterMarks(RequiredPayload<DtoWaterMarkData>(request, "watermark request")).GetAwaiter().GetResult());

                case "HasInternetConnectionAsyncTest":
                    return Ok(request.Id, _signService.HasInternetConnectionAsyncTest().GetAwaiter().GetResult());

                default:
                    throw new NotSupportedException("Phase 4 does not expose action '" + request.Action + "'.");
            }
        }

        private static XmlDocument ParseXml(string value)
        {
            var doc = new XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            doc.LoadXml(value);
            if (doc.DocumentElement == null) throw new ArgumentException("XML document does not have a root element.");
            return doc;
        }

        private static string RequiredStringPayload(PipeRequest request, string fieldName)
        {
            var value = JsonConvert.DeserializeObject<string>(request.PayloadJson ?? "null");
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(fieldName + " is required.");
            return value;
        }

        private static T RequiredPayload<T>(PipeRequest request, string description) where T : class
        {
            var data = JsonConvert.DeserializeObject<T>(request.PayloadJson ?? "null");
            if (data == null) throw new ArgumentException(description + " is required.");
            return data;
        }

        private static PipeResponse Ok(string id, object value)
        {
            return new PipeResponse
            {
                Id = id,
                Success = true,
                DataJson = JsonConvert.SerializeObject(value)
            };
        }

        private static void TryLog(Exception ex)
        {
            try { ErrorLog.LogErrorToFile(ex); } catch { }
        }

        public void Dispose()
        {
            _stopping = true;
            try
            {
                using (var wakeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                    wakeClient.Connect(250);
            }
            catch { }
        }

        private sealed class CrlOcspRequest
        {
            public bool IsCheckCrl { get; set; }
            public string ThumbPrint { get; set; }
        }
        private sealed class PipeRequest
        {
            public string Id { get; set; }
            public string Action { get; set; }
            public string PayloadJson { get; set; }
        }
        private sealed class PipeResponse
        {
            public string Id { get; set; }
            public bool Success { get; set; }
            public string DataJson { get; set; }
            public string Error { get; set; }
        }
    }
}
