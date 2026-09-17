using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace SignService.DTOs
{
    public class TokenDetailsOcspDTO
    {

        [DataMember]
        public Boolean OCSPCheck { get; set; }
        [DataMember]
        public String OCSPMsg { get; set; }

        [DataMember]
        public String Status { get; set; }
        [DataMember]
        public String Remarks { get; set; }
        
    }
}