using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace SignService.DTOs
{
    public class PersDTO
    {
        [DataMember]
        public String subject { get; set; }
    
        public String Status { get; set; }
        [DataMember]
        public String Remarks { get; set; }
      
    }
}