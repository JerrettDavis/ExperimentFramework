using ExperimentFramework.Dashboard.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace ExperimentFramework.Dashboard;

/// <summary>
/// Middleware for the experiment dashboard.
/// </summary>
/// <remarks>
/// This middleware:
/// - Resolves tenant context from HTTP requests
/// - Validates authorization if required
/// - Stores tenant context for downstream access
/// </remarks>
public sealed class DashboardMiddleware
{
    private readonly RequestDelegate _next;
    private readonly DashboardOptions _options;
    private readonly IAuthorizationService? _authorizationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The dashboard configuration options.</param>
    /// <param name="authorizationService">Optional authorization service for policy-based authorization.</param>
    public DashboardMiddleware(
        RequestDelegate next,
        DashboardOptions options,
        IAuthorizationService? authorizationService = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Check if request is for dashboard path
        if (!context.Request.Path.StartsWithSegments(_options.PathBase))
        {
            await _next(context);
            return;
        }

        // Resolve tenant context
        var tenantContext = await _options.TenantResolver.ResolveAsync(context);

        // Store tenant context for downstream access
        context.Items["TenantContext"] = tenantContext;
        TenantContextAccessor.Current = tenantContext;

        try
        {
            // Validate authorization if required
            if (_options.RequireAuthorization)
            {
                // Check if user is authenticated
                if (!context.User.Identity?.IsAuthenticated ?? true)
                {
                    // Redirect to login page
                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
                    return;
                }

                // Check authorization policy if specified
                if (_authorizationService != null && !string.IsNullOrEmpty(_options.AuthorizationPolicy))
                {
                    var authResult = await _authorizationService.AuthorizeAsync(
                        context.User,
                        _options.AuthorizationPolicy);

                    if (!authResult.Succeeded)
                    {
                        // User is authenticated but doesn't have required permissions
                        context.Response.Redirect("/Account/AccessDenied");
                        return;
                    }
                }
            }

            // Continue to next middleware
            await _next(context);
        }
        finally
        {
            // Clear tenant context after request
            TenantContextAccessor.Current = null;
        }
    }
}
