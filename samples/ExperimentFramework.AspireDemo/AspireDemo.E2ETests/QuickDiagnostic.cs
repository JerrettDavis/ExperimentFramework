using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace AspireDemo.E2ETests;

[TestFixture]
public class QuickDiagnostic : PageTest
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
    public async Task CaptureErrorDetails()
    {
        // Set up console capture
        var consoleMessages = new List<string>();
        Page.Console += (_, msg) => consoleMessages.Add($"[{msg.Type}] {msg.Text}");

        // Login
        await Page.GotoAsync($"{BaseUrl}/Account/Login");
        await Page.FillAsync("input[name='email']", "admin@experimentdemo.com");
        await Page.FillAsync("input[name='password']", "Admin123!");
        await Page.ClickAsync("button[type='submit']");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        Console.WriteLine($"After login URL: {Page.Url}");

        // Wait a bit for page to render
        await Task.Delay(3000);

        // Get raw HTML
        var html = await Page.ContentAsync();
        Console.WriteLine("=== RAW HTML ===");
        Console.WriteLine(html);
        Console.WriteLine("=== END HTML ===");

        // Get error message if present
        var errorMessage = await Page.Locator(".error-message").TextContentAsync();
        Console.WriteLine($"\nError message: '{errorMessage}'");

        // Get all h2 text
        var h2Count = await Page.Locator("h2").CountAsync();
        Console.WriteLine($"\nH2 count: {h2Count}");
        for (int i = 0; i < h2Count; i++)
        {
            var h2Text = await Page.Locator("h2").Nth(i).TextContentAsync();
            Console.WriteLine($"H2[{i}]: '{h2Text}'");
        }

        // Console messages
        Console.WriteLine("\n=== CONSOLE MESSAGES ===");
        foreach (var msg in consoleMessages)
        {
            Console.WriteLine(msg);
        }
    }
}
