using ExperimentFramework.Dashboard.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ExperimentFramework.Dashboard.TenantResolvers;

/// <summary>
/// Resolves tenant from a user claim.
/// </summary>
public sealed class ClaimTenantResolver : ITenantResolver
{
    private readonly string _claimType;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClaimTenantResolver"/> class.
    /// </summary>
    /// <param name="claimType">The claim type containing the tenant ID (default: "tenant_id").</param>
    public ClaimTenantResolver(string claimType = "tenant_id")
    {
        _claimType = claimType ?? throw new ArgumentNullException(nameof(claimType));
    }

    /// <inheritdoc />
    public Task<TenantContext?> ResolveAsync(HttpContext httpContext)
    {
        var claim = httpContext.User?.FindFirst(_claimType);

        if (claim != null && !string.IsNullOrWhiteSpace(claim.Value))
        {
            var context = new TenantContext
            {
                TenantId = claim.Value
            };

            return Task.FromResult<TenantContext?>(context);
        }

        return Task.FromResult<TenantContext?>(null);
    }
}
