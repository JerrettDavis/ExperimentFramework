using ExperimentFramework.Dashboard.Abstractions;
using Microsoft.AspNetCore.Http;

namespace ExperimentFramework.Dashboard.TenantResolvers;

/// <summary>
/// Resolves tenant from an HTTP header.
/// </summary>
public sealed class HttpHeaderTenantResolver : ITenantResolver
{
    private readonly string _headerName;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpHeaderTenantResolver"/> class.
    /// </summary>
    /// <param name="headerName">The HTTP header name containing the tenant ID (default: "X-Tenant-Id").</param>
    public HttpHeaderTenantResolver(string headerName = "X-Tenant-Id")
    {
        _headerName = headerName ?? throw new ArgumentNullException(nameof(headerName));
    }

    /// <inheritdoc />
    public Task<TenantContext?> ResolveAsync(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(_headerName, out var tenantId) &&
            !string.IsNullOrWhiteSpace(tenantId))
        {
            var context = new TenantContext
            {
                TenantId = tenantId.ToString()
            };

            return Task.FromResult<TenantContext?>(context);
        }

        return Task.FromResult<TenantContext?>(null);
    }
}
