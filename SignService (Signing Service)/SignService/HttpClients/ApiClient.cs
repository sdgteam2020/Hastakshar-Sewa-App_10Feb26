using SignService.Security;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SignService.HttpClients
{
    public class ApiClient
    {
        //private static readonly string _baseUrl =
        //    "https://localhost:7018/";

        //private static readonly string _baseUrl =
        //    "https://192.168.10.41";

        //private static readonly string _baseUrl =
        //    "https://192.168.10.251";

        private static readonly string _baseUrl =
            "https://hastaksharsewa.army.mil";

        private static readonly DeviceJwtHttpClient _client =
            new DeviceJwtHttpClient(_baseUrl);

        public async Task<List<XmlDataForPublicKey>>
            PostRequestAsync(
                string endpoint,
                object postData,
                CancellationToken cancellationToken = default)
        {
            try
            {
                var creds =
                    DeviceCredentialStore.GetOrCreate();

                return await _client
                    .PostJsonAsync<List<XmlDataForPublicKey>>(
                        endpoint,
                        postData,
                        creds.DomainId,
                        creds.IPAddress,
                        creds.ClientKey,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    $"API request timed out. Endpoint={endpoint}");

                throw;
            }
            catch (OperationCanceledException ex)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    $"API request cancelled. Endpoint={endpoint}");

                throw;
            }
            catch (HttpRequestException ex)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    $"API HTTP request failed. Endpoint={endpoint}");

                throw;
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    $"API request failed. Endpoint={endpoint}");

                throw;
            }
        }

        public async Task<T> PostRequestAsync<T>(
            string endpoint,
            object postData,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var creds =
                    DeviceCredentialStore.GetOrCreate();

                return await _client
                    .PostJsonAsync<T>(
                        endpoint,
                        postData,
                        creds.DomainId,
                        creds.IPAddress,
                        creds.ClientKey,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    $"API request timed out. Endpoint={endpoint}");

                throw;
            }
            catch (OperationCanceledException ex)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    $"API request cancelled. Endpoint={endpoint}");

                throw;
            }
            catch (HttpRequestException ex)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    $"API HTTP request failed. Endpoint={endpoint}");

                throw;
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    $"API request failed. Endpoint={endpoint}");

                throw;
            }
        }
    }
}