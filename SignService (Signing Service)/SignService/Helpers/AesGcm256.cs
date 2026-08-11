using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Text;

namespace SignService.Helpers
{
    public class AesGcm256
    {
        private static readonly SecureRandom Random = new SecureRandom();
        string download = Environment.GetEnvironmentVariable("USERPROFILE") + @"\" + "Downloads";
         
        public static readonly int NonceBitSize = 128;
        public static readonly int MacBitSize = 128;
        public static readonly int KeyBitSize = 256;
         
        public static readonly int SaltBitSize = 128;
        public static readonly int Iterations = 10000;
        public static readonly int MinPasswordLength = 04;

        private AesGcm256() { }

        public static byte[] NewKey()
        {
            var key = new byte[KeyBitSize / 8];
            Random.NextBytes(key);
            return key;
        }

        public static byte[] NewIv()
        {
            var iv = new byte[NonceBitSize / 8];
            Random.NextBytes(iv);
            return iv;
        }

        public static Byte[] HexToByte(string hexStr)
        {
            byte[] bArray = new byte[hexStr.Length / 2];
            for (int i = 0; i < (hexStr.Length / 2); i++)
            {
                byte firstNibble = Byte.Parse(hexStr.Substring((2 * i), 1),
                                   System.Globalization.NumberStyles.HexNumber);  
                byte secondNibble = Byte.Parse(hexStr.Substring((2 * i) + 1, 1),
                                    System.Globalization.NumberStyles.HexNumber);
                int finalByte = (secondNibble) | (firstNibble << 4);   
                bArray[i] = (byte)finalByte;
            }
            return bArray;
        }

        public static string toHex(byte[] data)
        {
            string hex = string.Empty;
            foreach (byte c in data)
            {
                hex += c.ToString("X2");
            }
            return hex;
        }

        public static string toHex(string asciiString)
        {
            string hex = string.Empty;
            foreach (char c in asciiString)
            {
                int tmp = c;
                hex += string.Format("{0:x2}", System.Convert.ToUInt32(tmp.ToString()));
            }
            return hex;
        }




        public static byte[] SimpleEncrypt(byte[] secretMessage, byte[] key, byte[] nonSecretPayload = null)
        { 
            if (key == null || key.Length != KeyBitSize / 8)
                throw new ArgumentException(String.Format("Key needs to be {0} bit!", KeyBitSize), "key");

            if (secretMessage == null || secretMessage.Length == 0)
                throw new ArgumentException("Secret Message Required!", "secretMessage");
             
            nonSecretPayload = nonSecretPayload ?? new byte[] { };
             
            var nonce = new byte[NonceBitSize / 8];
            Random.NextBytes(nonce, 0, nonce.Length);

            var cipher = new GcmBlockCipher(new AesFastEngine());
            var parameters = new AeadParameters(new KeyParameter(key), MacBitSize, nonce, nonSecretPayload);
            cipher.Init(true, parameters);
             
            var cipherText = new byte[cipher.GetOutputSize(secretMessage.Length)];
            var len = cipher.ProcessBytes(secretMessage, 0, secretMessage.Length, cipherText, 0);
            cipher.DoFinal(cipherText, len);
             
            using (var combinedStream = new MemoryStream())
            {
                using (var binaryWriter = new BinaryWriter(combinedStream))
                { 
                    binaryWriter.Write(nonSecretPayload); 
                    binaryWriter.Write(nonce); 
                    binaryWriter.Write(cipherText);
                }
                return combinedStream.ToArray();
            }
        }

