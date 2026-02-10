using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DGISApp
{
   static class CheckCertificateStatus
    {

        public static string[] GetCrlDistributionPoints(this X509Certificate2 certificate)
        {
            X509Extension ext = certificate.Extensions.Cast<X509Extension>().FirstOrDefault(
                e => e.Oid.Value == "2.5.29.31");


            if (ext == null || ext.RawData == null || ext.RawData.Length < 11)
                return EmptyStrings;

            int prev = -2;
            List<string> items = new List<string>();
            while (prev != -1 && ext.RawData.Length > prev + 1)
            {
                int next = IndexOf(ext.RawData, 0x86, prev == -2 ? 8 : prev + 1);
                if (next == -1)
                {
                    if (prev >= 0)
                    {
                        string item = Encoding.UTF8.GetString(ext.RawData, prev + 2, ext.RawData.Length - (prev + 2));
                        items.Add(item);
                    }

                    break;
                }

                if (prev >= 0 && next > prev)
                {
                    string item = Encoding.UTF8.GetString(ext.RawData, prev + 2, next - (prev + 2));
                    items.Add(item);
                }

                prev = next;
            }

            return items.ToArray();
        }

        static int IndexOf(byte[] instance, byte item, int start)
        {
            for (int i = start, l = instance.Length; i < l; i++)
                if (instance[i] == item)
                    return i;

            return -1;
        }

        static string[] EmptyStrings = new string[0];

        public static X509Certificate2 GetIssuer(X509Certificate2 leafCert)
        {
            if (leafCert.Subject == leafCert.Issuer) { return leafCert; }
            X509Chain chain = new X509Chain();
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.Build(leafCert);
            X509Certificate2 issuer = null;
            if (chain.ChainElements.Count > 1)
            {
                issuer = chain.ChainElements[1].Certificate;
            }
            chain.Reset();
            return issuer;
        }

        public static X509Crl GetCrl(String crlurl)
        {
            try
            {
                if (crlurl == null)
                    return null;
                Console.WriteLine("Getting CRL from " + crlurl);

                X509CrlParser crlParser = new X509CrlParser();
                // Creates the CRL
                Stream url = WebRequest.Create(crlurl).GetResponse().GetResponseStream();
                return crlParser.ReadCrl(url);
            }
            catch (IOException)
            {
                return null;
            }
            catch (GeneralSecurityException)
            {
                return null;
            }
            catch (WebException)
            {
                return null;
            }
        }

        public static bool IsCRLOK(X509Crl x509crl, Org.BouncyCastle.X509.X509Certificate issuerCertificate, DateTime
              validationDate)
        {
            try
            {
                if (issuerCertificate == null)
                {
                    throw new ArgumentNullException("Must provide a issuer certificate to validate the signature"
                        );
                }
                if (!x509crl.IssuerDN.Equals(issuerCertificate.SubjectDN))
                {
                    Console.WriteLine("The CRL must be signed by the issuer (" + issuerCertificate.SubjectDN
                        + " ) but instead is signed by " + x509crl.IssuerDN);
                    return false;
                }
                try
                {
                    x509crl.Verify(issuerCertificate.GetPublicKey());
                }
                catch (Exception e)
                {
                    Console.WriteLine("The signature verification for CRL cannot be performed : " + e.Message
                        );
                    return false;
                }
                DateTime thisUpdate = x509crl.ThisUpdate;
                Console.WriteLine("validation date: " + validationDate);
                Console.WriteLine("CRL this update: " + thisUpdate);
                Console.WriteLine("CRL next update: " + x509crl.NextUpdate);
                if (x509crl.NextUpdate != null && validationDate.CompareTo(x509crl.NextUpdate.Value) > 0) //jbonilla After
                {
                    Console.WriteLine("CRL too old");
                    return false;
                }
                // assert cRLSign KeyUsage bit
                if (null == issuerCertificate.GetKeyUsage())
                {
                    Console.WriteLine("No KeyUsage extension for CRL issuing certificate");
                    return false;
                }
                if (false == issuerCertificate.GetKeyUsage()[6])
                {
                    Console.WriteLine("cRLSign bit not set for CRL issuing certificate");
                    return false;
                }
                return true;
            }catch(Exception)
            {
                return false;
            }
        }

       
        static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }

    }
}
