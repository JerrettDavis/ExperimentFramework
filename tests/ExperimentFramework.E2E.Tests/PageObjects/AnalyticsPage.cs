using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the analytics page at <c>/dashboard/analytics</c>.
/// </summary>
public class AnalyticsPage
{
    private readonly IPage _page;

    private ILocator PageContainer   => _page.Locator(".analytics-container, [data-page='analytics'], main");
    private ILocator StatsSection    => _page.Locator(".analytics-stats, .stats-section, [data-stats]");
    private ILocator RefreshButton   => _page.Locator("button:has-text('Refresh'), button[data-action='refresh'], .refresh-btn");
    private ILocator AuditLogTable   => _page.Locator(".audit-log, table[data-audit], [data-audit-log]");
    private ILocator AuditLogRows    => AuditLogTable.Locator("tbody tr, .audit-entry, [data-audit-entry]");

    public AnalyticsPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the analytics page container is visible.</summary>
    public async Task<bool> IsLoadedAsync()
    {
        try
        {
            await PageContainer.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    /// <summary>
    /// Returns analytics summary statistics as a named tuple:
    /// Tracked, Total, AuditEntries, LastActivity.
    /// </summary>
    public async Task<(int Tracked, int Total, int AuditEntries, string LastActivity)> GetStatsAsync()
    {
        var text = await StatsSection.TextContentAsync() ?? string.Empty;
        return ParseStats(text);
    }

    /// <summary>Clicks the Refresh button and waits for the page to settle.</summary>
    public async Task RefreshAsync()
    {
        await RefreshButton.ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    /// <summary>Returns the text content of each row in the audit log table.</summary>
    public async Task<IReadOnlyList<string>> GetAuditLogEntriesAsync()
    {
        // In InteractiveServer mode (inherited from the Routes component), Blazor
        // re-runs OnInitializedAsync after the SignalR circuit connects, briefly
        // showing a loading/skeleton state before the real rows render.
        // Wait for at least one row to appear so we don't count during the
        // transient loading phase.
        try
        {
            await AuditLogRows.First.WaitForAsync(new LocatorWaitForOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 15_000,
            });
        }
        catch (TimeoutException)
        {
            // No rows appeared within the timeout — fall through and return empty.
        }

        var count = await AuditLogRows.CountAsync();
        var entries = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await AuditLogRows.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                entries.Add(text.Trim());
        }

        return entries;
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static (int Tracked, int Total, int AuditEntries, string LastActivity) ParseStats(string text)
    {
        static int ExtractInt(string src, string label)
        {
            var idx = src.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return 0;
            var slice = src[..idx].TrimEnd();
            var numStart = slice.Length - 1;
            while (numStart >= 0 && char.IsDigit(slice[numStart]))
                numStart--;
            return int.TryParse(slice[(numStart + 1)..], out var n) ? n : 0;
        }

        // Last activity is freeform text — try to extract it heuristically
        var lastActivity = string.Empty;
        var lastIdx = text.IndexOf("Last Activity", StringComparison.OrdinalIgnoreCase);
        if (lastIdx >= 0)
        {
            lastActivity = text[(lastIdx + "Last Activity".Length)..].Trim().Split('\n')[0].Trim(' ', ':');
        }

        return (
            Tracked:       ExtractInt(text, "Tracked"),
            Total:         ExtractInt(text, "Total"),
            AuditEntries:  ExtractInt(text, "Audit"),
            LastActivity:  lastActivity
        );
    }
}
