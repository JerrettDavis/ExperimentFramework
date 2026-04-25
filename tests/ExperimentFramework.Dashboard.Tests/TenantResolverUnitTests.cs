using System.Security.Claims;
using ExperimentFramework.Dashboard.Abstractions;
using ExperimentFramework.Dashboard.TenantResolvers;
using Microsoft.AspNetCore.Http;

namespace ExperimentFramework.Dashboard.Tests;

/// <summary>
/// Unit tests for the four ITenantResolver implementations.
/// </summary>
public sealed class TenantResolverUnitTests
{
    // ===== HttpHeaderTenantResolver =====

    [Fact]
    public async Task HttpHeaderResolver_WithHeader_ReturnsTenantContext()
    {
        var resolver = new HttpHeaderTenantResolver("X-Tenant-Id");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = "my-tenant";

        var result = await resolver.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal("my-tenant", result.TenantId);
    }

    [Fact]
    public async Task HttpHeaderResolver_MissingHeader_ReturnsNull()
    {
        var resolver = new HttpHeaderTenantResolver("X-Tenant-Id");
        var httpContext = new DefaultHttpContext();

        var result = await resolver.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task HttpHeaderResolver_EmptyHeader_ReturnsNull()
    {
        var resolver = new HttpHeaderTenantResolver("X-Tenant-Id");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = string.Empty;

        var result = await resolver.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task HttpHeaderResolver_WhitespaceHeader_ReturnsNull()
    {
        var resolver = new HttpHeaderTenantResolver("X-Tenant-Id");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = "   ";

        var result = await resolver.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task HttpHeaderResolver_DefaultHeader_UsesXTenantId()
    {
        var resolver = new HttpHeaderTenantResolver();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = "default-tenant";

        var result = await resolver.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal("default-tenant", result.TenantId);
    }

    [Fact]
    public async Task HttpHeaderResolver_CustomHeader_UsesCorrectHeader()
    {
        var resolver = new HttpHeaderTenantResolver("X-Custom-Tenant");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Custom-Tenant"] = "custom-tenant";

        var result = await resolver.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal("custom-tenant", result.TenantId);
    }

    // ===== ClaimTenantResolver =====

    [Fact]
    public async Task ClaimResolver_WithClaim_ReturnsTenantContext()
    {
        var resolver = new ClaimTenantResolver("tenant_id");
        var claims = new[] { new Claim("tenant_id", "claim-tenant") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext();
        httpContext.User = principal;

        var result = await resolver.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal("claim-tenant", result.TenantId);
    }

    [Fact]
    public async Task ClaimResolver_MissingClaim_ReturnsNull()
    {
        var resolver = new ClaimTenantResolver("tenant_id");
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var result = await resolver.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task ClaimResolver_EmptyClaimValue_ReturnsNull()
    {
        var resolver = new ClaimTenantResolver("tenant_id");
        var claims = new[] { new Claim("tenant_id", string.Empty) };
        var identity = new ClaimsIdentity(claims, "Test");
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(identity);

        var result = await resolver.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task ClaimResolver_DefaultClaimType_UsesTenantId()
    {
        var resolver = new ClaimTenantResolver();
        var claims = new[] { new Claim("tenant_id", "default-claim-tenant") };
        var identity = new ClaimsIdentity(claims, "Test");
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(identity);

        var result = await resolver.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal("default-claim-tenant", result.TenantId);
    }

    // ===== SubdomainTenantResolver =====

    [Fact]
    public async Task SubdomainResolver_WithSubdomain_ReturnsTenantContext()
    {
        var resolver = new SubdomainTenantResolver("example.com");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("tenant1.example.com");

        var result = await resolver.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal("tenant1", result.TenantId);
    }

    [Fact]
    public async Task SubdomainResolver_NoSubdomain_ReturnsNull()
    {
        var resolver = new SubdomainTenantResolver("example.com");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("example.com");

        var result = await resolver.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task SubdomainResolver_DifferentDomain_ReturnsNull()
    {
        var resolver = new SubdomainTenantResolver("example.com");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("other.org");

        var result = await resolver.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task SubdomainResolver_DeepSubdomain_ExtractsOnlySubdomain()
    {
        var resolver = new SubdomainTenantResolver("example.com");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("deep.tenant1.example.com");

        var result = await resolver.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal("deep.tenant1", result.TenantId);
    }

    // ===== CompositeTenantResolver =====

    [Fact]
    public async Task CompositeResolver_FirstResolverSucceeds_ReturnsTenantContext()
    {
        var headerResolver = new HttpHeaderTenantResolver("X-Tenant-Id");
        var claimResolver = new ClaimTenantResolver();
        var composite = new CompositeTenantResolver(headerResolver, claimResolver);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = "header-tenant";

        var result = await composite.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal("header-tenant", result.TenantId);
    }

    [Fact]
    public async Task CompositeResolver_FirstFails_SecondSucceeds()
    {
        var headerResolver = new HttpHeaderTenantResolver("X-Tenant-Id");
        var claimResolver = new ClaimTenantResolver();
        var composite = new CompositeTenantResolver(headerResolver, claimResolver);

        var claims = new[] { new Claim("tenant_id", "claim-tenant") };
        var identity = new ClaimsIdentity(claims, "Test");
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(identity);
        // No X-Tenant-Id header

        var result = await composite.ResolveAsync(httpContext);

        Assert.NotNull(result);
        Assert.Equal("claim-tenant", result.TenantId);
    }

    [Fact]
    public async Task CompositeResolver_AllFail_ReturnsNull()
    {
        var resolver1 = new HttpHeaderTenantResolver("X-Tenant-Id");
        var resolver2 = new HttpHeaderTenantResolver("X-Other-Tenant");
        var composite = new CompositeTenantResolver(resolver1, resolver2);

        var httpContext = new DefaultHttpContext();

        var result = await composite.ResolveAsync(httpContext);

        Assert.Null(result);
    }

    [Fact]
    public async Task CompositeResolver_NoResolvers_ReturnsNull()
    {
        var composite = new CompositeTenantResolver();
        var httpContext = new DefaultHttpContext();

        var result = await composite.ResolveAsync(httpContext);

        Assert.Null(result);
    }
}
