using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Ocsp;
using Org.BouncyCastle.X509;

namespace ValidateCertificate
{
    public enum CertificateStatus { Good = 0, Revoked = 1, Unknown = 2, NotFound = 3 };

    class OcspClient
    {

        public readonly int BufferSize = 4096 * 8;
        private readonly int MaxClockSkew = 36000000;

        public CertificateStatus Query(X509Certificate eeCert, X509Certificate issuerCert)
        {
            // Query the first Ocsp Url found in certificate
            List<string> urls = GetAuthorityInformationAccessOcspUrl(eeCert);



            if (urls.Count == 0)
            {
                Console.WriteLine("No OCSP url found in ee certificate.");
                //return;
                return CertificateStatus.NotFound;
            }
            else
            {

                string url = urls[0];

                Console.WriteLine("Querying '" + url + "'...");

                OcspReq req = GenerateOcspRequest(issuerCert, eeCert.SerialNumber);

                byte[] binaryResp = PostData(url, req.GetEncoded(), "application/ocsp-request", "application/ocsp-response");

                return ProcessOcspResponse(eeCert, issuerCert, binaryResp);
            }
        }

        public byte[] PostData(string url, byte[] data, string contentType, string accept)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "POST";
                request.ContentType = contentType;
                request.ContentLength = data.Length;
                request.Accept = accept;
                //request.Timeout = 15000; 
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream respStream = response.GetResponseStream())
                {
                    byte[] resp = ToByteArray(respStream);
                    return resp;
                }
            }
            catch (WebException)
            {
                return null;
            }
        }

        public byte[] ToByteArray(Stream stream)
        {
            byte[] buffer = new byte[BufferSize];
            MemoryStream ms = new MemoryStream();

            int read = 0;

            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }

            return ms.ToArray();
        }


        public static List<string> GetAuthorityInformationAccessOcspUrl(X509Certificate cert)
        {
            List<string> ocspUrls = new List<string>();

            try
            {
                Asn1Object obj = GetExtensionValue(cert, X509Extensions.AuthorityInfoAccess.Id);

                if (obj == null)
                {
                    return null;
                }

                // For a strange reason I cannot acess the aia.AccessDescription[].
                // Hope it will be fixed in the next version (1.5).
                // AuthorityInformationAccess aia = AuthorityInformationAccess.GetInstance(obj);

                // Switched to manual parse
                Asn1Sequence s = (Asn1Sequence)obj;
                IEnumerator elements = s.GetEnumerator();

                while (elements.MoveNext())
                {
                    Asn1Sequence element = (Asn1Sequence)elements.Current;
                    DerObjectIdentifier oid = (DerObjectIdentifier)element[0];

                    if (oid.Id.Equals("1.3.6.1.5.5.7.48.1")) // Is Ocsp?
                    {
                        Asn1TaggedObject taggedObject = (Asn1TaggedObject)element[1];
                        GeneralName gn = (GeneralName)GeneralName.GetInstance(taggedObject);
                        ocspUrls.Add(((DerIA5String)DerIA5String.GetInstance(gn.Name)).GetString());
                    }
                }
            }
            catch (Exception e)
            {
                //throw new Exception("Error parsing AIA.", e);
                Console.WriteLine("Error parsing AIA." + e.Message);
            }

            return ocspUrls;
        }

        protected static Asn1Object GetExtensionValue(X509Certificate cert,
                string oid)
        {
            if (cert == null)
            {
                return null;
            }

            byte[] bytes = cert.GetExtensionValue(new DerObjectIdentifier(oid)).GetOctets();

            if (bytes == null)
            {
                return null;
            }

            Asn1InputStream aIn = new Asn1InputStream(bytes);

            return aIn.ReadObject();
        }


        private CertificateStatus ProcessOcspResponse(X509Certificate eeCert, X509Certificate issuerCert, byte[] binaryResp)
        {
            try
            {
                OcspResp r = new OcspResp(binaryResp);
                CertificateStatus cStatus = CertificateStatus.Unknown;

                switch (r.Status)
                {
                    case OcspRespStatus.Successful:
                        BasicOcspResp or = (BasicOcspResp)r.GetResponseObject();

                        if (or.Responses.Length == 1)
                        {
                            SingleResp resp = or.Responses[0];

                            ValidateCertificateId(issuerCert, eeCert, resp.GetCertID());

                            Object certificateStatus = resp.GetCertStatus();

                            if (certificateStatus == Org.BouncyCastle.Ocsp.CertificateStatus.Good)
                            {
                                cStatus = CertificateStatus.Good;
                            }
                            else if (certificateStatus is Org.BouncyCastle.Ocsp.RevokedStatus)
                            {
                                cStatus = CertificateStatus.Revoked;
                            }
                            else if (certificateStatus is Org.BouncyCastle.Ocsp.UnknownStatus)
                            {
                                cStatus = CertificateStatus.Unknown;
                            }
                        }
                        break;
                    default:
                        throw new Exception("Unknow status '" + r.Status + "'.");
                }

                return cStatus;
            }
            catch (ArgumentNullException)
            {
                return CertificateStatus.NotFound;
            }
        }

        private void ValidateResponse(BasicOcspResp or, X509Certificate issuerCert)
        {
            ValidateResponseSignature(or, issuerCert.GetPublicKey());
            ValidateSignerAuthorization(issuerCert, or.GetCerts()[0]);
        }

        private void ValidateSignerAuthorization(X509Certificate issuerCert, X509Certificate signerCert)
        {
            if (!(issuerCert.IssuerDN.Equivalent(signerCert.IssuerDN) && issuerCert.SerialNumber.Equals(signerCert.SerialNumber)))
            {
                throw new Exception("Invalid OCSP signer");
            }
        }

        private void ValidateResponseSignature(BasicOcspResp or, Org.BouncyCastle.Crypto.AsymmetricKeyParameter asymmetricKeyParameter)
        {
            if (!or.Verify(asymmetricKeyParameter))
            {
                throw new Exception("Invalid OCSP signature");
            }
        }

        private void ValidateNextUpdate(SingleResp resp)
        {
            if (resp.NextUpdate != null && resp.NextUpdate.Value != null && resp.NextUpdate.Value.Ticks <= DateTime.Now.Ticks)
            {
                throw new Exception("Invalid next update.");
            }
        }

        private void ValidateThisUpdate(SingleResp resp)
        {
            if (Math.Abs(resp.ThisUpdate.Ticks - DateTime.Now.Ticks) > MaxClockSkew)
            {
                throw new Exception("Max clock skew reached.");
            }
        }

        private void ValidateCertificateId(X509Certificate issuerCert, X509Certificate eeCert, CertificateID certificateId)
        {
            CertificateID expectedId = new CertificateID(CertificateID.HashSha1, issuerCert, eeCert.SerialNumber);

            if (!expectedId.SerialNumber.Equals(certificateId.SerialNumber))
            {
                throw new Exception("Invalid certificate ID in response");
            }

            if (!Org.BouncyCastle.Utilities.Arrays.AreEqual(expectedId.GetIssuerNameHash(), certificateId.GetIssuerNameHash()))
            {
                throw new Exception("Invalid certificate Issuer in response");
            }

        }

        private OcspReq GenerateOcspRequest(X509Certificate issuerCert, BigInteger serialNumber)
        {
            CertificateID id = new CertificateID(CertificateID.HashSha1, issuerCert, serialNumber);
            return GenerateOcspRequest(id);
        }

        private OcspReq GenerateOcspRequest(CertificateID id)
        {
            OcspReqGenerator ocspRequestGenerator = new OcspReqGenerator();

            ocspRequestGenerator.AddRequest(id);

            BigInteger nonce = BigInteger.ValueOf(new DateTime().Ticks);

            ArrayList oids = new ArrayList();
            Hashtable values = new Hashtable();

            oids.Add(OcspObjectIdentifiers.PkixOcsp);

            Asn1OctetString asn1 = new DerOctetString(new DerOctetString(new byte[] { 1, 3, 6, 1, 5, 5, 7, 48, 1, 1 }));

            values.Add(OcspObjectIdentifiers.PkixOcsp, new X509Extension(false, asn1));
#pragma warning disable CS0612 // Type or member is obsolete
            ocspRequestGenerator.SetRequestExtensions(new X509Extensions(oids, values));
#pragma warning restore CS0612 // Type or member is obsolete

            return ocspRequestGenerator.Generate();
        }
    }
}
