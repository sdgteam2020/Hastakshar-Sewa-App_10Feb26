using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace DGISApp
{
    public interface ICrlClient
    {
        
        ICollection<byte[]> GetEncoded(X509Certificate checkCert, String url);
    }
}
