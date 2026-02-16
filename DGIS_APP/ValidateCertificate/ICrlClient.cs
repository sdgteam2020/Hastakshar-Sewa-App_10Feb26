using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ValidateCertificate
{
    public interface ICrlClient
    {
        
        ICollection<byte[]> GetEncoded(X509Certificate checkCert, String url);
    }
}
