using System.Security.Claims;
using AspireDemo.Blog.Contracts;

namespace AspireDemo.Blog.Plugins.Auth;

/// <summary>
/// Simple API key authentication for integrations.
/// Best for server-to-server communication.
/// </summary>
public sealed class ApiKeyAuthProvider : IBlogAuthProvider
{
    public string AuthMethod => "API Key";
    public string AuthDescription => "Simple API key authentication for integrations and server-to-server communication.";

    // Pre-configured API keys for demo
    private readonly Dictionary<string, BlogUser> _apiKeys = new()
    {
        ["demo-admin-key-12345"] = new BlogUser(
            Guid.NewGuid(),
            "api_admin",
            "api-admin@blog.dev",
            "API Admin",
            ["admin", "author"]),
        ["demo-reader-key-67890"] = new BlogUser(
            Guid.NewGuid(),
            "api_reader",
            "api-reader@blog.dev",
            "API Reader",
            ["reader"]),
        ["demo-author-key-abcde"] = new BlogUser(
            Guid.NewGuid(),
            "api_author",
            "api-author@blog.dev",
            "API Author",
            ["author"])
    };

    public Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken ct = default)
    {
        if (request.Type == AuthRequestType.ApiKey && request.ApiKey != null)
        {
            if (_apiKeys.TryGetValue(request.ApiKey, out var user))
            {
                return Task.FromResult(new AuthResult
                {
                    Success = true,
                    AccessToken = request.ApiKey, // API key is the token
                    RefreshToken = null,
                    ExpiresAt = null, // API keys don't expire (in this demo)
                    User = user
                });
            }

            return Task.FromResult(new AuthResult
            {
                Success = false,
                Error = "Invalid API key"
            });
        }

        return Task.FromResult(new AuthResult
        {
            Success = false,
            Error = "API key authentication requires an API key"
        });
    }

    public Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        return Task.FromResult(new AuthResult
        {
            Success = false,
            Error = "API keys do not support refresh"
        });
    }

    public Task<bool> RevokeAsync(string token, CancellationToken ct = default)
    {
        // In production, you'd invalidate the key in your database
        // For demo, we don't actually revoke
        return Task.FromResult(false);
    }

    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        if (_apiKeys.TryGetValue(token, out var user))
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.Email, user.Email),
                new("display_name", user.DisplayName),
                new("auth_method", "api_key")
            };

            foreach (var role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "ApiKey");
            return Task.FromResult<ClaimsPrincipal?>(new ClaimsPrincipal(identity));
        }

        return Task.FromResult<ClaimsPrincipal?>(null);
    }

    public Task<BlogUser?> GetUserFromTokenAsync(string token, CancellationToken ct = default)
    {
        if (_apiKeys.TryGetValue(token, out var user))
        {
            return Task.FromResult<BlogUser?>(user);
        }
        return Task.FromResult<BlogUser?>(null);
    }

    public AuthProviderConfig GetConfiguration() => new()
    {
        RequiresRefresh = false,
        TokenLifetime = TimeSpan.MaxValue, // Never expires
        RequiredScopes = [],
        IsOAuth = false,
        LoginButtonText = "Authenticate with API Key",
        LoginButtonIcon = "key"
    };

    public string? GetLoginUrl(string redirectUri) => null; // Not OAuth
}
