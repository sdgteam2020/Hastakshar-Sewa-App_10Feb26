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

        public static void Save(string deviceId, string deviceKey)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));

            string plain = $"{deviceId}\n{deviceKey}";
            byte[] bytes = Encoding.UTF8.GetBytes(plain);

          
            byte[] encrypted = ProtectedData.Protect(bytes, null, DataProtectionScope.LocalMachine);

            File.WriteAllBytes(FilePath, encrypted);
        }

        public static (string DeviceId, string DeviceKey)? Load()
        {
            if (!File.Exists(FilePath))
                return null;

            byte[] encrypted = File.ReadAllBytes(FilePath);

             
            byte[] bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.LocalMachine);

            string plain = Encoding.UTF8.GetString(bytes);
            var parts = plain.Split(new[] { '\n' }, 2);

            if (parts.Length < 2)
                return null;

            return (parts[0].Trim(), parts[1].Trim());
        }

        public static (string DeviceId, string DeviceKey) GetOrCreate()
        {
            var existing = Load();
            if (existing != null)
                return existing.Value;

            string deviceId = Environment.MachineName;
            string deviceKey = Guid.NewGuid().ToString("N"); 

            Save(deviceId, deviceKey);
            return (deviceId, deviceKey);
        }

        public static bool Exists() => File.Exists(FilePath);

        public static void Delete()
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
    }
}