        public static byte[] SimpleDecrypt(byte[] encryptedMessage, byte[] key, int nonSecretPayloadLength = 0)
        { 
            if (key == null || key.Length != KeyBitSize / 8)
                throw new ArgumentException(String.Format("Key needs to be {0} bit!", KeyBitSize), "key");

            if (encryptedMessage == null || encryptedMessage.Length == 0)
                throw new ArgumentException("Encrypted Message Required!", "encryptedMessage");

            using (var cipherStream = new MemoryStream(encryptedMessage))
            using (var cipherReader = new BinaryReader(cipherStream))
            { 
                var nonSecretPayload = cipherReader.ReadBytes(nonSecretPayloadLength);
                 
                var nonce = cipherReader.ReadBytes(NonceBitSize / 8);

                var cipher = new GcmBlockCipher(new AesFastEngine());
                var parameters = new AeadParameters(new KeyParameter(key), MacBitSize, nonce, nonSecretPayload);
                cipher.Init(false, parameters);
                 
                var cipherText = cipherReader.ReadBytes(encryptedMessage.Length - nonSecretPayloadLength - nonce.Length);
                var plainText = new byte[cipher.GetOutputSize(cipherText.Length)];

                try
                {
                    var len = cipher.ProcessBytes(cipherText, 0, cipherText.Length, plainText, 0);
                    cipher.DoFinal(plainText, len);

                }
                catch (InvalidCipherTextException)
                {
                    return null;
                }

                return plainText;
            }

        }

        public static byte[] SimpleEncryptWithPassword(byte[] secretMessage, string password, byte[] nonSecretPayload = null)
        {
            nonSecretPayload = nonSecretPayload ?? new byte[] { };

            if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
                throw new ArgumentException(String.Format("Must have a password of at least {0} characters!", MinPasswordLength), "password");

            if (secretMessage == null || secretMessage.Length == 0)
                throw new ArgumentException("Secret Message Required!", "secretMessage");

            var generator = new Pkcs5S2ParametersGenerator();

            var salt = new byte[SaltBitSize / 8];
            Random.NextBytes(salt);

            generator.Init(
              PbeParametersGenerator.Pkcs5PasswordToBytes(password.ToCharArray()),
              salt,
              Iterations);
            var key = (KeyParameter)generator.GenerateDerivedMacParameters(KeyBitSize);

            var payload = new byte[salt.Length + nonSecretPayload.Length];
            Array.Copy(nonSecretPayload, payload, nonSecretPayload.Length);
            Array.Copy(salt, 0, payload, nonSecretPayload.Length, salt.Length);

            return SimpleEncrypt(secretMessage, key.GetKey(), payload);
        }
        public static byte[] SimpleEncryptWithPasswordForSecureFile(byte[] secretMessage, string password, string macAddress = null, byte[] nonSecretPayload = null)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
                throw new ArgumentException(
                    $"Must have a password of at least {MinPasswordLength} characters!",
                    nameof(password));

            if (secretMessage == null || secretMessage.Length == 0)
                throw new ArgumentException("Secret Message Required!", nameof(secretMessage));

            var generator = new Pkcs5S2ParametersGenerator();

            var salt = new byte[SaltBitSize / 8];
            Random.NextBytes(salt);

            generator.Init(
                PbeParametersGenerator.Pkcs5PasswordToBytes(password.ToCharArray()),
                salt,
                Iterations);

            var key = (KeyParameter)generator.GenerateDerivedMacParameters(KeyBitSize);

            // 1. Create metadata
            string metadata = macAddress;

            byte[] metadataBytes =Encoding.UTF8.GetBytes(metadata);



            // 2. Encrypt metadata using same key
            byte[] encryptedMetadata =SimpleEncrypt(metadataBytes,key.GetKey(), salt);

            // 3. Store encrypted metadata length
            byte[] metadataLength =
                BitConverter.GetBytes(encryptedMetadata.Length);

            // 4. Create payload
            byte[] payload =
                new byte[
                    metadataLength.Length +
                    encryptedMetadata.Length +
                    salt.Length];


            int offset = 0;

            Array.Copy(metadataLength,0,payload,offset,metadataLength.Length);

            offset += metadataLength.Length;


            Array.Copy(encryptedMetadata,0,payload,offset,encryptedMetadata.Length);

            offset += encryptedMetadata.Length;


