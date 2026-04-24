using Bunit;
using ExperimentFramework.Dashboard.UI.Components.Pages;
using ExperimentFramework.Dashboard.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace ExperimentFramework.Dashboard.UI.Tests.Components;

/// <summary>
/// bUnit tests for the Experiments page component.
///
/// Because the page guards data loading behind RendererInfo.IsInteractive (which is
/// false in bUnit's SSR context), OnInitializedAsync skips the API call and the
/// component renders in its skeleton/loading state. Tests verify that static structure
/// (header, stats row, toolbar) is always present and that service registration works.
/// </summary>
public sealed class ExperimentsPageTests : BunitContext
{
    private static ExperimentApiClient BuildApiClient(
        IEnumerable<ExperimentFramework.Dashboard.UI.Services.ExperimentInfo>? experiments = null)
    {
        var expList = experiments?.ToList() ?? [];
        var handler = new FakeExperimentsHandler(expList);
        return new ExperimentApiClient(new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });
    }

    private void RegisterServices(ExperimentApiClient? apiClient = null)
    {
        Services.AddSingleton(apiClient ?? BuildApiClient());
        Services.AddSingleton<DashboardStateService>();
        Services.AddSingleton<ThemeService>();

        // The Experiments page calls RendererInfo.IsInteractive, which requires explicit setup.
        // Setting IsInteractive=false simulates the SSR/prerender pass — data loading is skipped
        // and the component stays in the skeleton loading state. This is exactly what we test.
        SetRendererInfo(new Microsoft.AspNetCore.Components.RendererInfo("Static", isInteractive: false));
    }

    [Fact]
    public void Experiments_Renders_MainElement_WithCorrectDataPage()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        var main = cut.Find("main[data-page='experiments']");
        Assert.NotNull(main);
    }

    [Fact]
    public void Experiments_Renders_H1_WithExperimentsTitle()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        var h1 = cut.Find("h1");
        Assert.Contains("Experiments", h1.TextContent);
    }

    [Fact]
    public void Experiments_Renders_StatsRow_WithFourCards()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        var cards = cut.FindAll(".stat-card");
        Assert.Equal(4, cards.Count);
    }

    [Fact]
    public void Experiments_Renders_RefreshButton()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        // The refresh/secondary button in the header
        var btn = cut.Find(".btn-secondary");
        Assert.NotNull(btn);
    }

    [Fact]
    public void Experiments_Renders_CreateExperimentButton_AsDisabled()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        var primaryBtn = cut.Find(".btn-primary");
        Assert.True(primaryBtn.HasAttribute("disabled"));
    }

    [Fact]
    public void Experiments_Renders_SearchInput()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        var input = cut.Find("input[type='text']");
        Assert.NotNull(input);
    }

    [Fact]
    public void Experiments_Renders_CategoryFilterButtons()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        var filterBtns = cut.FindAll(".category-btn");
        // "All" + 5 categories (Revenue, Engagement, UX, Blog, Plugins)
        Assert.Equal(6, filterBtns.Count);
    }

    [Fact]
    public void Experiments_Renders_AllFilterButton_AsActive_ByDefault()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        // "All" button has active class when no category filter is set
        var allBtn = cut.Find(".category-btn[data-category='All']");
        Assert.Contains("active", allBtn.ClassName ?? "");
    }

    [Fact]
    public void Experiments_Renders_StatsRow_ShowingLoadingDash_WhenLoading()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        // In SSR / non-interactive mode, _loading = true and stats show "-"
        var statValues = cut.FindAll(".stat-value");
        Assert.True(statValues.Count > 0);
        // At least one stat shows the loading dash
        Assert.Contains(statValues, v => v.TextContent.Trim() == "-");
    }

    [Fact]
    public void Experiments_Renders_LoadingSpinner_InSSRMode()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        // In non-interactive mode the component stays in loading state — spinner is shown
        var spinner = cut.Find("[data-loading='true']");
        Assert.NotNull(spinner);
    }

    [Fact]
    public void Experiments_Renders_SkeletonRows_InSSRMode()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        // Three skeleton rows are rendered while loading
        var skeletons = cut.FindAll("[data-skeleton]");
        Assert.Equal(3, skeletons.Count);
    }

    [Fact]
    public void Experiments_NoError_WhenServicesRegistered()
    {
        RegisterServices();
        // Should render without exception
        var cut = Render<Experiments>();
        // No error banner visible
        var errorBanners = cut.FindAll(".error-banner");
        Assert.Empty(errorBanners);
    }

    [Fact]
    public void Experiments_Toolbar_ContainsSearchBox()
    {
        RegisterServices();
        var cut = Render<Experiments>();
        var searchBox = cut.Find(".search-box");
        Assert.NotNull(searchBox);
    }

    /// <summary>
    /// Minimal HttpMessageHandler that returns an empty experiments list.
    /// </summary>
    private sealed class FakeExperimentsHandler : HttpMessageHandler
    {
        private readonly List<ExperimentFramework.Dashboard.UI.Services.ExperimentInfo> _experiments;

        public FakeExperimentsHandler(
            List<ExperimentFramework.Dashboard.UI.Services.ExperimentInfo> experiments)
        {
            _experiments = experiments;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var envelope = new { experiments = _experiments };
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(envelope)
            };
            return Task.FromResult(response);
        }
    }
}
