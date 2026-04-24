using Bunit;
using ExperimentFramework.Dashboard.UI.Components.Pages;

namespace ExperimentFramework.Dashboard.UI.Tests.Components;

/// <summary>
/// bUnit tests for the Home (landing) page component.
/// Home.razor has no service dependencies and no @code logic — it is a pure markup component.
/// </summary>
public sealed class HomePageTests : BunitContext
{
    [Fact]
    public void Home_Renders_PageTitle()
    {
        var cut = Render<Home>();
        var title = cut.FindComponent<Microsoft.AspNetCore.Components.Web.PageTitle>();
        Assert.NotNull(title);
    }

    [Fact]
    public void Home_Renders_MainElement_WithCorrectDataPage()
    {
        var cut = Render<Home>();
        var main = cut.Find("main[data-page='home']");
        Assert.NotNull(main);
    }

    [Fact]
    public void Home_Renders_HeadingWithDashboardTitle()
    {
        var cut = Render<Home>();
        var h1 = cut.Find("h1");
        Assert.Contains("Experiment Dashboard", h1.TextContent);
    }

    [Fact]
    public void Home_Renders_SixFeatureCards()
    {
        var cut = Render<Home>();
        var cards = cut.FindAll(".feature-card");
        Assert.Equal(6, cards.Count);
    }

    [Fact]
    public void Home_FeatureCards_ContainExpectedLinks()
    {
        var cut = Render<Home>();
        var links = cut.FindAll(".feature-card")
                       .Select(c => c.GetAttribute("href"))
                       .ToList();

        Assert.Contains("/dashboard/experiments", links);
        Assert.Contains("/dashboard/analytics", links);
        Assert.Contains("/dashboard/governance/lifecycle", links);
        Assert.Contains("/dashboard/targeting", links);
        Assert.Contains("/dashboard/plugins", links);
        Assert.Contains("/dashboard/hypothesis", links);
    }

    [Fact]
    public void Home_ExperimentsCard_ContainsExpectedText()
    {
        var cut = Render<Home>();
        var card = cut.Find("[href='/dashboard/experiments']");
        Assert.Contains("Experiment Management", card.TextContent);
    }

    [Fact]
    public void Home_AnalyticsCard_ContainsAnalyticsText()
    {
        var cut = Render<Home>();
        var card = cut.Find("[href='/dashboard/analytics']");
        Assert.Contains("Analytics", card.TextContent);
    }

    [Fact]
    public void Home_Renders_HeroSubtitle()
    {
        var cut = Render<Home>();
        var subtitle = cut.Find(".hero-subtitle");
        Assert.NotNull(subtitle);
        Assert.NotEmpty(subtitle.TextContent);
    }

    [Fact]
    public void Home_Renders_FeaturesGrid()
    {
        var cut = Render<Home>();
        var grid = cut.Find(".features-grid");
        Assert.NotNull(grid);
    }

    [Fact]
    public void Home_EachFeatureCard_HasFeatureLinkSpan()
    {
        var cut = Render<Home>();
        var featureLinks = cut.FindAll(".feature-link");
        Assert.Equal(6, featureLinks.Count);
    }
}
