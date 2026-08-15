namespace ShoppingBasket.API.Interfaces;

public interface ITokenProvider
{
    /// <summary>
    /// Fetch a fresh token from the remote login endpoint. Returns token and optional expiry (UTC).
    /// </summary>
    Task<(string? Token, DateTimeOffset? Expiry)> GetTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Ensure there is a valid cached token. May call GetTokenAsync internally to refresh.
    /// </summary>
    Task EnsureTokenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Current cached token if available.
    /// </summary>
    string? CurrentToken { get; }
}