            Array.Copy(salt,0,payload,offset,salt.Length);

            // 5. Encrypt file
            return SimpleEncrypt(
                secretMessage,
                key.GetKey(),
                payload);
            //// 🔹 Convert macAddress to bytes
            //byte[] macBytes = Encoding.UTF8.GetBytes(macAddress);
            //byte[] macLengthBytes = BitConverter.GetBytes(macBytes.Length);

            //// 🔹 Build payload
            //var payload = new byte[
            //    macLengthBytes.Length +
            //    macBytes.Length +
            //    salt.Length];

            //int offset = 0;

            //Array.Copy(macLengthBytes, 0, payload, offset, macLengthBytes.Length);
            //offset += macLengthBytes.Length;

            //Array.Copy(macBytes, 0, payload, offset, macBytes.Length);
            //offset += macBytes.Length;

            //Array.Copy(salt, 0, payload, offset, salt.Length);

            //return SimpleEncrypt(secretMessage, key.GetKey(), payload);
        }

        public static byte[] SimpleDecryptWithPassword(byte[] encryptedMessage, string password, int nonSecretPayloadLength = 0)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < MinPasswordLength)
                throw new ArgumentException(String.Format("Must have a password of at least {0} characters!", MinPasswordLength), "password");

            if (encryptedMessage == null || encryptedMessage.Length == 0)
                throw new ArgumentException("Encrypted Message Required!", "encryptedMessage");

            var generator = new Pkcs5S2ParametersGenerator();

            var salt = new byte[SaltBitSize / 8];
            Array.Copy(encryptedMessage, nonSecretPayloadLength, salt, 0, salt.Length);

            generator.Init(
              PbeParametersGenerator.Pkcs5PasswordToBytes(password.ToCharArray()),
              salt,
              Iterations);

            var key = (KeyParameter)generator.GenerateDerivedMacParameters(KeyBitSize);

            return SimpleDecrypt(encryptedMessage, key.GetKey(), salt.Length + nonSecretPayloadLength);
        }
        public static byte[] SimpleDecryptWithPasswordForSecureFile(byte[] encryptedMessage, string password, out string macAddress,int nonSecretPayloadLength = 0)
        {
            try
            {
                macAddress = null;

                int offset = 0;

                // 1. Read encrypted metadata length
                int encryptedMacLength =
                    BitConverter.ToInt32(encryptedMessage, offset);

                offset += 4;


                // 2. Read encrypted MAC bytes
                byte[] encryptedMacBytes =
                    new byte[encryptedMacLength];

                Array.Copy(
                    encryptedMessage,
                    offset,
                    encryptedMacBytes,
                    0,
                    encryptedMacLength);

                offset += encryptedMacLength;



                // 3. Read salt
                byte[] salt =
                    new byte[SaltBitSize / 8];

                Array.Copy(
                    encryptedMessage,
                    offset,
                    salt,
                    0,
                    salt.Length);



                // 4. Generate key
                var generator =
                    new Pkcs5S2ParametersGenerator();

                generator.Init(
                    PbeParametersGenerator.Pkcs5PasswordToBytes(password.ToCharArray()),
                    salt,
                    Iterations);


                var key =
                    (KeyParameter)
                    generator.GenerateDerivedMacParameters(KeyBitSize);



                // 5. Decrypt MAC
                byte[] decryptedMac =
                    SimpleDecrypt(
                        encryptedMacBytes,
                        key.GetKey(),
                        salt.Length);


                if (decryptedMac == null)
                    return null;


                macAddress =
                    Encoding.UTF8.GetString(decryptedMac);



                // 6. Full payload length
                int payloadLength =
                    4 +
                    encryptedMacLength +
                    salt.Length;



                // 7. Decrypt file
                return SimpleDecrypt(
                    encryptedMessage,
                    key.GetKey(),
                    payloadLength);

            
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
                macAddress = null;
                return null;
            }

        }
    }
}