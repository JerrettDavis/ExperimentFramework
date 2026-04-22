using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Navigation;

[Binding]
public class NavigationStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;

    public NavigationStepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser   = browser;
        _dashboard = dashboard;
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Given
    // -------------------------------------------------------------------------

    [Given(@"I am on the dashboard home page")]
    public async Task GivenIAmOnTheDashboardHomePage()
    {
        await _dashboard.NavigateToAsync("/dashboard");
        var homePage = new HomePage(Page);
        var loaded = await homePage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Dashboard home page did not load — home container not visible.");
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I click {string} in the navigation menu")]
    public async Task WhenIClickInTheNavigationMenu(string navItem)
    {
        var navMenu = new NavMenuComponent(Page);

        // Map the feature-file label to the exact nav link text the app renders.
        var label = navItem switch
        {
            "Targeting Rules"   => "Targeting",
            "Hypothesis Testing"=> "Hypothesis",
            "Overview"          => "Home",
            _                   => navItem
        };

        // Delegate to named helpers where they exist; fall back to the generic helper via reflection.
        switch (label)
        {
            case "Experiments":   await navMenu.GoToExperimentsAsync();   break;
            case "Analytics":     await navMenu.GoToAnalyticsAsync();     break;
            case "Governance":    await navMenu.GoToGovernanceAsync();    break;
            case "Targeting":     await navMenu.GoToTargetingAsync();     break;
            case "Rollout":       await navMenu.GoToRolloutAsync();       break;
            case "Hypothesis":    await navMenu.GoToHypothesisAsync();    break;
            case "Plugins":       await navMenu.GoToPluginsAsync();       break;
            case "Configuration": await navMenu.GoToConfigurationAsync(); break;
            case "DSL Editor":    await navMenu.GoToDslEditorAsync();     break;
            case "Home":          await navMenu.GoToHomeAsync();          break;
            default:
                throw new ArgumentException(
                    $"No navigation helper is mapped for nav item '{navItem}' (resolved label: '{label}'). " +
                    "Add a case to WhenIClickInTheNavigationMenu.");
        }
    }

    [When(@"I click the {string} feature card")]
    public async Task WhenIClickTheFeatureCard(string cardTitle)
    {
        var homePage = new HomePage(Page);
        await homePage.ClickFeatureCardAsync(cardTitle);
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see feature cards for all sections")]
    public async Task ThenIShouldSeeFeatureCardsForAllSections(DataTable table)
    {
        var homePage = new HomePage(Page);
        var actualTitles = await homePage.GetFeatureCardTitlesAsync();

        var expectedTitles = table.Rows.Select(r => r["Card Title"]).ToList();
        var missing = expectedTitles
            .Where(expected => !actualTitles.Any(actual =>
                actual.Contains(expected, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missing.Count > 0)
        {
            throw new Exception(
                $"The following feature cards were not found on the home page: {string.Join(", ", missing.Select(m => $"'{m}'"))}. " +
                $"Cards found: {string.Join(", ", actualTitles.Select(t => $"'{t}'"))}.");
        }
    }

    [Then(@"the page should load successfully")]
    public async Task ThenThePageShouldLoadSuccessfully()
    {
        // Verify a primary content area is visible and no error indicators are present.
        await Page.WaitForSelectorAsync(
            "main, [role='main'], .page-content, #app",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

        // Check there is no access-denied message.
        var accessDeniedLocator = Page.Locator(
            "[class*='access-denied'], [class*='forbidden'], h1:has-text('Access Denied'), h1:has-text('403')");
        var count = await accessDeniedLocator.CountAsync();
        if (count > 0)
        {
            throw new Exception("Page loaded with an access-denied or 403 error.");
        }
    }
}
