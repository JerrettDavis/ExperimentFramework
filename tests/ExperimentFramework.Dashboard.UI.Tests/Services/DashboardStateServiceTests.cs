using ExperimentFramework.Dashboard.UI.Services;

namespace ExperimentFramework.Dashboard.UI.Tests.Services;

/// <summary>
/// Unit tests for DashboardStateService.
/// These tests run without Blazor infrastructure — the service is pure C#.
/// </summary>
public sealed class DashboardStateServiceTests
{
    // ── Kill switch ───────────────────────────────────────────────────────────

    [Fact]
    public void GetKillSwitch_Default_ReturnsFalse()
    {
        var svc = new DashboardStateService();
        Assert.False(svc.GetKillSwitch("any-exp"));
    }

    [Fact]
    public void SetKillSwitch_True_ReturnsTrue()
    {
        var svc = new DashboardStateService();
        svc.SetKillSwitch("exp-a", true);
        Assert.True(svc.GetKillSwitch("exp-a"));
    }

    [Fact]
    public void SetKillSwitch_RaisesOnStateChanged()
    {
        var svc = new DashboardStateService();
        var raised = 0;
        svc.OnStateChanged += () => raised++;

        svc.SetKillSwitch("exp-a", true);

        Assert.Equal(1, raised);
    }

    [Fact]
    public void SetKillSwitch_RaisesOnKillSwitchChanged_WithExperimentName()
    {
        var svc = new DashboardStateService();
        string? capturedName = null;
        svc.OnKillSwitchChanged += name => capturedName = name;

        svc.SetKillSwitch("exp-b", true);

        Assert.Equal("exp-b", capturedName);
    }

    [Fact]
    public void ToggleKillSwitch_FlipsState()
    {
        var svc = new DashboardStateService();
        Assert.False(svc.GetKillSwitch("exp-a"));

        svc.ToggleKillSwitch("exp-a");
        Assert.True(svc.GetKillSwitch("exp-a"));

        svc.ToggleKillSwitch("exp-a");
        Assert.False(svc.GetKillSwitch("exp-a"));
    }

    [Fact]
    public void GetAllKillSwitches_ReflectsAllSetSwitches()
    {
        var svc = new DashboardStateService();
        svc.SetKillSwitch("exp-1", true);
        svc.SetKillSwitch("exp-2", false);

        var all = svc.GetAllKillSwitches();
        Assert.True(all["exp-1"]);
        Assert.False(all["exp-2"]);
    }

    // ── Expanded items ────────────────────────────────────────────────────────

    [Fact]
    public void IsExpanded_Default_ReturnsFalse()
    {
        var svc = new DashboardStateService();
        Assert.False(svc.IsExpanded("experiments", "exp-a"));
    }

    [Fact]
    public void SetExpanded_True_ReturnsTrue()
    {
        var svc = new DashboardStateService();
        svc.SetExpanded("experiments", "exp-a", true);
        Assert.True(svc.IsExpanded("experiments", "exp-a"));
    }

    [Fact]
    public void SetExpanded_FalseAfterTrue_ReturnsFalse()
    {
        var svc = new DashboardStateService();
        svc.SetExpanded("experiments", "exp-a", true);
        svc.SetExpanded("experiments", "exp-a", false);
        Assert.False(svc.IsExpanded("experiments", "exp-a"));
    }

    [Fact]
    public void ToggleExpanded_FlipsState()
    {
        var svc = new DashboardStateService();
        Assert.False(svc.IsExpanded("experiments", "exp-a"));
        svc.ToggleExpanded("experiments", "exp-a");
        Assert.True(svc.IsExpanded("experiments", "exp-a"));
        svc.ToggleExpanded("experiments", "exp-a");
        Assert.False(svc.IsExpanded("experiments", "exp-a"));
    }

    [Fact]
    public void SetExpanded_DifferentPages_AreIndependent()
    {
        var svc = new DashboardStateService();
        svc.SetExpanded("experiments", "exp-a", true);
        Assert.False(svc.IsExpanded("analytics", "exp-a"));
    }

    // ── Filter / search state ─────────────────────────────────────────────────

    [Fact]
    public void ExperimentFilterCategory_Default_IsNull()
    {
        var svc = new DashboardStateService();
        Assert.Null(svc.ExperimentFilterCategory);
    }

    [Fact]
    public void ExperimentFilterCategory_SetValue_RaisesStateChanged()
    {
        var svc = new DashboardStateService();
        var raised = 0;
        svc.OnStateChanged += () => raised++;

        svc.ExperimentFilterCategory = "Revenue";

        Assert.Equal("Revenue", svc.ExperimentFilterCategory);
        Assert.Equal(1, raised);
    }

