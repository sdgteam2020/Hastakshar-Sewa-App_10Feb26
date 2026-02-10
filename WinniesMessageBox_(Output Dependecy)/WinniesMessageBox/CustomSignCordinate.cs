using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinniesMessageBox
{
    public static class CustomSignCordinate
    {
        public static int X { get; set; }
        public static int Y { get; set; }
        public static int PageNo { get; set; }
        public static string PdfFile { get; set; }
        public static DateTime UpdatedOn { get; set; }
    }
    public class DTOCustomSignCordinate
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int PageNo { get; set; }
        public string PdfFile { get; set; }
        
    }
}
