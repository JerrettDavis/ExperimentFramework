using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.Support;

/// <summary>
/// One-time Playwright browser installation check.
/// Call <see cref="EnsurePlaywrightAsync"/> once before running any tests.
/// </summary>
public static class PlaywrightSetup
{
    private static bool _installed;

    public static Task EnsurePlaywrightAsync()
    {
        if (_installed)
            return Task.CompletedTask;

        // Microsoft.Playwright.Program.Main installs browsers when passed ["install"].
        // In CI, prefer running `playwright install` via the dotnet tool or npm beforehand.
        // This guard is a development convenience; it exits early if already installed.
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
