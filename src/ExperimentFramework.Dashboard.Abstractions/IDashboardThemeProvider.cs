namespace ExperimentFramework.Dashboard.Abstractions;

/// <summary>
/// Provides theme customization for the dashboard.
/// </summary>
public interface IDashboardThemeProvider
{
    /// <summary>
    /// Gets the theme for a tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier, or null for the default tenant.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The dashboard theme.</returns>
    Task<DashboardTheme> GetThemeAsync(string? tenantId = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents dashboard theme settings.
/// </summary>
public sealed class DashboardTheme
{
    /// <summary>
    /// Gets or sets the primary color.
    /// </summary>
    public string? PrimaryColor { get; init; }

    /// <summary>
    /// Gets or sets the secondary color.
    /// </summary>
    public string? SecondaryColor { get; init; }

    /// <summary>
    /// Gets or sets the logo URL.
    /// </summary>
    public string? LogoUrl { get; init; }

    /// <summary>
    /// Gets or sets the dashboard title.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Gets or sets whether dark mode is enabled by default.
    /// </summary>
    public bool DarkModeDefault { get; init; }
}
