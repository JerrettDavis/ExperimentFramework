using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.Support;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.Hooks;

[Binding]
public class ScenarioHooks
{
    private readonly BrowserDriver _browserDriver;
    private readonly DashboardDriver _dashboardDriver;
    private readonly ScenarioContext _scenarioContext;

    public ScenarioHooks(
        BrowserDriver browserDriver,
        DashboardDriver dashboardDriver,
        ScenarioContext scenarioContext)
    {
        _browserDriver   = browserDriver;
        _dashboardDriver = dashboardDriver;
        _scenarioContext = scenarioContext;
    }

    // -------------------------------------------------------------------------
    // Lifecycle — all scenarios
    // -------------------------------------------------------------------------

    [BeforeScenario(Order = 0)]
    public async Task BeforeScenario()
    {
        await _browserDriver.InitializeAsync();
    }

    [AfterScenario(Order = int.MaxValue)]
    public async Task AfterScenario()
    {
        if (_scenarioContext.TestError is not null)
        {
            await TakeFailureScreenshotAsync();
        }

        await _browserDriver.DisposeAsync();
    }

    // -------------------------------------------------------------------------
    // Role-specific auto-login hooks
    // -------------------------------------------------------------------------

    /// <summary>Automatically logs in as Admin for scenarios tagged <c>@authenticated</c>.</summary>
    [BeforeScenario("authenticated", Order = 10)]
    public async Task BeforeAuthenticatedScenario()
    {
        await _dashboardDriver.LoginAsAdminAsync();
    }

    /// <summary>Automatically logs in as Admin for scenarios tagged <c>@admin</c>.</summary>
    [BeforeScenario("admin", Order = 10)]
    public async Task BeforeAdminScenario()
    {
        await _dashboardDriver.LoginAsAdminAsync();
    }

    /// <summary>Automatically logs in as Experimenter for scenarios tagged <c>@experimenter</c>.</summary>
    [BeforeScenario("experimenter", Order = 10)]
    public async Task BeforeExperimenterScenario()
    {
        await _dashboardDriver.LoginAsExperimenterAsync();
    }

    /// <summary>Automatically logs in as Viewer for scenarios tagged <c>@viewer</c>.</summary>
    [BeforeScenario("viewer", Order = 10)]
    public async Task BeforeViewerScenario()
    {
        await _dashboardDriver.LoginAsViewerAsync();
    }

    /// <summary>Automatically logs in as Analyst for scenarios tagged <c>@analyst</c>.</summary>
    [BeforeScenario("analyst", Order = 10)]
    public async Task BeforeAnalystScenario()
    {
        await _dashboardDriver.LoginAsAnalystAsync();
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task TakeFailureScreenshotAsync()
    {
        try
        {
            var screenshotsDir = Path.Combine(
                AppContext.BaseDirectory, "TestResults", "Screenshots");
            Directory.CreateDirectory(screenshotsDir);

            var safeName = string.Concat(
                _scenarioContext.ScenarioInfo.Title
                    .Split(Path.GetInvalidFileNameChars()))
                .Replace(' ', '_');

            var filePath = Path.Combine(
                screenshotsDir,
                $"{safeName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png");

            await _dashboardDriver.TakeScreenshotAsync(filePath);
        }
        catch
        {
            // Screenshot failure must never mask the original test error.
        }
    }
}
