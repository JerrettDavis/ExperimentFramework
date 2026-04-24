using Bunit;
using ExperimentFramework.Dashboard.UI.Components.Layout;

namespace ExperimentFramework.Dashboard.UI.Tests.Components;

/// <summary>
/// bUnit tests for the NavMenu layout component.
/// NavMenu has no service dependencies — it is static navigation markup.
/// </summary>
public sealed class NavMenuTests : BunitContext
{
    [Fact]
    public void NavMenu_Renders_NavElement_WithSidebarClass()
    {
        var cut = Render<NavMenu>();
        var nav = cut.Find("nav.sidebar");
        Assert.NotNull(nav);
    }

    [Fact]
    public void NavMenu_Renders_HomeNavLink()
    {
        var cut = Render<NavMenu>();
        // NavLink renders as an <a> tag
        var links = cut.FindAll("a");
        Assert.Contains(links, l => l.GetAttribute("href") == "/dashboard");
    }

    [Fact]
    public void NavMenu_Renders_ExperimentsNavLink()
    {
        var cut = Render<NavMenu>();
        var links = cut.FindAll("a");
        Assert.Contains(links, l => l.GetAttribute("href") == "/dashboard/experiments");
    }

    [Fact]
    public void NavMenu_Renders_AnalyticsNavLink()
    {
        var cut = Render<NavMenu>();
        var links = cut.FindAll("a");
        Assert.Contains(links, l => l.GetAttribute("href") == "/dashboard/analytics");
    }

    [Fact]
    public void NavMenu_Renders_GovernanceNavLink()
    {
        var cut = Render<NavMenu>();
        var links = cut.FindAll("a");
        Assert.Contains(links, l => l.GetAttribute("href") == "/dashboard/governance/lifecycle");
    }

    [Fact]
    public void NavMenu_Renders_TargetingNavLink()
    {
        var cut = Render<NavMenu>();
        var links = cut.FindAll("a");
        Assert.Contains(links, l => l.GetAttribute("href") == "/dashboard/targeting");
    }

    [Fact]
    public void NavMenu_Renders_PluginsNavLink()
    {
        var cut = Render<NavMenu>();
        var links = cut.FindAll("a");
        Assert.Contains(links, l => l.GetAttribute("href") == "/dashboard/plugins");
    }

    [Fact]
    public void NavMenu_Renders_ConfigurationNavLink()
    {
        var cut = Render<NavMenu>();
        var links = cut.FindAll("a");
        Assert.Contains(links, l => l.GetAttribute("href") == "/dashboard/configuration");
    }

    [Fact]
    public void NavMenu_Renders_DslEditorNavLink()
    {
        var cut = Render<NavMenu>();
        var links = cut.FindAll("a");
        Assert.Contains(links, l => l.GetAttribute("href") == "/dashboard/dsl");
    }

    [Fact]
    public void NavMenu_Renders_LogoutLink_WithDataActionAttribute()
    {
        var cut = Render<NavMenu>();
        var logout = cut.Find("[data-action='logout']");
        Assert.NotNull(logout);
    }

    [Fact]
    public void NavMenu_Renders_AllNavIcons()
    {
        var cut = Render<NavMenu>();
        var icons = cut.FindAll(".nav-icon");
        // 10 nav items (home, experiments, analytics, governance, targeting, rollout, hypothesis, plugins, configuration, dsl) + logout
        Assert.True(icons.Count >= 10, $"Expected at least 10 nav-icons but found {icons.Count}");
    }
}
