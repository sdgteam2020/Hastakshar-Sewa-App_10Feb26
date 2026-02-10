using iText.Signatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using WinniesMessageBox;

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
                   // throw new ArgumentException("Unknown encryption algorithm " + certificate.PrivateKey);
            }
            catch(Exception)
            {
                message = "Token not found";
                //MyMessageBox.ShowDialog("Token not found!");
            }
        }

        public virtual byte[] Sign(byte[] message)
        {
            try
            {
                if (certificate.PrivateKey is RSACryptoServiceProvider)
                {
                    RSACryptoServiceProvider rsa = (RSACryptoServiceProvider)certificate.PrivateKey;
                    return rsa.SignData(message, hashAlgorithm);
                }
                else
                {
                    DSACryptoServiceProvider dsa = (DSACryptoServiceProvider)certificate.PrivateKey;
                    return dsa.SignData(message);
                }
            }
            catch(Exception )
            {
                return null;
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
