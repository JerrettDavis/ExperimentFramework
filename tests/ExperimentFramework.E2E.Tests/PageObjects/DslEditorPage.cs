using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the DSL editor page at <c>/dashboard/dsl</c>.
/// Uses a Monaco editor loaded from CDN — requires waiting for editor initialization.
/// </summary>
public class DslEditorPage
{
    private readonly IPage _page;

    private ILocator PageContainer    => _page.Locator(".dsl-editor-container, [data-page='dsl'], main");
    private ILocator MonacoEditor     => _page.Locator(".monaco-editor, #monaco-editor-container, [data-editor='monaco']");
    private ILocator ValidateButton   => _page.Locator("button:has-text('Validate'), button[data-action='validate']");
    private ILocator ApplyButton      => _page.Locator("button:has-text('Apply'), button[data-action='apply']");
    private ILocator CancelButton     => _page.Locator("button:has-text('Cancel'), button[data-action='cancel']");
    private ILocator ConfirmButton    => _page.Locator("button:has-text('Confirm'), button[data-action='confirm'], .confirm-btn");
    private ILocator LoadCurrentButton => _page.Locator("button:has-text('Load Current'), button[data-action='load-current']");
    private ILocator ClearValidationButton => _page.Locator("button:has-text('Clear'), button[data-action='clear-validation']");
    private ILocator StatusBadge      => _page.Locator(".status-badge, [data-status-badge], .badge");
    private ILocator ValidationErrors => _page.Locator(".validation-error, .error-list li, [data-validation-error]");

    public DslEditorPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the DSL editor page container is visible.</summary>
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
    /// Waits for the Monaco editor to finish loading from CDN.
    /// Monaco signals readiness by adding the <c>.monaco-editor</c> class to the container
    /// and exposing <c>window.monaco</c>.
    /// </summary>
    public async Task WaitForEditorLoadedAsync()
    {
        // Wait for the Monaco DOM node to appear
        await MonacoEditor.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });

        // Wait until the monaco global is available in the JS context
        await _page.WaitForFunctionAsync("() => typeof window.monaco !== 'undefined' && window.monaco.editor !== undefined");
    }

    /// <summary>Replaces the entire editor content with <paramref name="yaml"/>.</summary>
    public async Task SetEditorContentAsync(string yaml)
    {
        // Use Monaco's JavaScript API to set value reliably, bypassing clipboard quirks
        var escaped = System.Text.Json.JsonSerializer.Serialize(yaml);
        await _page.EvaluateAsync($@"
            (function() {{
                var editor = window.monaco?.editor?.getEditors?.()?.[0];
                if (editor) {{
                    editor.setValue({escaped});
                }}
            }})()
        ");
    }

    /// <summary>Returns the current content of the Monaco editor.</summary>
    public async Task<string> GetEditorContentAsync()
    {
        var value = await _page.EvaluateAsync<string?>(@"
            (function() {
                var editor = window.monaco?.editor?.getEditors?.()?.[0];
                return editor ? editor.getValue() : null;
            })()
        ");
        return value ?? string.Empty;
    }

    /// <summary>Clicks the Validate button.</summary>
    public Task ValidateAsync() =>
        ValidateButton.ClickAsync();

    /// <summary>Returns text content of all visible validation error messages.</summary>
    public async Task<IReadOnlyList<string>> GetValidationErrorsAsync()
    {
        var count = await ValidationErrors.CountAsync();
        var errors = new List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var text = await ValidationErrors.Nth(i).TextContentAsync();
            if (!string.IsNullOrWhiteSpace(text))
                errors.Add(text.Trim());
        }

        return errors;
    }

    /// <summary>Clicks the Apply Changes button (opens confirmation dialog).</summary>
    public Task ApplyChangesAsync() =>
        ApplyButton.ClickAsync();

    /// <summary>Confirms the apply action in the confirmation dialog.</summary>
    public Task ConfirmApplyAsync() =>
        ConfirmButton.ClickAsync();

    /// <summary>Cancels the apply action in the confirmation dialog.</summary>
    public Task CancelApplyAsync() =>
        CancelButton.ClickAsync();

    /// <summary>Clicks the Load Current button to reload the live configuration into the editor.</summary>
    public async Task LoadCurrentAsync()
    {
        await LoadCurrentButton.ClickAsync();
        await WaitForEditorLoadedAsync();
    }

    /// <summary>Clicks the Clear Validation button to dismiss validation results.</summary>
    public Task ClearValidationAsync() =>
        ClearValidationButton.ClickAsync();

    /// <summary>Returns the text content of the status badge.</summary>
    public async Task<string> GetStatusBadgeTextAsync() =>
        (await StatusBadge.First.TextContentAsync() ?? string.Empty).Trim();
}
