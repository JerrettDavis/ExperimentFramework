using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.HypothesisTesting;

[Binding]
[Scope(Feature = "Hypothesis Testing Dashboard")]
public class HypothesisTestingStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;

    public HypothesisTestingStepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser   = browser;
        _dashboard = dashboard;
    }

    private IPage Page => _browser.Page;

    private HypothesisTestingPage HypothesisPage => new(_browser.Page);

    // -------------------------------------------------------------------------
    // Given / Background
    // -------------------------------------------------------------------------

    [Given(@"I am on the hypothesis testing page")]
    public async Task GivenIAmOnTheHypothesisTestingPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/hypothesis");

        // The first render seeds demo data via an API call — wait for that to complete.
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var loaded = await HypothesisPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Hypothesis testing page did not load — page container not visible.");
    }

    [Given(@"there are experiments with hypotheses")]
    public async Task GivenThereAreExperimentsWithHypotheses()
    {
        // Wait for the demo-data seed to complete so cards are present.
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var cards = await HypothesisPage.GetHypothesisCardsAsync();
        if (cards.Count == 0)
            throw new Exception(
                "No hypothesis cards found — this scenario requires seeded demo data. " +
                "Ensure the application seeds demo data on first render.");
    }

    [Given(@"there are completed hypothesis tests")]
    public async Task GivenThereAreCompletedHypothesisTests()
    {
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Completed tests expose statistical result fields.  Check for the presence of
        // result-specific selectors rather than requiring a specific card count.
        var resultPresent = await Page.QuerySelectorAsync(
            ".result-section, [data-result], .statistical-result, " +
            ".p-value, [data-p-value], " +
            ".effect-size, [data-effect-size]") is not null;

        if (!resultPresent)
            throw new Exception(
                "No completed hypothesis test results found — this scenario requires at least " +
                "one completed test with statistical results in the demo data.");
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I click the refresh button")]
    [Scope(Feature = "Hypothesis Testing Dashboard")]
    public async Task WhenIClickTheRefreshButton()
    {
        await HypothesisPage.RefreshAsync();
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see hypothesis cards or an empty state")]
    public async Task ThenIShouldSeeHypothesisCardsOrAnEmptyState()
    {
        // After demo-data seed the page should show cards; if no data exists show empty state.
        await Page.WaitForSelectorAsync(
            ".hypothesis-card, [data-hypothesis-card], .hypothesis-item, " +
            ".empty-state, [data-empty-state], .no-data",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"each hypothesis card should show a status badge")]
    public async Task ThenEachHypothesisCardShouldShowAStatusBadge()
    {
        await Page.WaitForSelectorAsync(
            ".status-badge, [data-status-badge], .badge",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

        var badgeCount = await Page.Locator(".status-badge, [data-status-badge], .badge").CountAsync();
        if (badgeCount == 0)
            throw new Exception("No status badges found on hypothesis cards.");
    }

    [Then(@"the status should be one of {string}, {string}, or {string}")]
    public async Task ThenTheStatusShouldBeOneOf(string status1, string status2, string status3)
    {
        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { status1, status2, status3 };
        var badges  = Page.Locator(".status-badge, [data-status-badge], .badge");
        var count   = await badges.CountAsync();

        for (var i = 0; i < count; i++)
        {
            var text = (await badges.Nth(i).TextContentAsync() ?? string.Empty).Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(text) && !allowed.Contains(text))
                throw new Exception(
                    $"Unexpected hypothesis status '{text}'. " +
                    $"Allowed values: {string.Join(", ", allowed)}.");
        }
    }

    [Then(@"each hypothesis card should display the test type")]
    public async Task ThenEachHypothesisCardShouldDisplayTheTestType()
    {
        var cardTexts = await HypothesisPage.GetHypothesisCardsAsync();
        if (cardTexts.Count == 0)
            throw new Exception("No hypothesis cards found.");

        // Common test types: "t-test", "z-test", "chi-square", "bayesian", "Mann-Whitney".
        var testTypePattern = new System.Text.RegularExpressions.Regex(
            @"t[\-\s]test|z[\-\s]test|chi[\-\s]?square|bayesian|mann[\-\s]?whitney|ab[\-\s]test",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        // Alternatively look for a dedicated test-type element.
        var hasTestTypeEl = await Page.QuerySelectorAsync(
            ".test-type, [data-test-type], .hypothesis-type") is not null;

        if (!hasTestTypeEl)
        {
            foreach (var text in cardTexts)
            {
                if (!testTypePattern.IsMatch(text))
                    throw new Exception(
                        $"Hypothesis card does not appear to display a test type. Card text: '{text}'");
            }
        }
    }

    [Then(@"each hypothesis card should display the primary metric")]
    public async Task ThenEachHypothesisCardShouldDisplayThePrimaryMetric()
    {
        // Look for a dedicated metric element first, then fall back to card text content.
        var hasMetricEl = await Page.QuerySelectorAsync(
            ".primary-metric, [data-primary-metric], .metric-name") is not null;

        if (!hasMetricEl)
        {
            var cardTexts = await HypothesisPage.GetHypothesisCardsAsync();
            if (cardTexts.Count == 0)
                throw new Exception("No hypothesis cards found.");

            foreach (var text in cardTexts)
            {
                if (!text.Contains("metric", StringComparison.OrdinalIgnoreCase)
                    && !text.Contains("conversion", StringComparison.OrdinalIgnoreCase)
                    && !text.Contains("revenue", StringComparison.OrdinalIgnoreCase)
                    && !text.Contains("rate", StringComparison.OrdinalIgnoreCase))
                {
                    throw new Exception(
                        $"Hypothesis card does not appear to display a primary metric. Card text: '{text}'");
                }
            }
        }
    }

    [Then(@"I should see sample sizes for control and treatment")]
    public async Task ThenIShouldSeeSampleSizesForControlAndTreatment()
    {
        // NOTE: the original selector mixed CSS and Playwright "text=" engine syntax
        // in a single comma-separated string, which the Playwright CSS parser rejects
        // with "Unexpected token '='". Use explicit CSS selectors plus a regex text
        // locator via ILocator.Or() so both branches are still accepted.
        var cssLocator = Page.Locator(
            ".sample-size, [data-sample-size], " +
            ".control-size, [data-control-size], " +
            ".treatment-size, [data-treatment-size]");
        var textLocator = Page.GetByText(new System.Text.RegularExpressions.Regex("sample|n=", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        await cssLocator.Or(textLocator).First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should see the effect size")]
    public async Task ThenIShouldSeeTheEffectSize()
    {
        var cssLocator = Page.Locator(".effect-size, [data-effect-size]");
        var textLocator = Page.GetByText(new System.Text.RegularExpressions.Regex("effect size", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        await cssLocator.Or(textLocator).First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should see the p-value")]
    public async Task ThenIShouldSeeThePValue()
    {
        var cssLocator = Page.Locator(".p-value, [data-p-value]");
        var textLocator = Page.GetByText(new System.Text.RegularExpressions.Regex(@"p[\-\s]?value|p\s*=", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        await cssLocator.Or(textLocator).First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should see the confidence interval")]
    public async Task ThenIShouldSeeTheConfidenceInterval()
    {
        var cssLocator = Page.Locator(".confidence-interval, [data-confidence-interval], .ci");
        var textLocator = Page.GetByText(new System.Text.RegularExpressions.Regex("confidence interval|95%|CI", System.Text.RegularExpressions.RegexOptions.IgnoreCase));
        await cssLocator.Or(textLocator).First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the hypothesis data should reload")]
    public async Task ThenTheHypothesisDataShouldReload()
    {
        var loaded = await HypothesisPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Hypothesis testing page container disappeared after refresh.");
    }
}
