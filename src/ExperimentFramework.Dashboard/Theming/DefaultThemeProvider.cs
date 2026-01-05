using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard.Theming;

/// <summary>
/// Default theme provider that returns a consistent theme.
/// </summary>
public sealed class DefaultThemeProvider : IDashboardThemeProvider
{
    private readonly DashboardTheme _theme;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultThemeProvider"/> class.
    /// </summary>
    /// <param name="theme">The theme to return, or null to use defaults.</param>
    public DefaultThemeProvider(DashboardTheme? theme = null)
    {
        _theme = theme ?? new DashboardTheme
        {
            Title = "Experiment Dashboard",
            PrimaryColor = "#3b82f6",
            SecondaryColor = "#8b5cf6",
            DarkModeDefault = false
        };
    }

    /// <inheritdoc />
    public Task<DashboardTheme> GetThemeAsync(string? tenantId = null, CancellationToken cancellationToken = default)
    {
        // Tenant-specific theming could be implemented here
        return Task.FromResult(_theme);
    }
}
