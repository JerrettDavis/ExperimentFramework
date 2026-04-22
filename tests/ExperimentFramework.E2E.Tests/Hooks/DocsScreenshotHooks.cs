using ExperimentFramework.E2E.Tests.Drivers;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.Hooks;

[Binding]
public class DocsScreenshotHooks
{
    private readonly BrowserDriver _browserDriver;
    private readonly ScenarioContext _scenarioContext;

    private const string DeterminismCss =
        "*, *::before, *::after { " +
        "  animation: none !important; " +
        "  transition: none !important; " +
        "  caret-color: transparent !important; " +
        "}";

    public DocsScreenshotHooks(BrowserDriver browserDriver, ScenarioContext scenarioContext)
    {
        _browserDriver   = browserDriver;
        _scenarioContext = scenarioContext;
    }

    /// <summary>
    /// Runs after the default BeforeScenario (Order=0) and before role-specific
    /// auto-login hooks (Order=10). Sets 1280x800 viewport, injects determinism
    /// CSS, and stores the screenshot area from @screenshot-area:{area}.
    /// </summary>
    [BeforeScenario("docs-screenshot", Order = 5)]
    public async Task ConfigureDocsScreenshotScenario()
    {
        await _browserDriver.Page.SetViewportSizeAsync(1280, 800);

        // Inject CSS on every new document load
        await _browserDriver.Page.AddInitScriptAsync(
            "document.addEventListener('DOMContentLoaded', () => {" +
            "  const s = document.createElement('style');" +
            "  s.textContent = `" + DeterminismCss.Replace("`", "\\`") + "`;" +
            "  document.head.appendChild(s);" +
            "});");

        // Inject immediately for the currently loaded page (if any)
        await _browserDriver.Page.AddStyleTagAsync(new PageAddStyleTagOptions
        {
            Content = DeterminismCss
        });

        // Read area tag. Reverse so scenario-level tags override feature-level.
        var areaTag = _scenarioContext.ScenarioInfo.Tags
            .Reverse()
            .FirstOrDefault(t => t.StartsWith("screenshot-area:", StringComparison.OrdinalIgnoreCase));

        var area = areaTag is not null
            ? areaTag["screenshot-area:".Length..]
            : "misc";

        _scenarioContext["DocsScreenshotArea"] = area;
    }
}
