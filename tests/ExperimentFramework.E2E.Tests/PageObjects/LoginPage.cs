using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the login page at <c>/Account/Login</c>.
/// </summary>
public class LoginPage
{
    private readonly IPage _page;

    private ILocator EmailInput      => _page.Locator("input[name='email']");
    private ILocator PasswordInput   => _page.Locator("input[name='password']");
    private ILocator RememberMeCheck => _page.Locator("input[name='RememberMe']");
    private ILocator SubmitButton    => _page.Locator("button[type='submit']");
    private ILocator ErrorMessage    => _page.Locator(".validation-summary-errors, .text-danger, [data-error]");

    public LoginPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the login page is ready by checking the submit button is visible.</summary>
    public async Task<bool> IsLoadedAsync()
    {
        try
        {
            await SubmitButton.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>Fills the email input field.</summary>
    public Task FillEmailAsync(string email) =>
        EmailInput.FillAsync(email);

    /// <summary>Fills the password input field.</summary>
    public Task FillPasswordAsync(string password) =>
        PasswordInput.FillAsync(password);

    /// <summary>Clicks the submit button to submit the login form.</summary>
    public Task SubmitAsync() =>
        SubmitButton.ClickAsync();

    /// <summary>Convenience method: fills credentials and submits in one call.</summary>
    public async Task LoginAsync(string email, string password)
    {
        await FillEmailAsync(email);
        await FillPasswordAsync(password);
        await SubmitAsync();
    }

    /// <summary>Returns the text of the first visible validation/error message, or null if none.</summary>
    public async Task<string?> GetErrorMessageAsync()
    {
        var count = await ErrorMessage.CountAsync();
        if (count == 0)
            return null;

        return await ErrorMessage.First.TextContentAsync();
    }
}
