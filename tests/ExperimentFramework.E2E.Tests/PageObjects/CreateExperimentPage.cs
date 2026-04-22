using Microsoft.Playwright;

namespace ExperimentFramework.E2E.Tests.PageObjects;

/// <summary>
/// Page Object Model for the create-experiment wizard at <c>/dashboard/create</c>.
/// The wizard is divided into 4 steps; each step group of methods corresponds to one step.
/// </summary>
public class CreateExperimentPage
{
    private readonly IPage _page;

    // Wizard chrome
    private ILocator PageContainer   => _page.Locator(".create-experiment, [data-page='create'], main");
    private ILocator StepIndicators  => _page.Locator(".step-indicator, .wizard-step, [data-step]");
    private ILocator NextButton      => _page.Locator("button:has-text('Next'), button[data-action='next']");
    private ILocator BackButton      => _page.Locator("button:has-text('Back'), button[data-action='back']");

    // -----------------------------------------------------------------------
    // Step 1: Basic info
    // -----------------------------------------------------------------------
    private ILocator NameInput          => _page.Locator("input[name='name'], input[id*='name' i][name='name']");
    private ILocator DisplayNameInput   => _page.Locator("input[name='displayName'], input[name='display_name'], input[id*='displayname' i]");
    private ILocator DescriptionInput   => _page.Locator("textarea[name='description'], input[name='description']");
    private ILocator CategorySelect     => _page.Locator("select[name='category'], [data-select='category']");

    // -----------------------------------------------------------------------
    // Step 2: Variants
    // -----------------------------------------------------------------------
    private ILocator ServiceInterfaceSelect => _page.Locator("select[name*='serviceInterface' i], select[name*='interface' i], [data-select='service-interface']");
    private ILocator ControlVariantKeyInput => _page.Locator("[data-variant='control'] input[name*='key' i], .control-variant input[name*='key' i]");
    private ILocator ControlVariantNameInput => _page.Locator("[data-variant='control'] input[name*='display' i], .control-variant input[name*='display' i]");
    private ILocator ControlVariantImplInput => _page.Locator("[data-variant='control'] input[name*='impl' i], .control-variant input[name*='impl' i]");
    private ILocator AddVariantButton   => _page.Locator("button:has-text('Add Variant'), button[data-action='add-variant']");
    private ILocator VariantList        => _page.Locator(".variant-item:not(.control-variant), [data-variant]:not([data-variant='control'])");

    // -----------------------------------------------------------------------
    // Step 3: Configuration
    // -----------------------------------------------------------------------
    private ILocator SelectionModeSelect => _page.Locator("select[name*='selectionMode' i], select[name*='selection_mode' i], [data-select='selection-mode']");
    private ILocator ConfigKeyInput      => _page.Locator("input[name*='configKey' i], input[name*='config_key' i], input[placeholder*='config key' i]");
    private ILocator ErrorPolicySelect   => _page.Locator("select[name*='errorPolicy' i], select[name*='error_policy' i], [data-select='error-policy']");

    // -----------------------------------------------------------------------
    // Step 4: Review & output
    // -----------------------------------------------------------------------
    private ILocator GeneratedYaml       => _page.Locator(".generated-yaml, [data-output='yaml'], pre.yaml");
    private ILocator GeneratedCSharp     => _page.Locator(".generated-csharp, [data-output='csharp'], pre.csharp");
    private ILocator CopyCodeButton      => _page.Locator("button:has-text('Copy'), button[data-action='copy']");
    private ILocator LoadIntoDemoButton  => _page.Locator("button:has-text('Load into Demo'), button[data-action='load-demo']");
    private ILocator StartOverButton     => _page.Locator("button:has-text('Start Over'), button[data-action='start-over']");

    public CreateExperimentPage(IPage page)
    {
        _page = page;
    }

    /// <summary>Verifies the create-experiment page container is visible.</summary>
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

    // =========================================================================
    // Step 1 — Basic info
    // =========================================================================

    /// <summary>Fills the experiment name field (Step 1).</summary>
    public Task FillNameAsync(string name) =>
        NameInput.FillAsync(name);

    /// <summary>Fills the display name field (Step 1).</summary>
    public Task FillDisplayNameAsync(string displayName) =>
        DisplayNameInput.FillAsync(displayName);

    /// <summary>Fills the description field (Step 1).</summary>
    public Task FillDescriptionAsync(string description) =>
        DescriptionInput.FillAsync(description);

