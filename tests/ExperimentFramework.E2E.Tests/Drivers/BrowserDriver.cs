using ExperimentFramework.E2E.Tests.Support;
using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.Drivers;

/// <summary>
/// Manages the Playwright browser and browser-context lifecycle for a single scenario.
/// Registered as scoped (per-scenario) in Reqnroll DI.
/// Each scenario gets a fresh browser context with isolated cookies and storage.
/// </summary>
public sealed class BrowserDriver : IAsyncDisposable
{
    private readonly TestConfiguration _config;
    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private IBrowserContext? _context;
    private IPage? _page;
    private bool _disposed;

    public BrowserDriver(TestConfiguration config)
    {
        _config = config;
    }

    /// <summary>
    /// The current page for the scenario. Initialised on first access via <see cref="InitializeAsync"/>.
    /// </summary>
    public IPage Page => _page ?? throw new InvalidOperationException(
        "BrowserDriver has not been initialised. Call InitializeAsync() first (ScenarioHooks does this automatically).");

    /// <summary>
    /// Creates a fresh Playwright instance, browser, and browser context.
    /// Called by <c>ScenarioHooks.BeforeScenario</c>.
    /// </summary>
    public async Task InitializeAsync()
    {
        _playwright = await Playwright.CreateAsync();

        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = _config.Headless,
            SlowMo   = _config.SlowMo
        });

        _context = await _browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize             = new ViewportSize { Width = 1920, Height = 1080 },
            IgnoreHTTPSErrors        = true,
            RecordVideoDir           = null   // Enable per-scenario if you want video artifacts
        });

        _context.SetDefaultTimeout(_config.DefaultTimeoutMs);
        _context.SetDefaultNavigationTimeout(_config.DefaultTimeoutMs);

        _page = await _context.NewPageAsync();
    }

    /// <summary>
    /// Closes the browser context and browser, then disposes the Playwright instance.
    /// Called automatically by Reqnroll DI when the scenario scope ends.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_context is not null)
            await _context.CloseAsync();

        if (_browser is not null)
            await _browser.CloseAsync();

        _playwright?.Dispose();
    }
}
