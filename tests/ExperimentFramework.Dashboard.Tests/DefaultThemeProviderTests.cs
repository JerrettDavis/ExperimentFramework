using ExperimentFramework.Dashboard.Abstractions;
using ExperimentFramework.Dashboard.Theming;

namespace ExperimentFramework.Dashboard.Tests;

/// <summary>
/// Unit tests for DefaultThemeProvider.
/// </summary>
public sealed class DefaultThemeProviderTests
{
    [Fact]
    public async Task GetThemeAsync_WithNullTheme_ReturnsDefaults()
    {
        var provider = new DefaultThemeProvider();

        var theme = await provider.GetThemeAsync();

        Assert.NotNull(theme);
        Assert.Equal("Experiment Dashboard", theme.Title);
        Assert.Equal("#3b82f6", theme.PrimaryColor);
        Assert.Equal("#8b5cf6", theme.SecondaryColor);
        Assert.False(theme.DarkModeDefault);
    }

    [Fact]
    public async Task GetThemeAsync_WithCustomTheme_ReturnsCustomTheme()
    {
        var customTheme = new DashboardTheme
        {
            Title = "My Dashboard",
            PrimaryColor = "#ff0000",
            SecondaryColor = "#00ff00",
            DarkModeDefault = true
        };

        var provider = new DefaultThemeProvider(customTheme);
        var theme = await provider.GetThemeAsync();

        Assert.Equal("My Dashboard", theme.Title);
        Assert.Equal("#ff0000", theme.PrimaryColor);
        Assert.Equal("#00ff00", theme.SecondaryColor);
        Assert.True(theme.DarkModeDefault);
    }

    [Fact]
    public async Task GetThemeAsync_WithTenantId_StillReturnsSameTheme()
    {
        var provider = new DefaultThemeProvider();
        var themeDefault = await provider.GetThemeAsync();
        var themeTenant = await provider.GetThemeAsync(tenantId: "tenant-123");

        Assert.Equal(themeDefault.Title, themeTenant.Title);
        Assert.Equal(themeDefault.PrimaryColor, themeTenant.PrimaryColor);
    }

    [Fact]
    public async Task GetThemeAsync_WithCancellationToken_ReturnsTheme()
    {
        var provider = new DefaultThemeProvider();
        using var cts = new CancellationTokenSource();

        var theme = await provider.GetThemeAsync(cancellationToken: cts.Token);

        Assert.NotNull(theme);
    }

    [Fact]
    public async Task GetThemeAsync_ReturnsSameInstanceEachCall()
    {
        var provider = new DefaultThemeProvider();

        var theme1 = await provider.GetThemeAsync();
        var theme2 = await provider.GetThemeAsync();

        Assert.Same(theme1, theme2);
    }
}
