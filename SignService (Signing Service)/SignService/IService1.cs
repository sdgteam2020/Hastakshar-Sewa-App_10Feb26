
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Threading.Tasks;
using System.Xml;
using WinniesMessageBox;

namespace SignService
{ 
    [ServiceContract]
    public interface IService1
    {

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/SignXml", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml)]

        Task<XmlElement> SignXml(XmlElement data);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/VerifySignXml", BodyStyle = WebMessageBodyStyle.Bare, RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Json)]

        List<DigitalVerifyDetails> VerifySignXml(XmlElement data);

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/GetPublicKey")]
        Task<TokenDetails> GetPublicKey();

        [OperationContract]
        
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/FetchPersID")]
        Task<List<TokenDetails>> FetchPersID();
       
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/FetchUniqueTokenDetails")]
        Task<List<TokenDetails>> FetchUniqueTokenDetails();
       
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/FetchTokenDetails")]
        Task<List<TokenDetails>> FetchTokenDetails();

       
        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/ValidatePersID", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<List<PersIdValidation>> ValidatePersID(string inputPersID);

        
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/FetchTokenOCSPCrlDetails?IsCheckCrl={IsCheckCrl}&ThumbPrint={ThumbPrint}")]
        Task<List<TokenDetails>> FetchTokenOCSPCrlDetailsAsync(bool IsCheckCrl,string ThumbPrint);

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/ValidatePersID2FA", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        Task<Boolean> ValidatePersID2FA(string inputPersID);
         
        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/DigitalSignBulkAsync")]
        Task<ResponseBulkSign> DigitalSignBulkAsync(List<DigitalSignData> reqData);  // Add to Sign PDF

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/DigitalSignAsync")]
        Task<ResponseMessage> DigitalSignAsync(List<DigitalSignData> reqData);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/DigitalSignVerifyAsync")]
        ResponseMessage DigitalSignVerifyAsync(DigitalSignData reqData);
         
        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/ByteDigitalSignAsync")]
        Task<ResponseMessage> ByteDigitalSignAsync(List<DigitalSignData> reqData);
       

        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/HasInternetConnectionAsyncTest")]
        Task<bool> HasInternetConnectionAsyncTest();

        [OperationContract]
        [WebInvoke(Method = "POST", UriTemplate = "/SignHash", BodyStyle = WebMessageBodyStyle.Wrapped, RequestFormat = WebMessageFormat.Json, ResponseFormat = WebMessageFormat.Json)]
        string SignHash(string rData);

       
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/Getpdffile")]
        string Getpdffile();

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/PdfCordinatefile")]
        int PdfCordinatefile(DTOCustomSignCordinate customSignCordinate);


        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/AsymmetricEncryption")]
        Task<ResponseMessage> AsymmetricEncryption(List<AsymmetricEncryptionData> reqData);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/AsymmetricDencryption")]
        Task<ResponseMessage> AsymmetricDencryption(List<AsymmetricEncryptionData> reqData);

        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/SymmetricEncryption")]
        Task<ResponseMessage> SymmetricEncryption(SymmetricEncryptionData reqData);
        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/SymmetricDencryption")]
        Task<ResponseMessage> SymmetricDencryption(SymmetricEncryptionData reqData); 
        [OperationContract]
        [WebInvoke(Method = "POST", ResponseFormat = WebMessageFormat.Json, UriTemplate = "/AddWaterMarks")]
        Task<ResponseMessage> AddWaterMarks(DtoWaterMarkData Data);
        [OperationContract]
        [WebInvoke(
             Method = "GET",
             ResponseFormat = WebMessageFormat.Json,
             BodyStyle = WebMessageBodyStyle.Bare,
             UriTemplate = "/GetMacAddress"
         )]
        Task<MacResponse> GetMacAddress();

        [OperationContract]
        [WebInvoke(
               Method = "POST",
               ResponseFormat = WebMessageFormat.Json,
               BodyStyle = WebMessageBodyStyle.Bare,
               UriTemplate = "/VerifyMac"
           )]
        Task<MacVerifyResponse> VerifyMac(DeviceVerifyRequest mac);

    }
    public class DigitalVerifyDetails
    {
        public bool IsVerified { get; set; }
        public string SignatureRemarks { get; set; }
        public string SignatureBy { get; set; }
        public bool IsDigest { get; set; }
        public string DigestRemarks { get; set; }
    }
    public class DigitalVerifyDetailsForUser
    {
      
        public string Signature { get; set; }
        public string SignatureBy { get; set; }
       
    } 
    [DataContract]
    public class ResponseMessage
    {
        [DataMember]
        public bool Valid { get; set; }
        [DataMember]
        public string Message { get; set; }
    }
    [DataContract]
    public class ResponseBulkSign
    {
        public List<ResponseMessage> ResponseMessagelst { get; set; }
        public ResponseMessage ResponseMessage { get; set; }
    }

    [DataContract]
    public class PersIdValidation
    {
        [DataMember]
        public bool vaildId { get; set; }
        [DataMember] 
        public bool Expired { get; set; }
        [DataMember]
        public string Status { get; set; }
        [DataMember]
        public string Remark { get; set; }
    }



