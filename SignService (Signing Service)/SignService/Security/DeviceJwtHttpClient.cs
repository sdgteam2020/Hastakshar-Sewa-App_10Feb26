using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SignService.Security
{
    public sealed class DeviceJwtHttpClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        private string _accessToken;
        private DateTime _expiresUtc;

        private DateTime _tokenFailUntilUtc = DateTime.MinValue;

        private readonly SemaphoreSlim _tokenLock =
            new SemaphoreSlim(1, 1);

        public DeviceJwtHttpClient(string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException(
                    "Base URL is required.",
                    nameof(baseUrl));

            _baseUrl = baseUrl.TrimEnd('/') + "/";

            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),

                // OLD VALUE WAS 15 SECONDS.
                // That was prematurely cancelling SaveDigitalSign.
                Timeout = TimeSpan.FromSeconds(60)
            };
        }

        public async Task EnsureTokenAsync(
            string domainId,
            string ipAddress,
            string clientKey,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            if (DateTime.UtcNow < _tokenFailUntilUtc)
            {
                throw new InvalidOperationException(
                    $"Device token request is in cooldown until " +
                    $"{_tokenFailUntilUtc:O}.");
            }

            if (TokenIsValid())
                return;

            await _tokenLock
                .WaitAsync(ct)
                .ConfigureAwait(false);

            try
            {
                ct.ThrowIfCancellationRequested();

                // Another thread might have refreshed the token
                // while we were waiting for the semaphore.
                if (TokenIsValid())
                    return;

                try
                {
                    var token = await RequestDeviceTokenAsync(
                        domainId,
                        ipAddress,
                        clientKey,
                        ct
                    ).ConfigureAwait(false);

                    _accessToken = token.AccessToken;

                    _expiresUtc = DateTime.UtcNow.AddSeconds(
                        token.ExpiresInSeconds);

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue(
                            "Bearer",
                            _accessToken);

                    _tokenFailUntilUtc = DateTime.MinValue;
                }
                catch (HttpRequestException ex)
                {
                    var message = ex.Message ?? string.Empty;

                    if (message.Contains("403") ||
                        message.IndexOf(
                            "Forbidden",
                            StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Do not permanently disable the client.
                        // Allow retry after cooldown.
                        _tokenFailUntilUtc =
                            DateTime.UtcNow.AddMinutes(5);
                    }

                    throw;
                }
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private bool TokenIsValid()
        {
            return !string.IsNullOrWhiteSpace(_accessToken) &&
                   DateTime.UtcNow <
                   _expiresUtc.AddMinutes(-2);
        }

        private async Task<TokenResponse> RequestDeviceTokenAsync(
            string domainId,
            string ipAddress,
            string clientKey,
            CancellationToken ct)
        {
            var payload = new
            {
                domainId,
                ipAddress,
                clientKey
            };

            var json = JsonConvert.SerializeObject(payload);

            var url = new Uri(
                _httpClient.BaseAddress,
                "api/device-auth/token");

            try
            {
                using (var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"))
                {
                    using (var resp = await _httpClient
                        .PostAsync(url, content, ct)
                        .ConfigureAwait(false))
                    {
                        var body = await resp.Content
                            .ReadAsStringAsync()
                            .ConfigureAwait(false);

                        if (!resp.IsSuccessStatusCode)
                        {
                            throw new HttpRequestException(
                                $"Token error: " +
                                $"{(int)resp.StatusCode} " +
                                $"{resp.StatusCode} - {body}");
                        }

                        var token =
                            JsonConvert.DeserializeObject<TokenResponse>(
                                body);

                        if (token == null ||
                            string.IsNullOrWhiteSpace(
                                token.AccessToken))
                        {
                            throw new InvalidOperationException(
                                $"Token response invalid. RawBody={body}");
                        }

                        if (token.ExpiresInSeconds <= 0)
                            token.ExpiresInSeconds = 1800;

                        return token;
                    }
                }
            }
            catch (TaskCanceledException ex)
                when (!ct.IsCancellationRequested)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    "DEVICE-JWT TOKEN HTTP TIMEOUT\n" +
                    $"BaseAddress={_httpClient.BaseAddress}\n" +
                    $"RequestUri={url}\n" +
                    "Method=POST\n" +
                    $"HttpClientTimeout={_httpClient.Timeout}\n");

                throw new TimeoutException(
                    $"Device token request timed out after " +
                    $"{_httpClient.Timeout.TotalSeconds} seconds.",
                    ex);
            }
            catch (OperationCanceledException ex)
                when (ct.IsCancellationRequested)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    "DEVICE-JWT TOKEN REQUEST CANCELLED\n" +
                    $"RequestUri={url}\n");

                throw;
            }
            catch (HttpRequestException ex)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    "DEVICE-JWT HTTP FAILED\n" +
                    $"BaseAddress={_httpClient.BaseAddress}\n" +
                    $"RequestUri={url}\n" +
                    "Method=POST\n" +
                    "ContentType=application/json\n");

                throw;
            }
            catch (Exception ex)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    "DEVICE-JWT UNEXPECTED ERROR\n" +
                    $"BaseAddress={_httpClient.BaseAddress}\n" +
                    $"RequestUri={url}\n");

                throw;
            }
        }

        public async Task<T> PostJsonAsync<T>(
            string endpoint,
            object postData,
            string domainId,
            string ipAddress,
            string clientKey,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            await EnsureTokenAsync(
                domainId,
                ipAddress,
                clientKey,
                ct
            ).ConfigureAwait(false);

            var result = await PostOnceAsync<T>(
                endpoint,
                postData,
                ct
            ).ConfigureAwait(false);

            // Safe retry for 401 because authorization was rejected.
            if (result.isUnauthorized)
            {
                await ForceRefreshTokenAsync(
                    domainId,
                    ipAddress,
                    clientKey,
                    ct
                ).ConfigureAwait(false);

                result = await PostOnceAsync<T>(
                    endpoint,
                    postData,
                    ct
                ).ConfigureAwait(false);
            }

            if (!result.isSuccess)
            {
                throw new HttpRequestException(
                    result.errorMessage ??
                    "HTTP request failed.");
            }

            return result.value;
        }

        private async Task ForceRefreshTokenAsync(
            string domainId,
            string ipAddress,
            string clientKey,
            CancellationToken ct)
        {
            await _tokenLock
                .WaitAsync(ct)
                .ConfigureAwait(false);

            try
            {
                _accessToken = null;
                _expiresUtc = DateTime.MinValue;

                _httpClient.DefaultRequestHeaders.Authorization =
                    null;
            }
            finally
            {
                _tokenLock.Release();
            }

            await EnsureTokenAsync(
                domainId,
                ipAddress,
                clientKey,
                ct
            ).ConfigureAwait(false);
        }

        private async Task<(
            bool isSuccess,
            bool isUnauthorized,
            T value,
            string errorMessage)> PostOnceAsync<T>(
                string endpoint,
                object postData,
                CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException(
                    "Endpoint is required.",
                    nameof(endpoint));

            ct.ThrowIfCancellationRequested();

            var url = endpoint.TrimStart('/');

            var jsonBody =
                JsonConvert.SerializeObject(postData);

            try
            {
                using (var content = new StringContent(
                    jsonBody,
                    Encoding.UTF8,
                    "application/json"))
                {
                    using (var resp = await _httpClient
                        .PostAsync(
                            url,
                            content,
                            ct)
                        .ConfigureAwait(false))
                    {
                        var body = await resp.Content
                            .ReadAsStringAsync()
                            .ConfigureAwait(false);

                        if (resp.StatusCode ==
                            HttpStatusCode.Unauthorized)
                        {
                            return (
                                false,
                                true,
                                default(T),
                                "Unauthorized " +
                                "(token expired/invalid).");
                        }

                        if (!resp.IsSuccessStatusCode)
                        {
                            return (
                                false,
                                false,
                                default(T),
                                $"Error: {(int)resp.StatusCode} - " +
                                $"{resp.StatusCode} - {body}");
                        }

                        if (string.IsNullOrWhiteSpace(body))
                        {
                            return (
                                false,
                                false,
                                default(T),
                                "API returned an empty response.");
                        }

                        var value =
                            JsonConvert.DeserializeObject<T>(
                                body);

                        return (
                            true,
                            false,
                            value,
                            null);
                    }
                }
            }
            catch (TaskCanceledException ex)
                when (!ct.IsCancellationRequested)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    "API HTTP TIMEOUT\n" +
                    $"BaseAddress={_httpClient.BaseAddress}\n" +
                    $"Endpoint={url}\n" +
                    $"HttpClientTimeout={_httpClient.Timeout}\n");

                // IMPORTANT:
                // Do NOT automatically retry signing POSTs after
                // a timeout because the server may already have
                // processed the first request.
                throw new TimeoutException(
                    $"API request '{url}' timed out after " +
                    $"{_httpClient.Timeout.TotalSeconds} seconds.",
                    ex);
            }
            catch (OperationCanceledException ex)
                when (ct.IsCancellationRequested)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    "API REQUEST CANCELLED BY CALLER\n" +
                    $"BaseAddress={_httpClient.BaseAddress}\n" +
                    $"Endpoint={url}\n");

                throw;
            }
            catch (HttpRequestException ex)
            {
                ErrorLog.LogErrorToFile(
                    ex,
                    "API HTTP ERROR\n" +
                    $"BaseAddress={_httpClient.BaseAddress}\n" +
                    $"Endpoint={url}\n");

                throw;
            }
        }
    }

    public sealed class TokenResponse
    {
        public string AccessToken { get; set; } = "";
        public int ExpiresInSeconds { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }

    public sealed class DeviceTokenRequest
    {
        public string DeviceId { get; set; } = "";
        public string DeviceKey { get; set; } = "";
    }
}