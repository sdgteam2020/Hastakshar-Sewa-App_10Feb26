using iText.Signatures;
using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace MyApp
{
    public class X509Certificate2Signature : IExternalSignature
    {
        private String hashAlgorithm;
        private String encryptionAlgorithm;
        private X509Certificate2 certificate;

        public X509Certificate2Signature(X509Certificate2 certificate, String hashAlgorithm, ref string message)
        {
            message = null;
            try
            {
                if (!certificate.HasPrivateKey)
                    throw new ArgumentException("No private key.");
                this.certificate = certificate;
                this.hashAlgorithm = DigestAlgorithms.GetDigest(DigestAlgorithms.GetAllowedDigest(hashAlgorithm));
                if (certificate.PrivateKey is RSACryptoServiceProvider)
                    encryptionAlgorithm = "RSA";
                else if (certificate.PrivateKey is DSACryptoServiceProvider)
                    encryptionAlgorithm = "DSA";
                else
                    message = "certificate not valid"; 
            }
            catch(Exception)
            {
                message = "Token not found"; 
            }
        }

        public virtual byte[] Sign(byte[] message)
        {
            if (certificate == null)
                throw new CryptographicException(
                    "Signing certificate is null.");

            if (message == null || message.Length == 0)
                throw new ArgumentException(
                    "Signing data is empty.", nameof(message));

            using (RSA rsa = certificate.GetRSAPrivateKey())
            {
                if (rsa == null)
                    throw new CryptographicException(
                        "RSA private key is unavailable. Check DSC token.");

                byte[] signature = rsa.SignData(
                    message,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );

                if (signature == null || signature.Length == 0)
                    throw new CryptographicException(
                        "DSC signing returned empty signature.");

                return signature;
            }
        }

        public virtual String GetHashAlgorithm()
        {
            return hashAlgorithm;
        }

        public virtual String GetEncryptionAlgorithm()
        {
            return encryptionAlgorithm;
        }
    }

}