    [DataContract]
    public class ResponseStatus
    {
        [DataMember]
        public String Status { get; set; }
        [DataMember]
        public String Remark { get; set; }
    }
    [DataContract]
    public class XmlDataForPublicKey
    {
        [DataMember]
        public string Public_Key { get; set; }
        [DataMember]
        public string SerialNo { get; set; }
        
        [DataMember]
        public bool Status { get; set; }
        [DataMember]
        public bool TokenValid { get; set; }
        [DataMember]
        public string ValidFrom { get; set; }
        [DataMember]
        public string ValidTo { get; set; }
    }
        [DataContract]
    public class TokenDetails
    {

        [DataMember]
        public String API { get; set; }
        [DataMember]
        public Boolean CRL_OCSPCheck { get; set; }
        [DataMember]
        public String CRL_OCSPMsg { get; set; }
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
        [DataMember]
        public string Public_Key { get; set; }

    }


    public class DigitalSignData
    {

        [DataMember]
        public String Thumbprint { get; set; }
        [DataMember]
        public String FolderLoc { get; set; }
        [DataMember]
        public String OutputFolderLoc { get; set; }
        [DataMember]       
        public string pdfpath { get; set; }
        [DataMember]
        public int XCoordinate { get; set; }
        [DataMember]
        public int YCoordinate { get; set; }
        [DataMember]
        public int Page { get; set; }
        [DataMember]
        public string CustomText { get; set; }
        [DataMember]
        public string Publickey { get; set; }
    }
    public class AsymmetricEncryptionData
    {

      
        [DataMember]
        public String FolderLoc { get; set; }
        [DataMember]
        public String OutputFolderLoc { get; set; }
        [DataMember]
        public string FilePath { get; set; }
        [DataMember]
        public string Publickey { get; set; }
    }
    public class SymmetricEncryptionData
    {


        [DataMember]
        public String FolderLoc { get; set; }
        [DataMember]
        public String OutputFolderLoc { get; set; }
        [DataMember]
        public string FilePath { get; set; }
        [DataMember]
        public string Password { get; set; }
    }
    public class DtoWaterMarkData
    {


        [DataMember]
        public bool Datetime { get; set; }
        [DataMember]
        public bool IpAddress { get; set; }
        [DataMember]
        public string CustomText { get; set; }
        [DataMember]
        public String FolderLoc { get; set; }
        public string FilePath { get; set; }
        
    }
    
    [DataContract]
    public class CompositeType
    {
        bool boolValue = true;
        string stringValue = "Hello ";

        [DataMember]
        public bool BoolValue
        {
            get { return boolValue; }
            set { boolValue = value; }
        }



        [DataMember]
        public string StringValue
        {
            get { return stringValue; }
            set { stringValue = value; }
        }
        [DataMember()]
        public object ScoreData;
    }

    [DataContract]
    public sealed class DTOSaveDigitalSignInfo
    {       
        [DataMember]
        public string PublicKey { get; set; }       
        [DataMember]
        public string SerialNo { get; set; }
        [DataMember]
        public bool ValidToken { get; set; }
        [DataMember]
        public string ValidFrom { get; set; }
        [DataMember] 
        public string ValidTo { get;set; }
        [DataMember]
        public string SignedDateTime { get; set; }
        [DataMember]
        public string OriginForSign { get; set; }

        [DataMember]
        public string RefererForSign { get; set; }

        [DataMember]
        public string IpAddress  { get; set; }
        [DataMember] 
        public string DocumentName { get;set; }
        [DataMember]
        public string DocumentType { get; set; }
        [DataMember]
        public string Remarks { get; set; }
    }
    [DataContract]
    public sealed class MacResponse
    {
        [DataMember] public bool Status { get; set; }
        [DataMember] public string Message { get; set; }
        [DataMember] public string MachineName { get; set; }
        [DataMember] public string MacAddress { get; set; }
        [DataMember] public string WindowsUserName { get; set; }   
        [DataMember] public string ClientIpAddress { get; set; }
    }

    [DataContract]
    public sealed class DeviceVerifyRequest
    {
        [DataMember] public string Mac { get; set; }
        [DataMember] public string UserName { get; set; }
        [DataMember] public string IpAddress { get; set; }
    }

    [DataContract]
    public sealed class MacVerifyResponse
    {
        [DataMember] public bool Status { get; set; }
        [DataMember] public string Message { get; set; }

        // Input
        [DataMember] public string InputMac { get; set; }
        [DataMember] public string InputUserName { get; set; }
        [DataMember] public string InputIpAddress { get; set; }

        // Device (actual)
        [DataMember] public string MachineName { get; set; }
        [DataMember] public string DeviceMac { get; set; }
        [DataMember] public string DeviceUserName { get; set; }
        [DataMember] public string DeviceIpAddress { get; set; }

        // Match
        [DataMember] public bool IsMacMatch { get; set; }
        [DataMember] public bool IsUserMatch { get; set; }
        [DataMember] public bool IsIpMatch { get; set; }
        [DataMember] public bool IsAllMatch { get; set; }
    }

}