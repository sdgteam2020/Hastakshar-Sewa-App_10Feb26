using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DGISApp
{
    public interface ICrlClient
    {
        /**
         * Gets a collection of byte array each representing a crl.
         * @param	checkCert	the certificate from which a CRL URL can be obtained
         * @param	url		a CRL url if you don't want to obtain it from the certificate
         * @return	a collection of byte array each representing a crl. It may return null or an empty collection
         */
        ICollection<byte[]> GetEncoded(X509Certificate checkCert, String url);
    }
}
