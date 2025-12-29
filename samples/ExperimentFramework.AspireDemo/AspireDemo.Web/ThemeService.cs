namespace AspireDemo.Web;

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
