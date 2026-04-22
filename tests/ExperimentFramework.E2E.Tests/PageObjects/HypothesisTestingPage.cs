using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the hypothesis testing page at <c>/dashboard/hypothesis</c>.
/// </summary>
public class HypothesisTestingPage
{
    private readonly IPage _page;

    private ILocator PageContainer    => _page.Locator(".hypothesis-container, [data-page='hypothesis'], main");
    private ILocator HypothesisCards  => _page.Locator(".hypothesis-card, [data-hypothesis-card], .hypothesis-item");
    private ILocator RefreshButton    => _page.Locator("button:has-text('Refresh'), button[data-action='refresh'], .refresh-btn");

    public HypothesisTestingPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the hypothesis testing page container is visible.</summary>
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

    /// <summary>Returns the text content of each hypothesis card.</summary>
    public async Task<IReadOnlyList<string>> GetHypothesisCardsAsync()
    {
        var count = await HypothesisCards.CountAsync();
        var cards = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await HypothesisCards.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                cards.Add(text.Trim());
        }

        return cards;
    }

    /// <summary>
    /// Returns the status label for the hypothesis with the given <paramref name="name"/>.
    /// Looks for a status badge or label within the card.
    /// </summary>
    public async Task<string> GetHypothesisStatusAsync(string name)
    {
        var card = HypothesisCards.Filter(new LocatorFilterOptions { HasText = name });
        var statusEl = card.Locator(".status, .badge, [data-status], .hypothesis-status");
        return (await statusEl.First.TextContentAsync() ?? string.Empty).Trim();
    }

    /// <summary>Clicks the Refresh button and waits for the network to settle.</summary>
    public async Task RefreshAsync()
    {
        await RefreshButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
