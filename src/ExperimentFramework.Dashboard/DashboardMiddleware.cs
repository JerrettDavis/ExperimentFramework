using ExperimentFramework.Dashboard.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

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
    private readonly ILogger<DashboardMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DashboardMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">The dashboard configuration options.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="authorizationService">Optional authorization service for policy-based authorization.</param>
    public DashboardMiddleware(
        RequestDelegate next,
        DashboardOptions options,
        ILogger<DashboardMiddleware> logger,
        IAuthorizationService? authorizationService = null)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip middleware for Blazor framework requests (SignalR, static assets, etc.)
        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/_framework", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_blazor", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_content", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/_vs", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // Check if request is for dashboard path
        if (!context.Request.Path.StartsWithSegments(_options.PathBase))
        {
            await _next(context);
            return;
        }

        var sanitizedPath = (context.Request.Path.Value ?? string.Empty)
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);

        _logger.LogInformation("Dashboard middleware invoked for path: {Path}, Method: {Method}, RequireAuthorization: {RequireAuth}",
            sanitizedPath, context.Request.Method, _options.RequireAuthorization);

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
                var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
                _logger.LogInformation("User authenticated: {IsAuthenticated}, User: {UserName}",
                    isAuthenticated, context.User.Identity?.Name ?? "Anonymous");

                // Check if user is authenticated
                if (!isAuthenticated)
                {
                    _logger.LogWarning("Unauthenticated access attempt to dashboard. Redirecting to login.");

                    // Redirect to login page
                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    context.Response.Redirect($"/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl)}");
                    return;
                }

                // Check authorization policy if specified
                if (_authorizationService != null && !string.IsNullOrEmpty(_options.AuthorizationPolicy))
                {
                    _logger.LogInformation("Checking authorization policy: {Policy}", _options.AuthorizationPolicy);

                    var authResult = await _authorizationService.AuthorizeAsync(
                        context.User,
                        _options.AuthorizationPolicy);

                    if (!authResult.Succeeded)
                    {
                        _logger.LogWarning("Authorization failed for user {UserName}. Redirecting to access denied.",
                            context.User.Identity?.Name);

                        // User is authenticated but doesn't have required permissions
                        context.Response.Redirect("/Account/AccessDenied");
                        return;
                    }

                    _logger.LogInformation("Authorization succeeded for user {UserName}", context.User.Identity?.Name);
                }
            }
            else
            {
                _logger.LogWarning("Dashboard RequireAuthorization is FALSE - allowing anonymous access!");
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
