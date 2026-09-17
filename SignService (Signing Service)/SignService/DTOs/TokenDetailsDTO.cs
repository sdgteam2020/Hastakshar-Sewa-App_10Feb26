using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace SignService.DTOs
{
    public class TokenDetailsDTO
    {

        [DataMember]
        public String subject { get; set; }
        [DataMember]
        public String issuer { get; set; }
        [DataMember]
        public String Thumbprint { get; set; }
        [DataMember]
        public String ValidFrom { get; set; }
        [DataMember]
        public String ValidTo { get; set; }
        [DataMember]
        public String Status { get; set; }
        [DataMember]
        public String Remarks { get; set; }
        [DataMember]
        public Boolean TokenValid { get; set; }
       
    }
}