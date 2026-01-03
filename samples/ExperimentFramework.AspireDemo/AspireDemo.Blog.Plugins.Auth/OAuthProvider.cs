using System.Security.Claims;
using AspireDemo.Blog.Contracts;

namespace AspireDemo.Blog.Plugins.Auth;

/// <summary>
/// OAuth 2.0 authentication supporting GitHub and Google.
/// Simulated for demo purposes.
/// </summary>
public sealed class OAuthProvider : IBlogAuthProvider
{
    public string AuthMethod => "OAuth 2.0";
    public string AuthDescription => "Sign in with GitHub or Google. Delegated authentication for enhanced security.";

    private readonly TimeSpan _tokenLifetime = TimeSpan.FromHours(8);

    // Simulated token storage
    private readonly Dictionary<string, (BlogUser user, DateTime expires)> _tokens = new();
    private readonly Dictionary<string, string> _pendingCodes = new(); // code -> provider

    public async Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken ct = default)
    {
        await Task.Delay(200, ct); // Simulate OAuth flow

        if (request.Type == AuthRequestType.OAuthCode && request.Code != null)
        {
            // Simulate code exchange
            if (_pendingCodes.TryGetValue(request.Code, out var provider))
            {
                _pendingCodes.Remove(request.Code);

                // Create simulated user based on provider
                var user = provider switch
                {
                    "github" => new BlogUser(
                        Guid.NewGuid(),
                        "github_user",
                        "dev@github.com",
                        "GitHub Developer",
                        ["author"]),
                    "google" => new BlogUser(
                        Guid.NewGuid(),
                        "google_user",
                        "user@gmail.com",
                        "Google User",
                        ["reader"]),
                    _ => null
                };

                if (user != null)
                {
                    var token = $"oauth_{Guid.NewGuid():N}";
                    _tokens[token] = (user, DateTime.UtcNow.Add(_tokenLifetime));

                    return new AuthResult
                    {
                        Success = true,
                        AccessToken = token,
                        RefreshToken = null, // OAuth tokens managed by provider
                        ExpiresAt = DateTime.UtcNow.Add(_tokenLifetime),
                        User = user
                    };
                }
            }

            return new AuthResult { Success = false, Error = "Invalid authorization code" };
        }

        return new AuthResult { Success = false, Error = "OAuth requires authorization code exchange" };
    }

    public Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        // OAuth refresh would go through the provider
        return Task.FromResult(new AuthResult
        {
            Success = false,
            Error = "OAuth token refresh requires re-authentication with provider"
        });
    }

    public Task<bool> RevokeAsync(string token, CancellationToken ct = default)
    {
        return Task.FromResult(_tokens.Remove(token));
    }

    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        if (_tokens.TryGetValue(token, out var data) && data.expires > DateTime.UtcNow)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, data.user.Id.ToString()),
                new(ClaimTypes.Name, data.user.Username),
                new(ClaimTypes.Email, data.user.Email),
                new("display_name", data.user.DisplayName)
            };

            foreach (var role in data.user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "OAuth");
            return Task.FromResult<ClaimsPrincipal?>(new ClaimsPrincipal(identity));
        }

        return Task.FromResult<ClaimsPrincipal?>(null);
    }

    public Task<BlogUser?> GetUserFromTokenAsync(string token, CancellationToken ct = default)
    {
        if (_tokens.TryGetValue(token, out var data) && data.expires > DateTime.UtcNow)
        {
            return Task.FromResult<BlogUser?>(data.user);
        }
        return Task.FromResult<BlogUser?>(null);
    }

    public AuthProviderConfig GetConfiguration() => new()
    {
        RequiresRefresh = false,
        TokenLifetime = _tokenLifetime,
        RequiredScopes = ["read:user", "user:email"],
        IsOAuth = true,
        LoginButtonText = "Sign in with GitHub",
        LoginButtonIcon = "github"
    };

    public string? GetLoginUrl(string redirectUri)
    {
        // Simulate OAuth authorization URL
        // In production, this would be a real GitHub/Google OAuth URL
        var code = Guid.NewGuid().ToString("N");
        _pendingCodes[code] = "github";

        return $"https://github.com/login/oauth/authorize?client_id=demo&redirect_uri={Uri.EscapeDataString(redirectUri)}&code={code}";
    }
}
