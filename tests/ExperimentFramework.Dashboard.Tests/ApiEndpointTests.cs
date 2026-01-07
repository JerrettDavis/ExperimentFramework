using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExperimentFramework.Dashboard.Tests;

/// <summary>
/// Integration tests for dashboard API endpoints.
/// </summary>
public class ApiEndpointTests : IClassFixture<DashboardWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ApiEndpointTests(DashboardWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetExperiments_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/dashboard/api/experiments");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.NotNull(content);
    }

    [Fact]
    public async Task GetExperiment_ValidName_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/dashboard/api/experiments/test-experiment");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("test-experiment", content);
    }

    [Fact]
    public async Task GetExperiment_InvalidName_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/dashboard/api/experiments/nonexistent");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetConfigurationInfo_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/dashboard/api/configuration/info");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ExperimentFramework", content);
    }

    [Fact]
    public async Task GetConfigurationYaml_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/dashboard/api/configuration/yaml");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetPlugins_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/dashboard/api/plugins");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetGovernanceState_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/dashboard/api/governance/test-experiment/state");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("test-experiment", content);
    }

    [Fact]
    public async Task GetPendingApprovals_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/dashboard/api/governance/approvals/pending");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAnalyticsStatistics_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/dashboard/api/analytics/test-experiment/statistics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("test-experiment", content);
    }

    [Fact]
    public async Task GetRolloutStages_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/dashboard/api/rollout/test-experiment/stages");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetTargetingRules_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/dashboard/api/targeting/test-experiment/rules");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
