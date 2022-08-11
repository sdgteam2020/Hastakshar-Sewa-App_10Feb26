using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SignService.Helpers
{
    public static class HugeFileSignatureService
    {
        private const int BufferSize = 16 * 1024 * 1024; 
        private const int ParallelChunks = 8;
 
        public static async Task<(string FilePath, string hash)> SignPortableAsync(
            string filePath,
            X509Certificate2 cert,
            Action<double> progress,
            string description = "Signature for file")
        {
            using (var rsa = cert.GetRSAPrivateKey())
            {
                if (rsa == null) throw new InvalidOperationException("Certificate is not RSA.");

                var totalBytes = new FileInfo(filePath).Length;
                 
                var hash = await HashFileInParallel(filePath, progress, totalBytes, (chunkHashes) =>
                {
                    using (var sha256 = SHA256.Create())
                    {
                        foreach (var chunkHash in chunkHashes)
                        {
                            sha256.TransformBlock(chunkHash, 0, chunkHash.Length, null, 0);
                        }
                        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                        return sha256.Hash;
                    }
                });
                 
                var signature = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                var chain = BuildCertChain(cert);
                 
                var sigObj = new PortableSig
                {
                    FileName = Path.GetFileName(filePath),
                    FileLength = totalBytes,
                    HashAlgorithm = "SHA-256",
                    HashHex = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant(),
                    SignatureAlgorithm = "RSA-PKCS1-v1_5",
                    SignatureBase64 = Convert.ToBase64String(signature),
                    CertificateChainBase64 = chain,
                    Description = description,
                    SignedBy = cert.Subject,
                    SigningDate = DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss tt")
                    
                };
                var MacResult = await new Service1().GetMacAddress();
                if (MacResult.Status == true)
                {
                    sigObj.MacAddress = MacResult.MacAddress.ToString();
                }
                string HashValue = sigObj.HashHex;
               // var sign = JsonConvert.SerializeObject(sigObj, Formatting.Indented);
                var outPath = filePath + ".sig.json";

                await Task.Run(() => File.WriteAllText(
                    outPath,
                    JsonConvert.SerializeObject(sigObj, Formatting.Indented),
                    Encoding.UTF8));

                progress?.Invoke(100);  
                return (outPath, HashValue);
            }
        }
         
        private static async Task<byte[]> HashFileInParallel(string filePath, Action<double> progress, long totalBytes, Func<IEnumerable<byte[]>, byte[]> combineHashes)
        {
            var tasks = new List<Task<byte[]>>();
            var chunkSize = totalBytes / ParallelChunks;

            for (int i = 0; i < ParallelChunks; i++)
            {
                var offset = i * chunkSize;
                var size = (i == ParallelChunks - 1) ? totalBytes - offset : chunkSize;

                tasks.Add(Task.Run(() => HashFileChunk(filePath, offset, size, progress, totalBytes)));
            }

            var chunkHashes = await Task.WhenAll(tasks);
            return combineHashes(chunkHashes);
        }
         
        private static byte[] HashFileChunk(string filePath, long offset, long size, Action<double> progress, long totalBytes)
        {
            using (var sha256 = SHA256.Create())
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize))
            {
                fileStream.Seek(offset, SeekOrigin.Begin);
                byte[] buffer = new byte[BufferSize];
                long bytesRead = 0;

                int bytesToRead;
                while ((bytesToRead = fileStream.Read(buffer, 0, (int)Math.Min(BufferSize, size - bytesRead))) > 0)
                {
                    sha256.TransformBlock(buffer, 0, bytesToRead, null, 0);
                    bytesRead += bytesToRead;
                     
                    progress?.Invoke((double)(offset + bytesRead) / totalBytes * 100);
                }

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                return sha256.Hash;
            }
        }
 
        public static async Task<bool> VerifyPortableAsync(string filePath, string sigJsonPath, Action<double> progress,string hashValue=null)
        {
            try
            {
                var sigObj = JsonConvert.DeserializeObject<PortableSig>(await Task.Run(() => File.ReadAllText(sigJsonPath, Encoding.UTF8)));

                if (!string.Equals(sigObj.HashAlgorithm, "SHA-256", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("Only SHA-256 supported.");
                if (!string.Equals(sigObj.SignatureAlgorithm, "RSA-PKCS1-v1_5", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("Only RSA PKCS#1 v1.5 supported.");

                using (var rsa = new X509Certificate2(Convert.FromBase64String(sigObj.CertificateChainBase64[0])).GetRSAPublicKey())
                {
                    var totalBytes = new FileInfo(filePath).Length;

                    var hash = await HashFileInParallel(filePath, progress, totalBytes, (chunkHashes) =>
                    {
                        using (var sha256 = SHA256.Create())
                        {
                            foreach (var chunkHash in chunkHashes)
                            {
                                sha256.TransformBlock(chunkHash, 0, chunkHash.Length, null, 0);
                            }
                            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                            return sha256.Hash;
                        }
                    });

                    bool result = rsa.VerifyHash(hash, Convert.FromBase64String(sigObj.SignatureBase64), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                    if (result)
                    {
                        if (hashValue != null)
                        {
                            if (hashValue == sigObj.HashHex)
                                return true;
                        }
                        return false;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex) 
            {
                return false;
            }
            
        }

        private static string[] BuildCertChain(X509Certificate2 leaf)
        {
            using (var chain = new X509Chain())
            {
                chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;  
                chain.Build(leaf);
                var list = new List<string>();
                foreach (var element in chain.ChainElements)
                    list.Add(Convert.ToBase64String(element.Certificate.Export(X509ContentType.Cert)));
                return list.ToArray();
            }
        }

        private class PortableSig
        {
            public string FileName { get; set; }
            public long FileLength { get; set; }
            public string HashAlgorithm { get; set; }
            public string HashHex { get; set; }
            public string SignatureAlgorithm { get; set; }
            public string SignatureBase64 { get; set; }
            public string[] CertificateChainBase64 { get; set; }
            public string Description { get; set; }
            public string SignedBy { get; set; }
            public string SigningDate { get; set; }
            public string MacAddress { get; set; }
        }
    }
}