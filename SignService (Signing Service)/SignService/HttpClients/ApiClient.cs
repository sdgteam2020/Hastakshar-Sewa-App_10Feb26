using SignService.Security;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SignService.HttpClients
{
    public class ApiClient
    {
        //private static readonly string _baseUrl = "https://localhost:7018/";
        private static readonly string _baseUrl = "https://192.168.10.41";
        //private static readonly string _baseUrl = "https://192.168.10.251";
        private static readonly DeviceJwtHttpClient _client = new DeviceJwtHttpClient(_baseUrl);

        public async Task<List<XmlDataForPublicKey>> PostRequestAsync(string endpoint, object postData)
        {
            try
            {
                var creds = DeviceCredentialStore.GetOrCreate();

                return await _client.PostJsonAsync<List<XmlDataForPublicKey>>(
                    endpoint,
                    postData,
                    creds.DeviceId,
                    creds.DeviceKey
                );
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
                return null;
            }
        }

        public async Task<T> PostRequestAsync<T>(string endpoint, object postData)
        {
            try
            {
                var creds = DeviceCredentialStore.GetOrCreate();


                return await _client.PostJsonAsync<T>(
                    endpoint,
                    postData,
                    creds.DeviceId,
                    creds.DeviceKey
                );
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(ex);
                return default;
            }
        }
    }
}