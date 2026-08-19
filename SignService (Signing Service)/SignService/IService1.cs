
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

       
        Task<XmlElement> SignXml(XmlElement data);

        List<DigitalVerifyDetails> VerifySignXml(XmlElement data);

       
        Task<TokenDetails> GetPublicKey();


        Task<List<TokenDetails>> FetchPersID();
       
       
        Task<List<TokenDetails>> FetchUniqueTokenDetails();
       
       
        Task<List<TokenDetails>> FetchTokenDetails();

       
         Task<List<PersIdValidation>> ValidatePersID(string inputPersID);

        
       
        Task<List<TokenDetails>> FetchTokenOCSPCrlDetails(bool IsCheckCrl,string ThumbPrint);
        Task<List<TokenDetailsOcsp>> FetchTokenOCSPDetailsAsync(string ThumbPrint);
        Task<List<TokenDetailsCrl>> FetchTokenCrlDetailsAsync(string ThumbPrint);
        Task<Boolean> ValidatePersID2FA(string inputPersID);
         
       
        Task<ResponseBulkSign> DigitalSignBulkAsync(List<DigitalSignData> reqData);   

       
        Task<ResponseMessage> DigitalSignAsync(List<DigitalSignData> reqData);

        
        ResponseMessage DigitalSignVerifyAsync(DigitalSignData reqData);
         
       
        Task<ResponseMessage> ByteDigitalSignAsync(List<DigitalSignData> reqData);
       

        Task<bool> HasInternetConnectionAsyncTest();

       
        string SignHash(string rData);

       
        
        string Getpdffile();

       
        int PdfCordinatefile(DTOCustomSignCordinate customSignCordinate);


       
        Task<ResponseMessage> AsymmetricEncryption(List<AsymmetricEncryptionData> reqData);


        Task<ResponseMessage> AsymmetricDencryption(List<AsymmetricEncryptionData> reqData);

       
        Task<ResponseMessage> SymmetricEncryption(SymmetricEncryptionData reqData);
       
        Task<ResponseMessage> SymmetricDencryption(SymmetricEncryptionData reqData); 
       
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
    public class TokenDetailsOcsp
    {

        [DataMember]
        public String API { get; set; }
        [DataMember]
        public Boolean OCSPCheck { get; set; }
        [DataMember]
        public String OCSPMsg { get; set; }

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
    public class TokenDetailsCrl
    {

        [DataMember]
        public String API { get; set; }
        [DataMember]
        public Boolean CrlCheck { get; set; }
        [DataMember]
        public String CrlMsg { get; set; }

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

        
        [DataMember] public string InputMac { get; set; }
        [DataMember] public string InputUserName { get; set; }
        [DataMember] public string InputIpAddress { get; set; }

         
        [DataMember] public string MachineName { get; set; }
        [DataMember] public string DeviceMac { get; set; }
        [DataMember] public string DeviceUserName { get; set; }
        [DataMember] public string DeviceIpAddress { get; set; }

      
        [DataMember] public bool IsMacMatch { get; set; }
        [DataMember] public bool IsUserMatch { get; set; }
        [DataMember] public bool IsIpMatch { get; set; }
        [DataMember] public bool IsAllMatch { get; set; }
    }

}