using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace SignService.DTOs
{
    public class TokenDetailsCrlDTO
    {

        [DataMember]
        public Boolean CrlCheck { get; set; }
        [DataMember]
        public String CrlMsg { get; set; }
      
        [DataMember]
        public String Status { get; set; }
        [DataMember]
        public String Remarks { get; set; }
    }
}