using ExperimentFramework.Dashboard.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ExperimentFramework.Dashboard.TenantResolvers;

/// <summary>
/// Resolves tenant from the subdomain portion of the host.
/// </summary>
/// <remarks>
/// For example, if the host is "tenant1.example.com", the tenant ID will be "tenant1".
/// </remarks>
public sealed class SubdomainTenantResolver : ITenantResolver
{
    private readonly string _baseDomain;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubdomainTenantResolver"/> class.
    /// </summary>
    /// <param name="baseDomain">The base domain (e.g., "example.com").</param>
    public SubdomainTenantResolver(string baseDomain)
    {
        _baseDomain = baseDomain ?? throw new ArgumentNullException(nameof(baseDomain));
    }

    /// <inheritdoc />
    public Task<TenantContext?> ResolveAsync(HttpContext httpContext)
    {
        var host = httpContext.Request.Host.Host;

        if (host.EndsWith(_baseDomain, StringComparison.OrdinalIgnoreCase))
        {
            var subdomain = host.Substring(0, host.Length - _baseDomain.Length).TrimEnd('.');

            if (!string.IsNullOrWhiteSpace(subdomain))
            {
                var context = new TenantContext
                {
                    TenantId = subdomain
                };

                return Task.FromResult<TenantContext?>(context);
            }
        }

        return Task.FromResult<TenantContext?>(null);
    }
}
