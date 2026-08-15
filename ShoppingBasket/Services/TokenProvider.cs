using ShoppingBasket.API.Interfaces;
using System.Text.Json;

namespace ShoppingBasket.API.Services;

public class TokenProvider : ITokenProvider
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenProvider> _logger;

    private string? _token;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly string _loginEmail;

    public TokenProvider(HttpClient httpClient, IConfiguration configuration, ILogger<TokenProvider> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _loginEmail = _configuration["CodeChallengeApi:LoginEmail"] ?? string.Empty;
    }

    public string? CurrentToken => _token;
        
    public async Task<(string? Token, DateTimeOffset? Expiry)> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_loginEmail))
        {
            _logger.LogWarning("CodeChallengeApi LoginEmail is not configured.");
            return (null, null);
        }

        try
        {
            var loginBody = new { email = _loginEmail };
            using var resp = await _httpClient.PostAsJsonAsync("Login", loginBody, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Login to Code Challenge API failed with status {Status}", resp.StatusCode);
                return (null, null);
            }

            var content = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(content))
            {
                return (null, null);
            }

            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            string? token = null;
            DateTimeOffset? expiry = null;

            if (root.TryGetProperty("token", out var tokenProp) || root.TryGetProperty("access_token", out tokenProp))
            {
                token = tokenProp.GetString();
            }

            if (root.TryGetProperty("expiresIn", out var expiresProp) && expiresProp.TryGetInt32(out var seconds))
            {
                expiry = DateTimeOffset.UtcNow.AddSeconds(seconds);
            }

            return (token, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while obtaining token from Code Challenge API");
            return (null, null);
        }
    }

    public async Task EnsureTokenAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(_token) && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-1))
        {
            return;
        }

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_token) && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-1))
            {
                return;
            }

            var (token, expiry) = await GetTokenAsync(cancellationToken);
            _token = token;
            _tokenExpiry = expiry ?? DateTimeOffset.UtcNow.AddHours(1);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
