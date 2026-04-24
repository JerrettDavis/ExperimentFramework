using Bunit;
using ExperimentFramework.Dashboard.UI.Components.Layout;
using Microsoft.AspNetCore.Components;

namespace ExperimentFramework.Dashboard.UI.Tests.Components;

/// <summary>
/// bUnit tests for the MainLayout component.
/// MainLayout inherits LayoutComponentBase, so the body slot is the Body parameter.
/// </summary>
public sealed class MainLayoutTests : BunitContext
{
    // bUnit helper: render a layout with a simple body fragment.
    private IRenderedComponent<MainLayout> RenderLayout(string bodyMarkup = "<p>body</p>")
    {
        RenderFragment body = builder =>
        {
            builder.AddMarkupContent(0, bodyMarkup);
        };
        return Render<MainLayout>(p => p.Add(l => l.Body, body));
    }

    [Fact]
    public void MainLayout_Renders_DashboardShellDiv()
    {
        var cut = RenderLayout();
        var shell = cut.Find(".dashboard-shell");
        Assert.NotNull(shell);
    }

    [Fact]
    public void MainLayout_Renders_Sidebar_AsideElement()
    {
        var cut = RenderLayout();
        var aside = cut.Find("aside.dashboard-sidebar");
        Assert.NotNull(aside);
    }

    [Fact]
    public void MainLayout_Renders_DashboardBrandHeader()
    {
        var cut = RenderLayout();
        var brand = cut.Find(".dashboard-brand");
        Assert.NotNull(brand);
    }

    [Fact]
    public void MainLayout_Renders_NavMenu()
    {
        var cut = RenderLayout();
        var nav = cut.Find("nav.sidebar");
        Assert.NotNull(nav);
    }

    [Fact]
    public void MainLayout_Renders_Body_InMainDiv()
    {
        var cut = RenderLayout("<p class='test-body'>hello</p>");
        var main = cut.Find(".dashboard-main");
        Assert.NotNull(main);
        Assert.Contains("hello", main.TextContent);
    }
}
