using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace AspireDemo.E2ETests;

[TestFixture]
public class LoginDiagnostic : PageTest
{
    private const string BaseUrl = "https://localhost:7201";

    public override BrowserNewContextOptions ContextOptions()
    {
        return new BrowserNewContextOptions()
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        };
    }

    [Test]
    public async Task DiagnoseLogin()
    {
        // Capture console messages
        Page.Console += (_, msg) =>
        {
            Console.WriteLine($"[BROWSER CONSOLE {msg.Type}] {msg.Text}");
        };

        // Capture page errors
        Page.PageError += (_, exception) =>
        {
            Console.WriteLine($"[BROWSER ERROR] {exception}");
        };

        // Capture requests
        Page.Request += (_, request) =>
        {
            Console.WriteLine($"[BROWSER REQUEST] {request.Method} {request.Url}");
        };

        // Capture responses
        Page.Response += (_, response) =>
        {
            Console.WriteLine($"[BROWSER RESPONSE] {response.Status} {response.Url}");
        };

        // Go to login page
        Console.WriteLine("========== NAVIGATING TO LOGIN PAGE ==========");
        await Page.GotoAsync($"{BaseUrl}/Account/Login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Console.WriteLine($"Initial URL: {Page.Url}");

        // Take screenshot of login page
        await Page.ScreenshotAsync(new() { Path = "diagnostic_login_page.png", FullPage = true });

        Console.WriteLine("========== CLICKING ADMIN USER CARD ==========");

        // Click the Admin demo user card (which is a submit button in a form)
        await Page.Locator("button.user-card").First.ClickAsync();

        // Wait a bit for any async processing
        await Task.Delay(2000);

        Console.WriteLine($"URL after click: {Page.Url}");

        // Wait for either success or error
        try
        {
            await Page.WaitForURLAsync("**/dashboard/**", new() { Timeout = 5000 });
            Console.WriteLine($"Successfully navigated to: {Page.Url}");
            await Page.ScreenshotAsync(new() { Path = "diagnostic_dashboard.png", FullPage = true });
        }
        catch (TimeoutException)
        {
            Console.WriteLine($"Did not navigate to dashboard. Current URL: {Page.Url}");
            await Page.ScreenshotAsync(new() { Path = "diagnostic_after_submit.png", FullPage = true });

            // Get page content to see if there's an error
            var html = await Page.ContentAsync();
            Console.WriteLine("===== PAGE CONTENT =====");
            Console.WriteLine(html.Substring(0, Math.Min(1000, html.Length)));
        }
    }
}
