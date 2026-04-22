using ExperimentFramework.E2E.Tests.Drivers;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions;

[Binding]
public class DocsScreenshotStepDefinitions
{
    private readonly DashboardDriver _dashboardDriver;
    private readonly ScenarioContext _scenarioContext;

    // Tests run from bin/Debug/net10.0 inside the test project directory.
    // Walk up five levels to reach the repo root:
    // net10.0 -> Debug -> bin -> ExperimentFramework.E2E.Tests -> tests -> repo-root
    private static readonly string RepoRoot = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));

    public DocsScreenshotStepDefinitions(
        DashboardDriver dashboardDriver,
        ScenarioContext scenarioContext)
    {
        _dashboardDriver = dashboardDriver;
        _scenarioContext = scenarioContext;
    }

    /// <summary>
    /// Captures a PNG screenshot to docs/images/screenshots/{area}/{name}.png.
    /// The {area} is set by DocsScreenshotHooks from the @screenshot-area:{area} tag.
    /// </summary>
    [When(@"I capture screenshot ""([^""]+)""")]
    public async Task WhenICaptureScreenshot(string name)
    {
        var area = _scenarioContext.TryGetValue<string>("DocsScreenshotArea", out var a)
            ? a
            : "misc";

        var filePath = Path.Combine(RepoRoot, "docs", "images", "screenshots", area, $"{name}.png");
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        await _dashboardDriver.TakeScreenshotAsync(filePath);
    }
}
