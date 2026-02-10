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

        private bool _tokenFailedPermanently = false;
        private DateTime _tokenFailUntilUtc = DateTime.MinValue;

        private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);

        public DeviceJwtHttpClient(string baseUrl)
        {
            _baseUrl = baseUrl.TrimEnd('/') + "/";

            // IMPORTANT: reuse HttpClient (do NOT create per call)
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(_baseUrl),
                 Timeout = TimeSpan.FromSeconds(15)  // ✅ important
            };

            // If you're using self-signed cert locally, DON'T do this in production.
            // For production, remove this and fix the certificate properly.
            // ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        /// <summary>
        /// Ensures a valid JWT token exists in memory. Refreshes if expiring soon.
        /// </summary>
        public async Task EnsureTokenAsync(string deviceId, string deviceKey, CancellationToken ct = default)
        {
            // ✅ stop hammering if token is forbidden
            if (_tokenFailedPermanently)
                throw new Exception("Device token request previously failed (403). Skipping retry.");

            // Optional: cooldown (prevents tight loops even if not permanent)
            if (DateTime.UtcNow < _tokenFailUntilUtc)
                throw new Exception("Token request is in cooldown. Try again later.");

            // Refresh 2 minutes early
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresUtc.AddMinutes(-2))
                return;

            await _tokenLock.WaitAsync(ct);
            try
            {
                // double-check after lock
                if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _expiresUtc.AddMinutes(-2))
                    return;

                try
                {
                    var token = await RequestDeviceTokenAsync(deviceId, deviceKey, ct).ConfigureAwait(false);

                    _accessToken = token.AccessToken;
                    _expiresUtc = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds);

                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", _accessToken);

                    // ✅ reset failure flags on success
                    _tokenFailedPermanently = false;
                    _tokenFailUntilUtc = DateTime.MinValue;
                }
                catch (Exception ex)
                {
                    // ✅ IMPORTANT: mark as failed to prevent infinite retry loop
                    // If it’s 403, make it permanent. Otherwise cooldown for 60s.
                    var msg = ex.Message ?? "";
                  

                    if (msg.Contains("403") || msg.Contains("Forbidden"))
                        _tokenFailedPermanently = true;
                    if (msg.Contains("403") || msg.Contains("Forbidden"))
                        _tokenFailUntilUtc = DateTime.UtcNow.AddMinutes(5); // cooldown


                    throw; // rethrow so caller knows token failed
                }
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private async Task<TokenResponse> RequestDeviceTokenAsync(
                string deviceId,
                string deviceKey,
                CancellationToken ct)
        {
            var payload = new { deviceId, deviceKey }; // camelCase JSON
            var json = JsonConvert.SerializeObject(payload);

             var content = new StringContent(json, Encoding.UTF8, "application/json");

            
            var url = new Uri(_httpClient.BaseAddress, "api/device-auth/token");

            try
            {
                 var resp = await _httpClient.PostAsync(url, content, ct).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(
                        $"Token error: {(int)resp.StatusCode} {resp.StatusCode} - {body}"
                    );
                }

                var token = JsonConvert.DeserializeObject<TokenResponse>(body);
                if (token == null || string.IsNullOrWhiteSpace(token.AccessToken))
                    throw new Exception($"Token response invalid. RawBody={body}");

                if (token.ExpiresInSeconds <= 0)
                    token.ExpiresInSeconds = 1800;

                return token;
            }
            catch (HttpRequestException ex)
            {
                ErrorLog.LogErrorToFile(ex,
                    "DEVICE-JWT HTTP FAILED\n" +
                    $"BaseAddress={_httpClient.BaseAddress}\n" +
                    $"RequestUri={url}\n" +
                    $"Method=POST\n" +
                    "ContentType=application/json\n" +
                    $"Payload={json}\n" 
                   
                );
                throw;
            }
            catch (TaskCanceledException ex) // timeout / cancellation
            {
                ErrorLog.LogErrorToFile(ex,
                    "DEVICE-JWT TIMEOUT/CANCELED\n" +
                    $"BaseAddress={_httpClient.BaseAddress}\n" +
                    $"RequestUri={url}\n" +
                    $"Method=POST\n" +
                    $"Payload={json}\n" 
                );
                throw;
            }
            catch (Exception ex)
            {
                // optional: catch-all so you don't lose unexpected parsing issues
                ErrorLog.LogErrorToFile(ex,
                    "DEVICE-JWT UNEXPECTED ERROR\n" +
                    $"BaseAddress={_httpClient.BaseAddress}\n" +
                    $"RequestUri={url}\n" +
                    $"Method=POST\n" +
                    $"Payload={json}\n"                   
                );
                throw;
            }
        }



        /// <summary>
        /// POST JSON to endpoint and deserialize response. Attaches Bearer token automatically.
        /// Retries once if 401 occurs (token expired).
        /// </summary>
        public async Task<T> PostJsonAsync<T>(string endpoint, object postData, string deviceId, string deviceKey, CancellationToken ct = default)
        {
            await EnsureTokenAsync(deviceId, deviceKey, ct);

            // 1st attempt
            var result = await PostOnceAsync<T>(endpoint, postData, ct);

            // If token invalid/expired, retry once after forcing refresh
            if (result.isUnauthorized)
            {
                await ForceRefreshTokenAsync(deviceId, deviceKey, ct);
                result = await PostOnceAsync<T>(endpoint, postData, ct);
            }

            if (!result.isSuccess)
                throw new Exception(result.errorMessage);

            return result.value;
        }

        private async Task ForceRefreshTokenAsync(string deviceId, string deviceKey, CancellationToken ct)
        {
            await _tokenLock.WaitAsync(ct);
            try
            {
                _accessToken = null;
                _expiresUtc = DateTime.MinValue;
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
            finally
            {
                _tokenLock.Release();
            }

            await EnsureTokenAsync(deviceId, deviceKey, ct);
        }

        private async Task<(bool isSuccess, bool isUnauthorized, T value, string errorMessage)> PostOnceAsync<T>(string endpoint, object postData, CancellationToken ct)
        {
            var url = endpoint.TrimStart('/'); // so BaseAddress works
            var jsonBody = JsonConvert.SerializeObject(postData);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var resp = await _httpClient.PostAsync(url, content, ct);
            var body = await resp.Content.ReadAsStringAsync();

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                return (false, true, default(T), "Unauthorized (token expired/invalid).");

            if (!resp.IsSuccessStatusCode)
                return (false, false, default(T), $"Error: {(int)resp.StatusCode} - {resp.StatusCode} - {body}");

            var value = JsonConvert.DeserializeObject<T>(body);
            return (true, false, value, null);
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