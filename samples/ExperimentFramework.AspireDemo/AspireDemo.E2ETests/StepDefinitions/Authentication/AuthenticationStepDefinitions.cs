using AspireDemo.E2ETests.Drivers;
using AspireDemo.E2ETests.Support;
using Microsoft.Playwright;
using Reqnroll;

namespace AspireDemo.E2ETests.StepDefinitions.Authentication;

[Binding]
public class AuthenticationStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly WebDriver _webDriver;
    private readonly TestConfiguration _config;

    public AuthenticationStepDefinitions(
        BrowserDriver browser,
        WebDriver webDriver,
        TestConfiguration config)
    {
        _browser   = browser;
        _webDriver = webDriver;
        _config    = config;
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Given
    // -------------------------------------------------------------------------

    [Given(@"I am not logged in")]
    public Task GivenIAmNotLoggedIn()
    {
        // Fresh browser context (provided by ScenarioHooks) starts unauthenticated.
        return Task.CompletedTask;
    }

    [Given(@"I am on the login page")]
    public async Task GivenIAmOnTheLoginPage()
    {
        await _webDriver.NavigateToAsync("/Account/Login");
        // Verify submit button is present so we know the page rendered
        await Page.WaitForSelectorAsync(
            "button[type='submit'], input[type='submit']",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I navigate to {string}")]
    public async Task WhenINavigateTo(string path)
    {
        await _webDriver.NavigateToAsync(path);
    }

    [When(@"I log in as {string}")]
    public async Task WhenILogInAs(string role)
    {
        var user = TestUsers.GetByRole(role);
        await Page.FillAsync("input[name='Email'], input[name='email'], input[type='email']", user.Email);
        await Page.FillAsync("input[name='Password'], input[name='password'], input[type='password']", user.Password);
        await Page.ClickAsync("button[type='submit'], input[type='submit']");
    }

    [When(@"I log in with email {string} and password {string}")]
    public async Task WhenILogInWithEmailAndPassword(string email, string password)
    {
        await Page.FillAsync("input[name='Email'], input[name='email'], input[type='email']", email);
        await Page.FillAsync("input[name='Password'], input[name='password'], input[type='password']", password);
        await Page.ClickAsync("button[type='submit'], input[type='submit']");
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should be redirected to the login page")]
    public async Task ThenIShouldBeRedirectedToTheLoginPage()
    {
        // The web app redirects unauthenticated users to /Account/Login
        await Page.WaitForURLAsync("**/Account/Login**");
        await Page.WaitForSelectorAsync(
            "button[type='submit'], input[type='submit']",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should be on the dashboard home page")]
    public async Task ThenIShouldBeOnTheDashboardHomePage()
    {
        await Page.WaitForURLAsync("**/dashboard**", new PageWaitForURLOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await _webDriver.WaitForDashboardLoadedAsync();
        var url = Page.Url;
        if (!url.Contains("/dashboard", StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Expected to be on the dashboard but current URL is '{url}'.");
    }

    [Then(@"I should see a login error message")]
    public async Task ThenIShouldSeeALoginErrorMessage()
    {
        // After a failed login attempt the page stays at /Account/Login and shows an error.
        // The Login.cshtml.cs sets ErrorMessage = "Invalid email or password."
        await Page.WaitForSelectorAsync(
            ".alert-error, .text-danger, .validation-summary-errors, [data-valmsg-summary], p:has-text('Invalid')",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the page should load without errors")]
    public async Task ThenThePageShouldLoadWithoutErrors()
    {
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        // Verify no hard error page — just check body has meaningful content
        var bodyText = await Page.Locator("body").TextContentAsync();
        if (string.IsNullOrWhiteSpace(bodyText))
            throw new Exception("Page body is empty — possible render failure.");
    }
}
