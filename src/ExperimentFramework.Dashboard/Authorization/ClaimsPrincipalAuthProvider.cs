using System.Security.Claims;
using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard.Authorization;

/// <summary>
/// Default authorization provider that extracts roles and claims from ClaimsPrincipal.
/// </summary>
public sealed class ClaimsPrincipalAuthProvider : IAuthorizationProvider
{
    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetRolesAsync(ClaimsPrincipal user)
    {
        if (user == null)
        {
            return Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
        }

        var roles = user.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .Distinct()
            .ToList();

        return Task.FromResult<IReadOnlyList<string>>(roles);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Claim>> GetClaimsAsync(ClaimsPrincipal user)
    {
        if (user == null)
        {
            return Task.FromResult<IReadOnlyList<Claim>>(Array.Empty<Claim>());
        }

        var claims = user.Claims.ToList();
        return Task.FromResult<IReadOnlyList<Claim>>(claims);
    }

    /// <inheritdoc />
    public Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission)
    {
        if (user == null || string.IsNullOrWhiteSpace(permission))
        {
            return Task.FromResult(false);
        }

        // Check for permission claim
        var hasPermission = user.Claims.Any(c =>
            (c.Type == "permission" || c.Type == "permissions") &&
            c.Value.Equals(permission, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(hasPermission);
    }
}
