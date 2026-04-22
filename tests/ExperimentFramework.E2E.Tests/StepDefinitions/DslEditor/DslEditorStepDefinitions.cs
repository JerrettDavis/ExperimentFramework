using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.DslEditor;

[Binding]
public class DslEditorStepDefinitions
{
    private const string ValidExperimentYaml = """
        experiments:
          - name: test-experiment
            service: ITestService
            variants:
              - key: control
                type: ControlImpl
              - key: treatment
                type: TreatmentImpl
        """;

    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;

    public DslEditorStepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser   = browser;
        _dashboard = dashboard;
    }

    private IPage Page => _browser.Page;

    private DslEditorPage DslPage => new(_browser.Page);

    // -------------------------------------------------------------------------
    // Given / Background
    // -------------------------------------------------------------------------

    [Given(@"I am on the DSL editor page")]
    public async Task GivenIAmOnTheDslEditorPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/dsl");
        var dslPage = DslPage;
        var loaded  = await dslPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("DSL editor page did not load — page container not visible.");

        // Monaco loads from CDN; wait for full initialization before interacting.
        await dslPage.WaitForEditorLoadedAsync();
    }

    [Given(@"the editor contains valid experiment YAML")]
    public async Task GivenTheEditorContainsValidExperimentYaml()
    {
        var dslPage = DslPage;
        await dslPage.WaitForEditorLoadedAsync();
        await dslPage.SetEditorContentAsync(ValidExperimentYaml);
    }

    [Given(@"the editor contains invalid YAML {string}")]
    public async Task GivenTheEditorContainsInvalidYaml(string invalidYaml)
    {
        var dslPage = DslPage;
        await dslPage.WaitForEditorLoadedAsync();
        await dslPage.SetEditorContentAsync(invalidYaml);
    }

    [Given(@"the YAML has been validated successfully")]
    public async Task GivenTheYamlHasBeenValidatedSuccessfully()
    {
        var dslPage = DslPage;
        await dslPage.ValidateAsync();

        // Wait for the "Valid" status badge to appear
        await Page.WaitForSelectorAsync(
            ".status-badge:has-text('Valid'), [data-status-badge]:has-text('Valid'), .badge:has-text('Valid')",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Given(@"validation results are shown")]
    public async Task GivenValidationResultsAreShown()
    {
        var dslPage = DslPage;
        await dslPage.WaitForEditorLoadedAsync();
        await dslPage.SetEditorContentAsync(ValidExperimentYaml);
        await dslPage.ValidateAsync();

        // Wait for any validation result element (valid or error)
        await Page.WaitForSelectorAsync(
            ".status-badge, [data-status-badge], .badge, .validation-error, .error-list li",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I click the load current button")]
    public async Task WhenIClickTheLoadCurrentButton()
    {
        await DslPage.LoadCurrentAsync();
    }

    [When(@"I click the validate button")]
    public async Task WhenIClickTheValidateButton()
    {
        await DslPage.ValidateAsync();
        await _browser.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [When(@"I click the apply changes button")]
    public async Task WhenIClickTheApplyChangesButton()
    {
        await DslPage.ApplyChangesAsync();
    }

    [When(@"I cancel the apply")]
    public async Task WhenICancelTheApply()
    {
        await DslPage.CancelApplyAsync();
    }

    [When(@"I click the clear button")]
    public async Task WhenIClickTheClearButton()
    {
        await DslPage.ClearValidationAsync();
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"the Monaco editor should be initialized")]
    public async Task ThenTheMonacoEditorShouldBeInitialized()
    {
        await DslPage.WaitForEditorLoadedAsync();
        await Page.WaitForSelectorAsync(
            ".monaco-editor, #monaco-editor-container, [data-editor='monaco']",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the editor should contain YAML content")]
    public async Task ThenTheEditorShouldContainYamlContent()
    {
        var content = await DslPage.GetEditorContentAsync();
        if (string.IsNullOrWhiteSpace(content))
            throw new Exception("Monaco editor content is empty — expected YAML content to be present.");
    }

    [Then(@"the editor content should be updated")]
    public async Task ThenTheEditorContentShouldBeUpdated()
    {
        // After LoadCurrentAsync the editor has fresh content from the live configuration.
        var content = await DslPage.GetEditorContentAsync();
        if (string.IsNullOrWhiteSpace(content))
            throw new Exception("Editor content is empty after Load Current — expected refreshed YAML.");
    }

    [Then(@"the validation status should show {string}")]
    public async Task ThenTheValidationStatusShouldShow(string expectedStatus)
    {
        await Page.WaitForSelectorAsync(
            $".status-badge:has-text('{expectedStatus}'), " +
            $"[data-status-badge]:has-text('{expectedStatus}'), " +
            $".badge:has-text('{expectedStatus}')",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

        var actual = await DslPage.GetStatusBadgeTextAsync();
        if (!actual.Contains(expectedStatus, StringComparison.OrdinalIgnoreCase))
            throw new Exception($"Expected validation status '{expectedStatus}' but found '{actual}'.");
    }

    [Then(@"I should see validation errors")]
    public async Task ThenIShouldSeeValidationErrors()
    {
        await Page.WaitForSelectorAsync(
            ".validation-error, .error-list li, [data-validation-error]",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });

        var errors = await DslPage.GetValidationErrorsAsync();
        if (errors.Count == 0)
            throw new Exception("No validation errors were displayed — expected at least one error for invalid YAML.");
    }

    [Then(@"I should see the apply confirmation modal")]
    public async Task ThenIShouldSeeTheApplyConfirmationModal()
    {
        await Page.WaitForSelectorAsync(
            ".modal, [role='dialog'], .confirmation-dialog, [data-modal]",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the confirmation modal should close")]
    public async Task ThenTheConfirmationModalShouldClose()
    {
        await Page.WaitForSelectorAsync(
            ".modal, [role='dialog'], .confirmation-dialog, [data-modal]",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Hidden });
    }

    [Then(@"the validation results should be cleared")]
    public async Task ThenTheValidationResultsShouldBeCleared()
    {
        // After clearing, status badge and error list should be gone or hidden.
        try
        {
            await Page.WaitForSelectorAsync(
                ".status-badge, .validation-error, .error-list li",
                new PageWaitForSelectorOptions
                {
                    State   = WaitForSelectorState.Hidden,
                    Timeout = 5_000
                });
        }
        catch (TimeoutException)
        {
            throw new Exception("Validation results were not cleared after clicking the clear button.");
        }
    }
}
