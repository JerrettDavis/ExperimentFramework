using ExperimentFramework.Dashboard.UI.Services;

namespace ExperimentFramework.Dashboard.UI.Tests.Services;

/// <summary>
/// Unit tests for ThemeService (pure C#, no Blazor).
/// </summary>
public sealed class ThemeServiceTests
{
    [Fact]
    public void CurrentTheme_Default_IsNull()
    {
        var svc = new ThemeService();
        Assert.Null(svc.CurrentTheme);
    }

    [Fact]
    public void SetTheme_Null_DoesNotUpdateCurrentTheme()
    {
        var svc = new ThemeService();
        svc.SetTheme(null);
        Assert.Null(svc.CurrentTheme);
    }

    [Fact]
    public void SetTheme_ValidTheme_UpdatesCurrentTheme()
    {
        var svc = new ThemeService();
        var theme = new ThemeResponse { Name = "dark", PrimaryColor = "#000" };

        svc.SetTheme(theme);

        Assert.Equal("dark", svc.CurrentTheme?.Name);
        Assert.Equal("#000", svc.CurrentTheme?.PrimaryColor);
    }

    [Fact]
    public void SetTheme_RaisesOnThemeChanged()
    {
        var svc = new ThemeService();
        var raised = 0;
        svc.OnThemeChanged += () => raised++;

        svc.SetTheme(new ThemeResponse { Name = "light" });

        Assert.Equal(1, raised);
    }

    [Fact]
    public void SetTheme_Null_DoesNotRaiseOnThemeChanged()
    {
        var svc = new ThemeService();
        var raised = 0;
        svc.OnThemeChanged += () => raised++;

        svc.SetTheme(null);

        Assert.Equal(0, raised);
    }

    [Fact]
    public void GetCssVariables_WithNoTheme_ReturnsEmptyString()
    {
        var svc = new ThemeService();
        Assert.Equal(string.Empty, svc.GetCssVariables());
    }

    [Fact]
    public void GetCssVariables_WithTheme_ContainsPrimaryColor()
    {
        var svc = new ThemeService();
        svc.SetTheme(new ThemeResponse
        {
            Name = "test",
            PrimaryColor = "#3b82f6",
            BackgroundColor = "#ffffff",
            TextColor = "#1f2937",
            AccentColor = "#8b5cf6"
        });

        var css = svc.GetCssVariables();

        Assert.Contains("--theme-primary", css);
        Assert.Contains("#3b82f6", css);
    }

    [Fact]
    public void GetCssVariables_WithTheme_ContainsAllFourVariables()
    {
        var svc = new ThemeService();
        svc.SetTheme(new ThemeResponse
        {
            PrimaryColor = "#aaa",
            BackgroundColor = "#bbb",
            TextColor = "#ccc",
            AccentColor = "#ddd"
        });

        var css = svc.GetCssVariables();

        Assert.Contains("--theme-primary", css);
        Assert.Contains("--theme-background", css);
        Assert.Contains("--theme-text", css);
        Assert.Contains("--theme-accent", css);
    }

    [Fact]
    public void SetTheme_Twice_UpdatesToLatestTheme()
    {
        var svc = new ThemeService();
        svc.SetTheme(new ThemeResponse { Name = "first", PrimaryColor = "#111" });
        svc.SetTheme(new ThemeResponse { Name = "second", PrimaryColor = "#222" });

        Assert.Equal("second", svc.CurrentTheme?.Name);
        Assert.Equal("#222", svc.CurrentTheme?.PrimaryColor);
    }
}