    [Fact]
    public void ExperimentSearchQuery_Default_IsEmpty()
    {
        var svc = new DashboardStateService();
        Assert.Equal(string.Empty, svc.ExperimentSearchQuery);
    }

    [Fact]
    public void ExperimentSearchQuery_SetValue_RaisesStateChanged()
    {
        var svc = new DashboardStateService();
        var raised = 0;
        svc.OnStateChanged += () => raised++;

        svc.ExperimentSearchQuery = "checkout";

        Assert.Equal("checkout", svc.ExperimentSearchQuery);
        Assert.Equal(1, raised);
    }

    // ── Active variants ───────────────────────────────────────────────────────

    [Fact]
    public void GetActiveVariant_Default_ReturnsDefault()
    {
        var svc = new DashboardStateService();
        Assert.Equal("default", svc.GetActiveVariant("exp-a"));
    }

    [Fact]
    public void SetActiveVariant_RaisesOnVariantChanged()
    {
        var svc = new DashboardStateService();
        string? capturedName = null;
        svc.OnVariantChanged += name => capturedName = name;

        svc.SetActiveVariant("exp-a", "variant-b");

        Assert.Equal("exp-a", capturedName);
        Assert.Equal("variant-b", svc.GetActiveVariant("exp-a"));
    }

    [Fact]
    public void SyncVariantsFromApi_UpdatesAllVariants()
    {
        var svc = new DashboardStateService();
        // ExperimentInfo here is ExperimentFramework.Dashboard.UI.Services.ExperimentInfo
        var experiments = new List<global::ExperimentFramework.Dashboard.UI.Services.ExperimentInfo>
        {
            new() { Name = "exp-1", ActiveVariant = "variant-b" },
            new() { Name = "exp-2", ActiveVariant = "control" },
        };

        svc.SyncVariantsFromApi(experiments);

        Assert.Equal("variant-b", svc.GetActiveVariant("exp-1"));
        Assert.Equal("control", svc.GetActiveVariant("exp-2"));
    }

    // ── Kill switch sync ──────────────────────────────────────────────────────

    [Fact]
    public void SyncKillSwitchesFromApi_UpdatesKillSwitchStates()
    {
        var svc = new DashboardStateService();
        var statuses = new List<global::ExperimentFramework.Dashboard.UI.Services.KillSwitchStatus>
        {
            new() { Experiment = "exp-1", ExperimentDisabled = true },
            new() { Experiment = "exp-2", ExperimentDisabled = false },
        };

        svc.SyncKillSwitchesFromApi(statuses);

        Assert.True(svc.GetKillSwitch("exp-1"));
        Assert.False(svc.GetKillSwitch("exp-2"));
    }

    // ── Plugin implementation cache ───────────────────────────────────────────

    [Fact]
    public void GetActivePluginImplementation_Default_ReturnsNull()
    {
        var svc = new DashboardStateService();
        Assert.Null(svc.GetActivePluginImplementation("IMyPlugin"));
    }

    [Fact]
    public void SetActivePluginImplementation_StoresAndRetrievesImpl()
    {
        var svc = new DashboardStateService();
        var impl = new ActivePluginImplementation
        {
            PluginId = "my-plugin",
            Interface = "IMyPlugin",
            ImplementationType = "MyImpl"
        };

        svc.SetActivePluginImplementation("IMyPlugin", impl);

        var retrieved = svc.GetActivePluginImplementation("IMyPlugin");
        Assert.NotNull(retrieved);
        Assert.Equal("my-plugin", retrieved.PluginId);
    }

    [Fact]
    public void SetActivePluginImplementation_Null_RemovesEntry()
    {
        var svc = new DashboardStateService();
        svc.SetActivePluginImplementation("IMyPlugin", new ActivePluginImplementation { PluginId = "x" });
        svc.SetActivePluginImplementation("IMyPlugin", null);

        Assert.Null(svc.GetActivePluginImplementation("IMyPlugin"));
    }

    [Fact]
    public void SyncPluginImplementationsFromApi_ReplacesAll()
    {
        var svc = new DashboardStateService();
        svc.SetActivePluginImplementation("IOld", new ActivePluginImplementation { PluginId = "old" });

        svc.SyncPluginImplementationsFromApi(new Dictionary<string, ActivePluginImplementation>
        {
            ["INew"] = new ActivePluginImplementation { PluginId = "new-plugin" }
        });

        Assert.Null(svc.GetActivePluginImplementation("IOld"));
        Assert.NotNull(svc.GetActivePluginImplementation("INew"));
    }
}
