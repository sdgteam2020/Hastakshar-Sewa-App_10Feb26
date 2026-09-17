using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace SignService.DTOs
{
    public class PublicKeyDTO
    {
        [DataMember]
        public string Public_Key { get; set; }
        [DataMember]
        public String Status { get; set; }
        [DataMember]
        public String Remarks { get; set; }
    }
    
}