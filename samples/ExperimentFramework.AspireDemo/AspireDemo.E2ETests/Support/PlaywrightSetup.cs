using Microsoft.Playwright;

namespace AspireDemo.E2ETests.Support;

/// <summary>
/// One-time Playwright browser installation check.
/// </summary>
public static class PlaywrightSetup
{
    private static bool _installed;

    public static Task EnsurePlaywrightAsync()
    {
        if (_installed)
            return Task.CompletedTask;

        var exitCode = Microsoft.Playwright.Program.Main(["install", "--with-deps"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException(
                $"Playwright browser installation failed with exit code {exitCode}. " +
                "Run 'pwsh bin/Debug/net10.0/playwright.ps1 install' manually.");
        }

        _installed = true;
        return Task.CompletedTask;
    }
}
