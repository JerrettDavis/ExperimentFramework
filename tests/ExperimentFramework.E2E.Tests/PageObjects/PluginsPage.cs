using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the plugins page at <c>/dashboard/plugins</c>.
/// </summary>
public class PluginsPage
{
    private readonly IPage _page;

    private ILocator PageContainer  => _page.Locator(".plugins-container, [data-page='plugins'], main");
    private ILocator PluginCards    => _page.Locator(".plugin-card, [data-plugin-card], .plugin-item");
    private ILocator StatsSection   => _page.Locator(".plugin-stats, [data-stats], .stats-section");
    private ILocator RefreshButton  => _page.Locator("button:has-text('Refresh'), button[data-action='refresh'], .refresh-btn");

    public PluginsPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the plugins page container is visible.</summary>
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

    /// <summary>Returns the number of plugin cards displayed.</summary>
    public Task<int> GetPluginCountAsync() =>
        PluginCards.CountAsync();

    /// <summary>Returns the text content of each plugin card.</summary>
    public async Task<IReadOnlyList<string>> GetPluginCardsAsync()
    {
        var count = await PluginCards.CountAsync();
        var cards = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await PluginCards.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                cards.Add(text.Trim());
        }

        return cards;
    }

    /// <summary>Clicks the Reload button on the plugin whose id matches <paramref name="id"/>.</summary>
    public async Task ReloadPluginAsync(string id)
    {
        var card = GetPluginCardLocator(id);
        var reloadBtn = card.Locator("button:has-text('Reload'), button[data-action='reload']");
        await reloadBtn.ClickAsync();
    }

    /// <summary>Clicks the Unload button on the plugin whose id matches <paramref name="id"/>.</summary>
    public async Task UnloadPluginAsync(string id)
    {
        var card = GetPluginCardLocator(id);
        var unloadBtn = card.Locator("button:has-text('Unload'), button[data-action='unload']");
        await unloadBtn.ClickAsync();
    }

    /// <summary>
    /// Selects a specific implementation for an interface exposed by a plugin.
    /// </summary>
    public async Task UseImplementationAsync(string pluginId, string interfaceName, string implName)
    {
        var card = GetPluginCardLocator(pluginId);

        // Find the interface section inside the card
        var interfaceSection = card
            .Locator(".interface-item, [data-interface]")
            .Filter(new LocatorFilterOptions { HasText = interfaceName });

        // Find the implementation option and click "Use" / select it
        var implOption = interfaceSection
            .Locator(".impl-item, [data-impl]")
            .Filter(new LocatorFilterOptions { HasText = implName });

        var useBtn = implOption.Locator("button:has-text('Use'), button[data-action='use']");
        await useBtn.ClickAsync();
    }

    /// <summary>Clicks the Refresh button and waits for the network to settle.</summary>
    public async Task RefreshAsync()
    {
        await RefreshButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>Returns plugin statistics text from the stats section.</summary>
    public async Task<string> GetStatsAsync() =>
        (await StatsSection.TextContentAsync() ?? string.Empty).Trim();

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private ILocator GetPluginCardLocator(string id) =>
        PluginCards.Filter(new LocatorFilterOptions { HasText = id });
}
