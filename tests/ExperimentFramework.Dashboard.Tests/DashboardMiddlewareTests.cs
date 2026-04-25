using System.Security.Claims;
using ExperimentFramework.Dashboard.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExperimentFramework.Dashboard.Tests;

/// <summary>
/// Unit tests for DashboardMiddleware.
/// </summary>
public sealed class DashboardMiddlewareTests
{
    private static DashboardMiddleware CreateMiddleware(
        RequestDelegate? next = null,
        DashboardOptions? options = null,
        IAuthorizationService? authorizationService = null)
    {
        return new DashboardMiddleware(
            next ?? (ctx => Task.CompletedTask),
            options ?? new DashboardOptions(),
            NullLogger<DashboardMiddleware>.Instance,
            authorizationService
        );
    }

    private static DefaultHttpContext CreateContext(string path, bool authenticated = false, string? user = null)
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;

        if (authenticated)
        {
            var claims = new List<Claim> { new Claim(ClaimTypes.Name, user ?? "test-user") };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            ctx.User = new ClaimsPrincipal(identity);
        }

        return ctx;
    }

    // ===== Bypass paths =====

    [Theory]
    [InlineData("/_framework/blazor.server.js")]
    [InlineData("/_blazor")]
    [InlineData("/_content/something")]
    [InlineData("/_vs/browserLink")]
    public async Task InvokeAsync_BlazorFrameworkPath_BypassesMiddleware(string path)
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(next: ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateContext(path);

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_NonDashboardPath_BypassesMiddleware()
    {
        bool nextCalled = false;
        var middleware = CreateMiddleware(next: ctx => { nextCalled = true; return Task.CompletedTask; });
        var context = CreateContext("/other-path");

        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    // ===== Dashboard path without authorization =====

    [Fact]
    public async Task InvokeAsync_DashboardPath_NoAuth_CallsNext()
    {
        bool nextCalled = false;
        var options = new DashboardOptions { PathBase = "/dashboard", RequireAuthorization = false };
        var middleware = CreateMiddleware(
            next: ctx => { nextCalled = true; return Task.CompletedTask; },
            options: options);

        var context = CreateContext("/dashboard");
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    // ===== Dashboard path with authorization - unauthenticated =====

    [Fact]
    public async Task InvokeAsync_DashboardPath_RequiresAuth_UnauthenticatedUser_Redirects()
    {
        bool nextCalled = false;
        var options = new DashboardOptions
        {
            PathBase = "/dashboard",
            RequireAuthorization = true,
            LoginPath = "/login"
        };
        var middleware = CreateMiddleware(
            next: ctx => { nextCalled = true; return Task.CompletedTask; },
            options: options);

        var context = CreateContext("/dashboard", authenticated: false);
        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(302, context.Response.StatusCode);
    }

    // ===== Dashboard path with authorization - authenticated =====

    [Fact]
    public async Task InvokeAsync_DashboardPath_RequiresAuth_AuthenticatedUser_CallsNext()
    {
        bool nextCalled = false;
        var options = new DashboardOptions
        {
            PathBase = "/dashboard",
            RequireAuthorization = true
        };
        var middleware = CreateMiddleware(
            next: ctx => { nextCalled = true; return Task.CompletedTask; },
            options: options);

        var context = CreateContext("/dashboard", authenticated: true);
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    // ===== API subpath bypass =====

    [Fact]
    public async Task InvokeAsync_ApiSubpath_RequiresAuth_BypassesAuthCheck_CallsNext()
    {
        bool nextCalled = false;
        var options = new DashboardOptions
        {
            PathBase = "/dashboard",
            RequireAuthorization = true
        };
        var middleware = CreateMiddleware(
            next: ctx => { nextCalled = true; return Task.CompletedTask; },
            options: options);

        // Internal API path should bypass auth even for unauthenticated user
        var context = CreateContext("/dashboard/api/experiments", authenticated: false);
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    // ===== Tenant context is set =====

    [Fact]
    public async Task InvokeAsync_DashboardPath_SetsTenantContextInItems()
    {
        var options = new DashboardOptions
        {
            PathBase = "/dashboard",
            RequireAuthorization = false,
            TenantResolver = new FixedTenantResolver("tenant-123")
        };
        object? capturedContext = null;
        var middleware = CreateMiddleware(
            next: ctx => { capturedContext = ctx.Items["TenantContext"]; return Task.CompletedTask; },
            options: options);

        var context = CreateContext("/dashboard");
        await middleware.InvokeAsync(context);

        Assert.NotNull(capturedContext);
        Assert.IsType<TenantContext>(capturedContext);
        Assert.Equal("tenant-123", ((TenantContext)capturedContext).TenantId);
    }

    // ===== Tenant context is cleared after request =====

    [Fact]
    public async Task InvokeAsync_DashboardPath_ClearsTenantContextAfterRequest()
    {
        var options = new DashboardOptions
        {
            PathBase = "/dashboard",
            RequireAuthorization = false,
            TenantResolver = new FixedTenantResolver("tenant-456")
        };
        var middleware = CreateMiddleware(
            next: ctx => Task.CompletedTask,
            options: options);

        var context = CreateContext("/dashboard");
        await middleware.InvokeAsync(context);

        Assert.Null(TenantContextAccessor.Current);
    }

    // ===== Authorization service =====

    [Fact]
    public async Task InvokeAsync_WithAuthorizationPolicy_SuccessfulAuth_CallsNext()
    {
        bool nextCalled = false;
        var options = new DashboardOptions
        {
            PathBase = "/dashboard",
            RequireAuthorization = true,
            AuthorizationPolicy = "DashboardAccess"
        };
        var authService = new AlwaysSucceedingAuthorizationService();
        var middleware = CreateMiddleware(
            next: ctx => { nextCalled = true; return Task.CompletedTask; },
            options: options,
            authorizationService: authService);

        var context = CreateContext("/dashboard", authenticated: true);
        await middleware.InvokeAsync(context);

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_WithAuthorizationPolicy_FailedAuth_Redirects()
    {
        bool nextCalled = false;
        var options = new DashboardOptions
        {
            PathBase = "/dashboard",
            RequireAuthorization = true,
            AuthorizationPolicy = "DashboardAccess"
        };
        var authService = new AlwaysFailingAuthorizationService();
        var middleware = CreateMiddleware(
            next: ctx => { nextCalled = true; return Task.CompletedTask; },
            options: options,
            authorizationService: authService);

        var context = CreateContext("/dashboard", authenticated: true);
        await middleware.InvokeAsync(context);

        Assert.False(nextCalled);
        Assert.Equal(302, context.Response.StatusCode);
    }

    // ===== Helper types =====

    private sealed class FixedTenantResolver : ITenantResolver
    {
        private readonly string _tenantId;

        public FixedTenantResolver(string tenantId) => _tenantId = tenantId;

        public Task<TenantContext?> ResolveAsync(HttpContext httpContext)
            => Task.FromResult<TenantContext?>(new TenantContext { TenantId = _tenantId });
    }

    private sealed class AlwaysSucceedingAuthorizationService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Success());

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(AuthorizationResult.Success());
    }

    private sealed class AlwaysFailingAuthorizationService : IAuthorizationService
    {
        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> requirements)
            => Task.FromResult(AuthorizationResult.Failed());

        public Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user, object? resource, string policyName)
            => Task.FromResult(AuthorizationResult.Failed());
    }
}
