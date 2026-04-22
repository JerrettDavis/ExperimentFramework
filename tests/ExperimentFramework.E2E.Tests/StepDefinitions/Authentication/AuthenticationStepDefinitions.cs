using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using ExperimentFramework.E2E.Tests.Support;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Authentication;

[Binding]
public class AuthenticationStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly TestConfiguration _config;

    public AuthenticationStepDefinitions(
        BrowserDriver browser,
        DashboardDriver dashboard,
        TestConfiguration config)
    {
        _browser   = browser;
        _dashboard = dashboard;
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
        await _dashboard.NavigateToAsync("/login");
        var loginPage = new LoginPage(Page);
        var loaded = await loginPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Login page did not load — submit button not visible.");
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I log in as {string}")]
    public async Task WhenILogInAs(string role)
    {
        var user = TestUsers.GetByRole(role);
        var loginPage = new LoginPage(Page);
        await loginPage.LoginAsync(user.Email, user.Password);
    }

    [When(@"I log in with email {string} and password {string}")]
    public async Task WhenILogInWithEmailAndPassword(string email, string password)
    {
        var loginPage = new LoginPage(Page);
        await loginPage.LoginAsync(email, password);
    }

    [When(@"I log out")]
    public async Task WhenILogOut()
    {
        // Click a logout button/link in the nav or header area.
        await Page.ClickAsync(
            "a[href*='logout'], a[href*='signout'], button:has-text('Log out'), " +
            "button:has-text('Sign out'), [data-action='logout']");
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should be redirected to the login page")]
    public async Task ThenIShouldBeRedirectedToTheLoginPage()
    {
        await Page.WaitForURLAsync("**/login**");
        var loginPage = new LoginPage(Page);
        var loaded = await loginPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Expected the login page after redirect but submit button was not visible.");
    }

    [Then(@"I should be on the dashboard home page")]
    public async Task ThenIShouldBeOnTheDashboardHomePage()
    {
        await _dashboard.WaitForDashboardLoadedAsync();
        var isOnDashboard = await _dashboard.IsOnPageAsync("/dashboard");
        if (!isOnDashboard)
        {
            throw new Exception(
                $"Expected to be on the dashboard home page but current URL is '{Page.Url}'.");
        }
    }

    [Then(@"I should be on the login page")]
    public async Task ThenIShouldBeOnTheLoginPage()
    {
        await Page.WaitForURLAsync("**/login**");
        var loginPage = new LoginPage(Page);
        var loaded = await loginPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Expected the login page but submit button was not visible.");
    }

    [Then(@"the remember me checkbox should be visible")]
    public async Task ThenTheRememberMeCheckboxShouldBeVisible()
    {
        var rememberMe = Page.Locator("input[name='RememberMe'], input[id='RememberMe'], input[type='checkbox'][name*='emember']");
        await rememberMe.First.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }
}
