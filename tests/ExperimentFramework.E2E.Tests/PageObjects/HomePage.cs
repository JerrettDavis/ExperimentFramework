using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the dashboard home page at <c>/dashboard</c>.
/// </summary>
public class HomePage
{
    private readonly IPage _page;

    private ILocator HomeContainer  => _page.Locator(".home-container");
    private ILocator FeatureCards   => _page.Locator(".feature-card");
    private ILocator HeroSection    => _page.Locator(".hero");

    public HomePage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the home page is ready by waiting for the main container.</summary>
    public async Task<bool> IsLoadedAsync()
    {
        try
        {
            await HomeContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>Returns the title text of every visible feature card.</summary>
    public async Task<IReadOnlyList<string>> GetFeatureCardTitlesAsync()
    {
        var count = await FeatureCards.CountAsync();
        var titles = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var card = FeatureCards.Nth(i);
            // Try common heading selectors inside a card
            var heading = card.Locator("h2, h3, .card-title, [class*='title']").First;
            var text = await heading.TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                titles.Add(text.Trim());
        }

        return titles;
    }

    /// <summary>Clicks the feature card whose title matches <paramref name="title"/> (case-insensitive).</summary>
    public async Task ClickFeatureCardAsync(string title)
    {
        var count = await FeatureCards.CountAsync();

        for (var i = 0; i < count; i++)
        {
            var card = FeatureCards.Nth(i);
            var heading = card.Locator("h2, h3, .card-title, [class*='title']").First;
            var text = await heading.TextContentAsync();

            if (string.Equals(text?.Trim(), title, StringComparison.OrdinalIgnoreCase))
            {
                await card.ClickAsync();
                return;
            }
        }

        throw new InvalidOperationException($"Feature card with title '{title}' was not found.");
    }
}
