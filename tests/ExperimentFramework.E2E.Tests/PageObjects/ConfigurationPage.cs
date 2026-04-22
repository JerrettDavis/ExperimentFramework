using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the configuration page at <c>/dashboard/configuration</c>.
/// </summary>
public class ConfigurationPage
{
    private readonly IPage _page;

    private ILocator PageContainer      => _page.Locator(".configuration-container, [data-page='configuration'], main");
    private ILocator FrameworkInfoPanel => _page.Locator(".framework-info, [data-framework-info], .info-panel");
    private ILocator YamlCodeBlock      => _page.Locator("pre, code, .yaml-config, [data-yaml]");
    private ILocator CopyButton         => _page.Locator("button:has-text('Copy'), button[data-action='copy'], .copy-btn");
    private ILocator RefreshButton      => _page.Locator("button:has-text('Refresh'), button[data-action='refresh'], .refresh-btn");
    private ILocator EnabledFeatures    => _page.Locator(".feature-flag, .enabled-feature, [data-feature='enabled']");

    public ConfigurationPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the configuration page container is visible.</summary>
    public async Task<bool> IsLoadedAsync()
    {
        try
        {
            await PageContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>Returns the text content of the framework info panel.</summary>
    public async Task<string> GetFrameworkInfoAsync() =>
        (await FrameworkInfoPanel.TextContentAsync() ?? string.Empty).Trim();

    /// <summary>Returns the raw YAML configuration text displayed on the page.</summary>
    public async Task<string> GetYamlConfigAsync() =>
        (await YamlCodeBlock.First.TextContentAsync() ?? string.Empty).Trim();

    /// <summary>Clicks the Copy to Clipboard button.</summary>
    public Task CopyToClipboardAsync() =>
        CopyButton.ClickAsync();

    /// <summary>Clicks the Refresh button and waits for the network to settle.</summary>
    public async Task RefreshAsync()
    {
        await RefreshButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>Returns the labels of all enabled features shown on the page.</summary>
    public async Task<IReadOnlyList<string>> GetEnabledFeaturesAsync()
    {
        var count = await EnabledFeatures.CountAsync();
        var features = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await EnabledFeatures.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                features.Add(text.Trim());
        }

        return features;
    }
}
