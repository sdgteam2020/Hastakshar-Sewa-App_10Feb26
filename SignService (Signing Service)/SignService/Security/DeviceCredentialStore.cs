using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SignService.Security
{
    public static class DeviceCredentialStore
    {
        private static readonly string FilePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                         "DGIS", "device.dat");  

        public static void Save(string domainId, string ipAddress, string clientKey)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            string plain = $"{domainId}\n{ipAddress}\n{clientKey}";
            byte[] bytes = Encoding.UTF8.GetBytes(plain);

          
            byte[] encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);

            File.WriteAllBytes(FilePath, encrypted);
        }

        public static (string DomainId, string IPAddress, string ClientKey)? Load()
        {
            if (!File.Exists(FilePath))
                return null;

            byte[] encrypted = File.ReadAllBytes(FilePath);

             
            byte[] bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.LocalMachine);

            string plain = Encoding.UTF8.GetString(bytes);
            var parts = plain.Split(new[] { '\n' }, 3);

            if (parts.Length < 3)
                return null;

            return (parts[0].Trim(), parts[1].Trim(), parts.Length > 2 ? parts[2]?.Trim() : null);
        }

        public static (string DomainId,string IPAddress, string ClientKey) GetOrCreate()
        {
            var existing = Load();
            if(existing == null)
            {
                string domainId = Environment.MachineName;
                string ipAddress = Service1.GetClientIpAddressSafe();
                string clientKey = Guid.NewGuid().ToString("N");

                Save(domainId, ipAddress, clientKey);
                return (domainId, ipAddress, clientKey);
            }
            else if (existing != null
                && (string.IsNullOrEmpty(existing.Value.DomainId)
                || string.IsNullOrEmpty(existing.Value.IPAddress)
                || string.IsNullOrEmpty(existing.Value.ClientKey)))
            {
                Delete();
                string domainId = Environment.MachineName;
                string ipAddress = Service1.GetClientIpAddressSafe();
                string clientKey = Guid.NewGuid().ToString("N");

                Save(domainId, ipAddress, clientKey);
                return (domainId, ipAddress, clientKey);
            }
            else
            {
                return existing.Value;
            }

           
        }

        public static bool Exists() => File.Exists(FilePath);

        public static void Delete()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
    }
}
