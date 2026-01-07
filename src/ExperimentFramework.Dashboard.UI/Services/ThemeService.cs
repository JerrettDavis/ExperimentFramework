namespace ExperimentFramework.Dashboard.UI.Services;

/// <summary>
/// Service that manages the current theme state and notifies subscribers of changes.
/// </summary>
public class ThemeService
{
    private ThemeResponse? _currentTheme;

    public event Action? OnThemeChanged;

    public ThemeResponse? CurrentTheme => _currentTheme;

    public void SetTheme(ThemeResponse? theme)
    {
        if (theme == null) return;

        _currentTheme = theme;
        OnThemeChanged?.Invoke();
    }

    public string GetCssVariables()
    {
        if (_currentTheme == null)
        {
            return string.Empty;
        }

        return $@"
            --theme-primary: {_currentTheme.PrimaryColor};
            --theme-background: {_currentTheme.BackgroundColor};
            --theme-text: {_currentTheme.TextColor};
            --theme-accent: {_currentTheme.AccentColor};
        ";
    }
}

/// <summary>
/// Represents a theme response from the API.
/// </summary>
public class ThemeResponse
{
    public string Name { get; set; } = "";
    public string PrimaryColor { get; set; } = "#3b82f6";
    public string BackgroundColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#1f2937";
    public string AccentColor { get; set; } = "#8b5cf6";
    public string Variant { get; set; } = "";
    public DateTime Timestamp { get; set; }
}