    /// <summary>Selects a category from the category dropdown (Step 1).</summary>
    public Task SelectCategoryAsync(string category) =>
        CategorySelect.SelectOptionAsync(new SelectOptionValue { Label = category });

    // =========================================================================
    // Step 2 — Variants
    // =========================================================================

    /// <summary>Selects a service interface from the dropdown (Step 2).</summary>
    public Task SelectServiceInterfaceAsync(string interfaceName) =>
        ServiceInterfaceSelect.SelectOptionAsync(new SelectOptionValue { Label = interfaceName });

    /// <summary>Fills the control variant fields (Step 2).</summary>
    public async Task SetControlVariantAsync(string key, string displayName, string implType)
    {
        await ControlVariantKeyInput.FillAsync(key);
        await ControlVariantNameInput.FillAsync(displayName);
        await ControlVariantImplInput.FillAsync(implType);
    }

    /// <summary>Adds a new variant row and fills its key, display name, and implementation type (Step 2).</summary>
    public async Task AddVariantAsync(string key, string displayName, string implType)
    {
        await AddVariantButton.ClickAsync();

        // The newly added row will be the last in VariantList
        var newRow = VariantList.Last;
        await newRow.Locator("input[name*='key' i]").FillAsync(key);
        await newRow.Locator("input[name*='display' i]").FillAsync(displayName);
        await newRow.Locator("input[name*='impl' i]").FillAsync(implType);
    }

    /// <summary>Removes the non-control variant at zero-based <paramref name="index"/> (Step 2).</summary>
    public async Task RemoveVariantAsync(int index)
    {
        var row = VariantList.Nth(index);
        var removeBtn = row.Locator("button:has-text('Remove'), button[data-action='remove-variant']");
        await removeBtn.ClickAsync();
    }

    // =========================================================================
    // Step 3 — Configuration
    // =========================================================================

    /// <summary>Selects a selection mode from the dropdown (Step 3).</summary>
    public Task SelectSelectionModeAsync(string mode) =>
        SelectionModeSelect.SelectOptionAsync(new SelectOptionValue { Label = mode });

    /// <summary>Fills the configuration key input (Step 3).</summary>
    public Task SetConfigKeyAsync(string key) =>
        ConfigKeyInput.FillAsync(key);

    /// <summary>Selects an error policy from the dropdown (Step 3).</summary>
    public Task SelectErrorPolicyAsync(string policy) =>
        ErrorPolicySelect.SelectOptionAsync(new SelectOptionValue { Label = policy });

    // =========================================================================
    // Step 4 — Review
    // =========================================================================

    /// <summary>Returns the generated YAML text (Step 4).</summary>
    public async Task<string> GetGeneratedYamlAsync() =>
        (await GeneratedYaml.TextContentAsync() ?? string.Empty).Trim();

    /// <summary>Returns the generated C# code text (Step 4).</summary>
    public async Task<string> GetGeneratedCSharpAsync() =>
        (await GeneratedCSharp.TextContentAsync() ?? string.Empty).Trim();

    /// <summary>Clicks the Copy Code button (Step 4).</summary>
    public Task CopyCodeAsync() =>
        CopyCodeButton.ClickAsync();

    /// <summary>Clicks the Load into Demo button (Step 4).</summary>
    public Task LoadIntoDemoAsync() =>
        LoadIntoDemoButton.ClickAsync();

    /// <summary>Clicks the Start Over button to reset the wizard (Step 4).</summary>
    public Task StartOverAsync() =>
        StartOverButton.ClickAsync();

    // =========================================================================
    // Navigation helpers (all steps)
    // =========================================================================

    /// <summary>Advances to the next wizard step.</summary>
    public Task NextAsync() =>
        NextButton.ClickAsync();

    /// <summary>Returns to the previous wizard step.</summary>
    public Task BackAsync() =>
        BackButton.ClickAsync();

    /// <summary>Clicks the step indicator for the given 1-based step number.</summary>
    public async Task GoToStepAsync(int step)
    {
        var indicator = StepIndicators
            .Filter(new LocatorFilterOptions { HasText = step.ToString() });
        await indicator.First.ClickAsync();
    }

    /// <summary>
    /// Returns the current active step number (1-based), or 0 if it cannot be determined.
    /// </summary>
    public async Task<int> GetCurrentStepAsync()
    {
        var active = StepIndicators.Locator(".active, [aria-current='step'], [data-active='true']");
        var count = await active.CountAsync();
        if (count == 0) return 0;

        var text = await active.First.TextContentAsync() ?? string.Empty;
        return int.TryParse(text.Trim(), out var n) ? n : 0;
    }
}
