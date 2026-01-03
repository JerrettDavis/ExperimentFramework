using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AspireDemo.Blog.Contracts;
using Microsoft.IdentityModel.Tokens;

namespace AspireDemo.Blog.Plugins.Auth;

/// <summary>
/// JWT-based authentication with access and refresh tokens.
/// Standard token-based auth for APIs and SPAs.
/// </summary>
public sealed class JwtAuthProvider : IBlogAuthProvider
{
    public string AuthMethod => "JWT";
    public string AuthDescription => "JSON Web Token authentication with access and refresh tokens. Stateless and scalable.";

    private const string SecretKey = "SuperSecretKeyForDemoOnlyDoNotUseInProduction123!";
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromHours(1);
    private readonly TimeSpan _refreshLifetime = TimeSpan.FromDays(7);

    private readonly Dictionary<string, (BlogUser user, DateTime expires)> _refreshTokens = new();
    private readonly Dictionary<string, BlogUser> _users = new()
    {
        ["admin"] = new BlogUser(Guid.NewGuid(), "admin", "admin@blog.dev", "Blog Admin", ["admin", "author"]),
        ["author"] = new BlogUser(Guid.NewGuid(), "author", "author@blog.dev", "Content Author", ["author"]),
        ["reader"] = new BlogUser(Guid.NewGuid(), "reader", "reader@blog.dev", "Blog Reader", ["reader"])
    };

    public async Task<AuthResult> AuthenticateAsync(AuthRequest request, CancellationToken ct = default)
    {
        await Task.Delay(100, ct); // Simulate processing

        if (request.Type == AuthRequestType.UsernamePassword)
        {
            // Demo: accept any password for known users
            if (request.Username != null && _users.TryGetValue(request.Username, out var user))
            {
                return GenerateTokens(user);
            }

            return new AuthResult { Success = false, Error = "Invalid username or password" };
        }

        if (request.Type == AuthRequestType.RefreshToken && request.RefreshToken != null)
        {
            return await RefreshTokenAsync(request.RefreshToken, ct);
        }

        return new AuthResult { Success = false, Error = "Unsupported authentication type" };
    }

    public Task<AuthResult> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        if (_refreshTokens.TryGetValue(refreshToken, out var data))
        {
            if (data.expires > DateTime.UtcNow)
            {
                _refreshTokens.Remove(refreshToken); // Single use
                return Task.FromResult(GenerateTokens(data.user));
            }
            _refreshTokens.Remove(refreshToken);
        }

        return Task.FromResult(new AuthResult { Success = false, Error = "Invalid or expired refresh token" });
    }

    public Task<bool> RevokeAsync(string token, CancellationToken ct = default)
    {
        // For JWTs, we'd typically add to a blocklist
        // For refresh tokens, remove from storage
        return Task.FromResult(_refreshTokens.Remove(token));
    }

    public Task<ClaimsPrincipal?> ValidateTokenAsync(string token, CancellationToken ct = default)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(SecretKey);

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "BlogDemo",
                ValidateAudience = true,
                ValidAudience = "BlogDemoApp",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            }, out _);

            return Task.FromResult<ClaimsPrincipal?>(principal);
        }
        catch
        {
            return Task.FromResult<ClaimsPrincipal?>(null);
        }
    }

    public async Task<BlogUser?> GetUserFromTokenAsync(string token, CancellationToken ct = default)
    {
        var principal = await ValidateTokenAsync(token, ct);
        if (principal == null) return null;

        var username = principal.FindFirst(ClaimTypes.Name)?.Value;
        if (username != null && _users.TryGetValue(username, out var user))
        {
            return user;
        }

        return null;
    }

    public AuthProviderConfig GetConfiguration() => new()
    {
        RequiresRefresh = true,
        TokenLifetime = _tokenLifetime,
        RequiredScopes = [],
        IsOAuth = false,
        LoginButtonText = "Sign In with JWT",
        LoginButtonIcon = "key"
    };

    public string? GetLoginUrl(string redirectUri) => null; // Not OAuth

    private AuthResult GenerateTokens(BlogUser user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(SecretKey);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new("display_name", user.DisplayName)
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(_tokenLifetime),
            Issuer = "BlogDemo",
            Audience = "BlogDemoApp",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var accessToken = tokenHandler.WriteToken(token);

        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        _refreshTokens[refreshToken] = (user, DateTime.UtcNow.Add(_refreshLifetime));

        return new AuthResult
        {
            Success = true,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = tokenDescriptor.Expires,
            User = user
        };
    }
}
