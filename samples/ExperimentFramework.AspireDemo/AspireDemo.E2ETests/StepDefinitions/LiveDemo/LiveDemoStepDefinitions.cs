using AspireDemo.E2ETests.Drivers;
using Microsoft.Playwright;
using Reqnroll;

namespace AspireDemo.E2ETests.StepDefinitions.LiveDemo;

[Binding]
public class LiveDemoStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly WebDriver _webDriver;

    public LiveDemoStepDefinitions(BrowserDriver browser, WebDriver webDriver)
    {
        _browser   = browser;
        _webDriver = webDriver;
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"the live demo heading should be visible")]
    public async Task ThenLiveDemoHeadingShouldBeVisible()
    {
        // LiveDemo.razor renders <h1>Live Experiment Demo</h1>
        await Page.WaitForSelectorAsync(
            "h1:has-text('Live Experiment Demo'), .demo-header h1",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 20000
            });
    }

    [Then(@"the pricing calculator card should be visible")]
    public async Task ThenPricingCalculatorCardShouldBeVisible()
    {
        // Each demo card has an <h2> heading
        await Page.WaitForSelectorAsync(
            "h2:has-text('Pricing Calculator'), .pricing-card",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 20000
            });
    }

    [Then(@"the notification preview card should be visible")]
    public async Task ThenNotificationPreviewCardShouldBeVisible()
    {
        await Page.WaitForSelectorAsync(
            "h2:has-text('Notification Preview'), .notification-card",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 20000
            });
    }

    [Then(@"the recommendations card should be visible")]
    public async Task ThenRecommendationsCardShouldBeVisible()
    {
        await Page.WaitForSelectorAsync(
            "h2:has-text('Recommendations'), .recommendations-card",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 20000
            });
    }

    [Then(@"the welcome page heading should contain {string}")]
    public async Task ThenWelcomePageHeadingShouldContain(string expectedText)
    {
        // Home.razor at /welcome renders a large <h1> with "ExperimentFramework AspireDemo"
        await Page.WaitForSelectorAsync(
            "h1",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15000
            });
        var headingText = await Page.Locator("h1").First.TextContentAsync() ?? "";
        if (!headingText.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Expected heading to contain '{expectedText}' but was '{headingText}'.");
    }

    [Then(@"the Experiment Dashboard link should be visible")]
    public async Task ThenExperimentDashboardLinkShouldBeVisible()
    {
        // Home.razor has <a href="/dashboard"> linking to the Experiment Dashboard card
        await Page.WaitForSelectorAsync(
            "a[href='/dashboard'], a[href*='dashboard']",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15000
            });
    }

    [Then(@"the Live Test Lab link should be visible")]
    public async Task ThenLiveTestLabLinkShouldBeVisible()
    {
        // Home.razor has <a href="/demo"> linking to the Live Test Lab card
        await Page.WaitForSelectorAsync(
            "a[href='/demo'], a[href*='demo']",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15000
            });
    }
}
