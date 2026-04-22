using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Analytics;

[Binding]
public class AnalyticsStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;
    private readonly AnalyticsPage _analyticsPage;

    public AnalyticsStepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser       = browser;
        _dashboard     = dashboard;
        _analyticsPage = new AnalyticsPage(browser.Page);
    }

    private IPage Page => _browser.Page;

    // -------------------------------------------------------------------------
    // Background
    // -------------------------------------------------------------------------

    [Given(@"I am on the analytics page")]
    public async Task GivenIAmOnTheAnalyticsPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/analytics");

        var loaded = await _analyticsPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Analytics page container did not become visible after navigation.");
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I click the refresh button")]
    public async Task WhenIClickTheRefreshButton()
    {
        await _analyticsPage.RefreshAsync();
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see the analytics stats row")]
    public async Task ThenIShouldSeeTheAnalyticsStatsRow()
    {
        await Page.WaitForSelectorAsync(
            ".analytics-stats, .stats-section, [data-stats]",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should see tracked experiments count")]
    public async Task ThenIShouldSeeTrackedExperimentsCount()
    {
        var stats = await _analyticsPage.GetStatsAsync();
        if (stats.Tracked < 0)
            throw new Exception($"Expected a non-negative tracked experiments count but got {stats.Tracked}.");

        // Verify there is a visible element containing a tracked-count indicator
        await Page.WaitForSelectorAsync(
            "[data-stat='tracked'], .tracked-count, :text('Tracked')",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should see total selections count")]
    public async Task ThenIShouldSeeTotalSelectionsCount()
    {
        await Page.WaitForSelectorAsync(
            "[data-stat='total'], .total-count, :text('Total')",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should see the audit log table")]
    public async Task ThenIShouldSeeTheAuditLogTable()
    {
        await Page.WaitForSelectorAsync(
            ".audit-log, table[data-audit], [data-audit-log]",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the audit log should have entries with timestamps")]
    public async Task ThenTheAuditLogShouldHaveEntriesWithTimestamps()
    {
        var entries = await _analyticsPage.GetAuditLogEntriesAsync();

        if (entries.Count == 0)
            throw new Exception("Expected the audit log to contain at least one entry but it was empty.");

        // Each entry should contain something that looks like a timestamp (digit sequences
        // with separators, or an ISO-8601 fragment).
        var timestampPattern = new System.Text.RegularExpressions.Regex(
            @"\d{1,4}[-/]\d{1,2}[-/]\d{1,4}|\d{2}:\d{2}",
            System.Text.RegularExpressions.RegexOptions.None);

        for (var i = 0; i < entries.Count; i++)
        {
            if (!timestampPattern.IsMatch(entries[i]))
                throw new Exception(
                    $"Audit log entry {i + 1} does not appear to contain a timestamp. " +
                    $"Entry text: '{entries[i]}'");
        }
    }

    [Then(@"the analytics data should reload")]
    public async Task ThenTheAnalyticsDataShouldReload()
    {
        // After refresh, the page should settle back to network-idle with the
        // stats section still visible.
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var loaded = await _analyticsPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Analytics page did not reload successfully after refresh.");
    }

    [Then(@"I should see variant distribution charts")]
    public async Task ThenIShouldSeeVariantDistributionCharts()
    {
        await Page.WaitForSelectorAsync(
            ".variant-distribution, .distribution-chart, canvas[data-chart], " +
            "[data-variant-distribution], .chart-container",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }
}
