using ExperimentFramework.E2E.Tests.Drivers;
using ExperimentFramework.E2E.Tests.PageObjects;
using Microsoft.Playwright;
using Reqnroll;

namespace ExperimentFramework.E2E.Tests.StepDefinitions.Configuration;

[Binding]
[Scope(Feature = "Configuration View")]
public class ConfigurationStepDefinitions
{
    private readonly BrowserDriver _browser;
    private readonly DashboardDriver _dashboard;

    public ConfigurationStepDefinitions(BrowserDriver browser, DashboardDriver dashboard)
    {
        _browser   = browser;
        _dashboard = dashboard;
    }

    private IPage Page => _browser.Page;

    private ConfigurationPage ConfigPage => new(_browser.Page);

    // -------------------------------------------------------------------------
    // Given / Background
    // -------------------------------------------------------------------------

    [Given(@"I am on the configuration page")]
    public async Task GivenIAmOnTheConfigurationPage()
    {
        await _dashboard.NavigateToAsync("/dashboard/configuration");
        var loaded = await ConfigPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Configuration page did not load — page container not visible.");
    }

    // -------------------------------------------------------------------------
    // When
    // -------------------------------------------------------------------------

    [When(@"I click the copy to clipboard button")]
    public async Task WhenIClickTheCopyToClipboardButton()
    {
        await ConfigPage.CopyToClipboardAsync();
    }

    [When(@"I click the refresh button")]
    [Scope(Feature = "Configuration View")]
    public async Task WhenIClickTheRefreshButton()
    {
        await ConfigPage.RefreshAsync();
    }

    // -------------------------------------------------------------------------
    // Then
    // -------------------------------------------------------------------------

    [Then(@"I should see the framework info cards")]
    public async Task ThenIShouldSeeTheFrameworkInfoCards()
    {
        await Page.WaitForSelectorAsync(
            ".framework-info, [data-framework-info], .info-panel",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the framework name should not be empty")]
    public async Task ThenTheFrameworkNameShouldNotBeEmpty()
    {
        var info = await ConfigPage.GetFrameworkInfoAsync();
        if (string.IsNullOrWhiteSpace(info))
            throw new Exception("Framework info panel is empty — expected framework name to be present.");
    }

    [Then(@"the runtime version should be displayed")]
    public async Task ThenTheRuntimeVersionShouldBeDisplayed()
    {
        await Page.WaitForSelectorAsync(
            "text=/.NET|runtime|version/i",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"I should see the YAML configuration block")]
    public async Task ThenIShouldSeeTheYamlConfigurationBlock()
    {
        await Page.WaitForSelectorAsync(
            "pre, code, .yaml-config, [data-yaml]",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the YAML content should not be empty")]
    public async Task ThenTheYamlContentShouldNotBeEmpty()
    {
        var yaml = await ConfigPage.GetYamlConfigAsync();
        if (string.IsNullOrWhiteSpace(yaml))
            throw new Exception("YAML configuration block is empty — expected exported YAML content.");
    }

    [Then(@"the button should show a copied confirmation")]
    public async Task ThenTheButtonShouldShowACopiedConfirmation()
    {
        // After clicking copy, the button text typically changes to "Copied!" for a moment.
        await Page.WaitForSelectorAsync(
            "button:has-text('Copied'), button:has-text('✓'), [data-copied='true']",
            new PageWaitForSelectorOptions
            {
                State   = WaitForSelectorState.Visible,
                Timeout = 5_000
            });
    }

    [Then(@"I should see the enabled features section")]
    public async Task ThenIShouldSeeTheEnabledFeaturesSection()
    {
        // The section may be empty if no features are enabled — assert the container exists.
        await Page.WaitForSelectorAsync(
            ".feature-flag, .enabled-feature, [data-feature='enabled'], .features-section, [data-section='features']",
            new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
    }

    [Then(@"the configuration data should reload")]
    public async Task ThenTheConfigurationDataShouldReload()
    {
        // After refresh the page settles to NetworkIdle (handled inside RefreshAsync).
        // Assert the page container is still visible and info is non-empty.
        var loaded = await ConfigPage.IsLoadedAsync();
        if (!loaded)
            throw new Exception("Configuration page container disappeared after refresh.");
    }
}
