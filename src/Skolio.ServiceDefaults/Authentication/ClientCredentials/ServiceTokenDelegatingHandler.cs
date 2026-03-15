using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Skolio.ServiceDefaults.Authentication.ClientCredentials;

public sealed class ServiceTokenDelegatingHandler : DelegatingHandler
{
    private readonly ServiceClientOptions _options;
    private readonly ILogger<ServiceTokenDelegatingHandler> _logger;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public ServiceTokenDelegatingHandler(
        IOptions<ServiceClientOptions> options,
        ILogger<ServiceTokenDelegatingHandler> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetTokenAsync(CancellationToken cancellationToken)
    {
        if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
        {
            return _cachedToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_cachedToken is not null && DateTimeOffset.UtcNow < _tokenExpiry)
            {
                return _cachedToken;
            }

            using var tokenClient = new HttpClient { Timeout = TimeSpan.FromSeconds(_options.TokenRequestTimeoutSeconds) };
            var tokenEndpoint = $"{_options.Authority.TrimEnd('/')}/connect/token";

            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["scope"] = _options.Scope
            });

            var response = await tokenClient.PostAsync(tokenEndpoint, tokenRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Failed to obtain service token from {Endpoint}. Status: {StatusCode}, Body: {Body}",
                    tokenEndpoint, (int)response.StatusCode, errorBody);
                throw new InvalidOperationException($"Failed to obtain service token. Status: {(int)response.StatusCode}");
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Empty token response from identity server.");

            _cachedToken = tokenResponse.AccessToken;
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn - 30);

            _logger.LogInformation("Service token obtained successfully. Expires in {ExpiresIn}s.", tokenResponse.ExpiresIn);
            return _cachedToken;
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = string.Empty;
    }
}
