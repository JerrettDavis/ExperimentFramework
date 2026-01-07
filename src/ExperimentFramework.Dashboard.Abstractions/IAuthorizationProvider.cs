using System.Security.Claims;

namespace ExperimentFramework.Dashboard.Abstractions;

/// <summary>
/// Provides authorization and role/claim discovery capabilities.
/// </summary>
/// <remarks>
/// This interface does not manage users or permissions.
/// It provides read-only access to authorization information from the host application's authentication system.
/// </remarks>
public interface IAuthorizationProvider
{
    /// <summary>
    /// Gets the roles assigned to the user.
    /// </summary>
    /// <param name="user">The claims principal representing the user.</param>
    /// <returns>A list of role names.</returns>
    Task<IReadOnlyList<string>> GetRolesAsync(ClaimsPrincipal user);

    /// <summary>
    /// Gets all claims for the user.
    /// </summary>
    /// <param name="user">The claims principal representing the user.</param>
    /// <returns>A list of claims.</returns>
    Task<IReadOnlyList<Claim>> GetClaimsAsync(ClaimsPrincipal user);

    /// <summary>
    /// Checks if the user has a specific permission.
    /// </summary>
    /// <param name="user">The claims principal representing the user.</param>
    /// <param name="permission">The permission name to check.</param>
    /// <returns>True if the user has the permission; otherwise, false.</returns>
    Task<bool> HasPermissionAsync(ClaimsPrincipal user, string permission);
}
