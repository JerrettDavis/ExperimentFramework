using System.Security.Claims;

namespace AspireDemo.Blog.Contracts;

/// <summary>
/// Plugin interface for authentication.
/// Implementations provide different auth mechanisms (JWT, OAuth, API Key, etc.)
/// </summary>
public interface IBlogAuthProvider
{
    /// <summary>Name of the authentication method.</summary>
    string AuthMethod { get; }

    /// <summary>Description of the auth provider.</summary>
    string AuthDescription { get; }

    /// <summary>Authenticates a user with the provided credentials.</summary>
    Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken ct = default);

    /// <summary>Refreshes an access token using a refresh token.</summary>
    Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>Revokes/invalidates a token.</summary>
    Task<bool> RevokeAsync(string token, CancellationToken ct = default);

    /// <summary>Validates a token and returns the claims principal.</summary>
    Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Gets user information from a valid token.</summary>
    Task<BlogUser?> GetUserFromTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Gets the configuration for this auth provider.</summary>
    AuthProviderConfig GetConfiguration();

    /// <summary>Gets the login URL for OAuth providers (null for non-OAuth).</summary>
    string? GetLoginUrl(string redirectUri);
}

/// <summary>
/// Request type for authentication.
/// </summary>
public enum AuthRequestType
{
    /// <summary>Username and password authentication.</summary>
    UsernamePassword,

    /// <summary>API key authentication.</summary>
    ApiKey,

    /// <summary>OAuth authorization code exchange.</summary>
    OAuthCode,

    /// <summary>Token refresh.</summary>
    RefreshToken
}

/// <summary>
/// Authentication request.
/// </summary>
public record AuthRequest
{
    /// <summary>Type of authentication being attempted.</summary>
    public AuthRequestType Type { get; init; }

    /// <summary>Username (for UsernamePassword type).</summary>
    public string? Username { get; init; }

    /// <summary>Password (for UsernamePassword type).</summary>
    public string? Password { get; init; }

    /// <summary>API key (for ApiKey type).</summary>
    public string? ApiKey { get; init; }

    /// <summary>OAuth authorization code (for OAuthCode type).</summary>
    public string? Code { get; init; }

    /// <summary>OAuth redirect URI (for OAuthCode type).</summary>
    public string? RedirectUri { get; init; }

    /// <summary>Refresh token (for RefreshToken type).</summary>
    public string? RefreshToken { get; init; }
}

/// <summary>
/// Result of an authentication attempt.
/// </summary>
public record AuthResult
{
    public bool Success { get; init; }
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? Error { get; init; }
    public BlogUser? User { get; init; }
}

/// <summary>
/// Configuration for an auth provider.
/// </summary>
public record AuthProviderConfig
{
    /// <summary>Whether this provider supports token refresh.</summary>
    public bool RequiresRefresh { get; init; }

    /// <summary>How long access tokens are valid.</summary>
    public TimeSpan TokenLifetime { get; init; }

    /// <summary>OAuth scopes required (if applicable).</summary>
    public List<string> RequiredScopes { get; init; } = [];

    /// <summary>Whether this is an OAuth provider.</summary>
    public bool IsOAuth { get; init; }

    /// <summary>Login button text.</summary>
    public string LoginButtonText { get; init; } = "Sign In";

    /// <summary>Login button icon.</summary>
    public string? LoginButtonIcon { get; init; }
}
