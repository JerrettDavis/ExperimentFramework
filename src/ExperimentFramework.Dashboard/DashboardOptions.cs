using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard;

/// <summary>
/// Configuration options for the experiment dashboard.
/// </summary>
public sealed class DashboardOptions
{
    /// <summary>
    /// Gets or sets the base path for the dashboard (default: "/dashboard").
    /// </summary>
    public string PathBase { get; set; } = "/dashboard";

    /// <summary>
    /// Gets or sets the dashboard title.
    /// </summary>
    public string Title { get; set; } = "Experiment Dashboard";

    /// <summary>
    /// Gets or sets whether analytics features are enabled.
    /// </summary>
    public bool EnableAnalytics { get; set; } = true;

    /// <summary>
    /// Gets or sets whether governance UI features are enabled.
    /// </summary>
    public bool EnableGovernanceUI { get; set; } = true;

    /// <summary>
    /// Gets or sets whether authorization is required.
    /// </summary>
    public bool RequireAuthorization { get; set; } = false;

    /// <summary>
    /// Gets or sets the authorization policy name.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Gets or sets the tenant resolver.
    /// </summary>
    public ITenantResolver TenantResolver { get; set; } = new NullTenantResolver();

    /// <summary>
    /// Gets or sets the authorization provider.
    /// </summary>
    public IAuthorizationProvider? AuthorizationProvider { get; set; }

    /// <summary>
    /// Gets or sets the data provider.
    /// </summary>
    public IDashboardDataProvider? DataProvider { get; set; }

    /// <summary>
    /// Gets or sets the analytics provider.
    /// </summary>
    public IAnalyticsProvider? AnalyticsProvider { get; set; }

    /// <summary>
    /// Gets or sets the theme provider.
    /// </summary>
    public IDashboardThemeProvider? ThemeProvider { get; set; }

    /// <summary>
    /// Gets or sets the default confidence level for statistical analysis (default: 0.95).
    /// </summary>
    public double DefaultConfidenceLevel { get; set; } = 0.95;

    /// <summary>
    /// Gets or sets the minimum sample size for statistical tests (default: 100).
    /// </summary>
    public int MinSampleSize { get; set; } = 100;

    /// <summary>
    /// Gets or sets the data retention period in days (default: 90).
    /// </summary>
    public int DataRetentionDays { get; set; } = 90;

    /// <summary>
    /// Gets or sets the number of items per page in list views (default: 25).
    /// </summary>
    public int ItemsPerPage { get; set; } = 25;

    /// <summary>
    /// Gets or sets whether dark mode is enabled (default: true).
    /// </summary>
    public bool EnableDarkMode { get; set; } = true;

    /// <summary>
    /// Gets or sets the default theme (Light or Dark).
    /// </summary>
    public string DefaultTheme { get; set; } = "Light";
}

/// <summary>
/// Default null tenant resolver that returns no tenant.
/// </summary>
internal sealed class NullTenantResolver : ITenantResolver
{
    public Task<TenantContext?> ResolveAsync(Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        return Task.FromResult<TenantContext?>(null);
    }
}
