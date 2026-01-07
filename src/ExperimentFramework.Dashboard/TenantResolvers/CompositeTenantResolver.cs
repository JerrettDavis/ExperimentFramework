using ExperimentFramework.Dashboard.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ExperimentFramework.Dashboard.TenantResolvers;

/// <summary>
/// Resolves tenant by trying multiple strategies in order.
/// </summary>
/// <remarks>
/// Returns the first non-null result from the chain of resolvers.
/// </remarks>
public sealed class CompositeTenantResolver : ITenantResolver
{
    private readonly ITenantResolver[] _resolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeTenantResolver"/> class.
    /// </summary>
    /// <param name="resolvers">The tenant resolvers to try in order.</param>
    public CompositeTenantResolver(params ITenantResolver[] resolvers)
    {
        _resolvers = resolvers ?? throw new ArgumentNullException(nameof(resolvers));
    }

    /// <inheritdoc />
    public async Task<TenantContext?> ResolveAsync(HttpContext httpContext)
    {
        foreach (var resolver in _resolvers)
        {
            var result = await resolver.ResolveAsync(httpContext);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
