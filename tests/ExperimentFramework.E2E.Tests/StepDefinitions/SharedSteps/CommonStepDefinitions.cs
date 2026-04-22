using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.Support;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.SharedSteps;

[Binding]
public class CommonStepDefinitions
{
    private readonly DashboardDriver _dashboard;
    private readonly BrowserDriver _browser;

    public CommonStepDefinitions(DashboardDriver dashboard, BrowserDriver browser)
    {
        _dashboard = dashboard;
        _browser   = browser;
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Given
    // -------------------------------------------------------------------------

    [Given(@"I am logged in as {string}")]
    public async Task GivenIAmLoggedInAs(string role)
    {
        await _dashboard.LoginAsRoleAsync(role);
    }

    [Given(@"I am on the {string} page")]
    public async Task GivenIAmOnThePage(string path)
    {
        var normalised = path.StartsWith('/') ? path : $"/{path}";
        await _dashboard.NavigateToAsync(normalised);
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I click the {string} button")]
    public async Task WhenIClickTheButton(string buttonText)
    {
        await Page.ClickAsync(
            $"button:has-text('{buttonText}'), " +
            $"input[type='button'][value='{buttonText}'], " +
            $"input[type='submit'][value='{buttonText}'], " +
            $"[role='button']:has-text('{buttonText}')");
    }

    [When(@"I navigate to {string}")]
    public async Task WhenINavigateTo(string path)
    {
        var normalised = path.StartsWith('/') ? path : $"/{path}";
        await _dashboard.NavigateToAsync(normalised);
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see {string}")]
    public async Task ThenIShouldSee(string text)
    {
        await Page.WaitForSelectorAsync(
            $"text={text}",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should be on the {string} page")]
    public async Task ThenIShouldBeOnThePage(string path)
    {
        var normalised = path.StartsWith('/') ? path : $"/{path}";
        var isOnPage = await _dashboard.IsOnPageAsync(normalised);
        if (!isOnPage)
        {
            throw new Exception(
                $"Expected to be on '{normalised}' but current URL is '{Page.Url}'.");
        }
    }

    [Then(@"I should see an error message {string}")]
    public async Task ThenIShouldSeeAnErrorMessage(string message)
    {
        await Page.WaitForSelectorAsync(
            $"[role='alert']:has-text('{message}'), " +
            $".error:has-text('{message}'), " +
            $".error-message:has-text('{message}')",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the page title should contain {string}")]
    public async Task ThenThePageTitleShouldContain(string expectedTitle)
    {
        var title = await Page.TitleAsync();
        if (!title.Contains(expectedTitle, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception(
                $"Expected page title to contain '{expectedTitle}' but was '{title}'.");
        }
    }
}
