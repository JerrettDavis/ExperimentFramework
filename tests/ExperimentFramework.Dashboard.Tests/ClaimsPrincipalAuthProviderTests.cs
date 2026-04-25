using System.Security.Claims;
using ExperimentFramework.Dashboard.Authorization;

namespace ExperimentFramework.Dashboard.Tests;

/// <summary>
/// Unit tests for ClaimsPrincipalAuthProvider.
/// </summary>
public sealed class ClaimsPrincipalAuthProviderTests
{
    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims)
    {
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }

    // ===== GetRolesAsync tests =====

    [Fact]
    public async Task GetRolesAsync_WithRoleClaims_ReturnsRoles()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = CreatePrincipal(
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "editor")
        );

        var roles = await provider.GetRolesAsync(principal);

        Assert.Equal(2, roles.Count);
        Assert.Contains("admin", roles);
        Assert.Contains("editor", roles);
    }

    [Fact]
    public async Task GetRolesAsync_WithRoleClaimType_ReturnsRoles()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = CreatePrincipal(new Claim("role", "developer"));

        var roles = await provider.GetRolesAsync(principal);

        Assert.Contains("developer", roles);
    }

    [Fact]
    public async Task GetRolesAsync_NullUser_ReturnsEmpty()
    {
        var provider = new ClaimsPrincipalAuthProvider();

        var roles = await provider.GetRolesAsync(null!);

        Assert.Empty(roles);
    }

    [Fact]
    public async Task GetRolesAsync_NoRoleClaims_ReturnsEmpty()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = CreatePrincipal(new Claim(ClaimTypes.Email, "test@test.com"));

        var roles = await provider.GetRolesAsync(principal);

        Assert.Empty(roles);
    }

    [Fact]
    public async Task GetRolesAsync_DuplicateRoles_DeduplicatesRoles()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = CreatePrincipal(
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Role, "admin"),
            new Claim("role", "admin")
        );

        var roles = await provider.GetRolesAsync(principal);

        Assert.Single(roles);
        Assert.Equal("admin", roles[0]);
    }

    // ===== GetClaimsAsync tests =====

    [Fact]
    public async Task GetClaimsAsync_WithClaims_ReturnsClaims()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = CreatePrincipal(
            new Claim(ClaimTypes.Name, "Alice"),
            new Claim(ClaimTypes.Email, "alice@example.com")
        );

        var claims = await provider.GetClaimsAsync(principal);

        Assert.Equal(2, claims.Count);
    }

    [Fact]
    public async Task GetClaimsAsync_NullUser_ReturnsEmpty()
    {
        var provider = new ClaimsPrincipalAuthProvider();

        var claims = await provider.GetClaimsAsync(null!);

        Assert.Empty(claims);
    }

    [Fact]
    public async Task GetClaimsAsync_NoClaims_ReturnsEmpty()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var claims = await provider.GetClaimsAsync(principal);

        Assert.Empty(claims);
    }

    // ===== HasPermissionAsync tests =====

    [Fact]
    public async Task HasPermissionAsync_WithPermissionClaim_ReturnsTrue()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = CreatePrincipal(new Claim("permission", "experiments:read"));

        var hasPermission = await provider.HasPermissionAsync(principal, "experiments:read");

        Assert.True(hasPermission);
    }

    [Fact]
    public async Task HasPermissionAsync_WithPermissionsClaim_ReturnsTrue()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = CreatePrincipal(new Claim("permissions", "experiments:write"));

        var hasPermission = await provider.HasPermissionAsync(principal, "experiments:write");

        Assert.True(hasPermission);
    }

    [Fact]
    public async Task HasPermissionAsync_MissingPermission_ReturnsFalse()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = CreatePrincipal(new Claim("permission", "experiments:read"));

        var hasPermission = await provider.HasPermissionAsync(principal, "experiments:admin");

        Assert.False(hasPermission);
    }

    [Fact]
    public async Task HasPermissionAsync_NullUser_ReturnsFalse()
    {
        var provider = new ClaimsPrincipalAuthProvider();

        var hasPermission = await provider.HasPermissionAsync(null!, "experiments:read");

        Assert.False(hasPermission);
    }

    [Fact]
    public async Task HasPermissionAsync_NullPermission_ReturnsFalse()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = CreatePrincipal(new Claim("permission", "experiments:read"));

        var hasPermission = await provider.HasPermissionAsync(principal, null!);

        Assert.False(hasPermission);
    }

    [Fact]
    public async Task HasPermissionAsync_EmptyPermission_ReturnsFalse()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = CreatePrincipal(new Claim("permission", "experiments:read"));

        var hasPermission = await provider.HasPermissionAsync(principal, string.Empty);

        Assert.False(hasPermission);
    }

    [Fact]
    public async Task HasPermissionAsync_CaseInsensitive_ReturnsTrue()
    {
        var provider = new ClaimsPrincipalAuthProvider();
        var principal = CreatePrincipal(new Claim("permission", "Experiments:Read"));

        var hasPermission = await provider.HasPermissionAsync(principal, "experiments:read");

        Assert.True(hasPermission);
    }
}
