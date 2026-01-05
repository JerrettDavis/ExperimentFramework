using ExperimentFramework.Dashboard;
using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard.Tests;

/// <summary>
/// Tests for DashboardOptions configuration.
/// </summary>
public class DashboardOptionsTests
{
    [Fact]
    public void DashboardOptions_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var options = new DashboardOptions();

        // Assert
        Assert.Equal("/dashboard", options.PathBase);
        Assert.Equal("Experiment Dashboard", options.Title);
        Assert.True(options.EnableAnalytics);
        Assert.True(options.EnableGovernanceUI);
        Assert.False(options.RequireAuthorization);
        Assert.Null(options.AuthorizationPolicy);
        Assert.NotNull(options.TenantResolver);
    }

    [Fact]
    public void DashboardOptions_CanBeConfigured()
    {
        // Arrange
        var options = new DashboardOptions
        {
            PathBase = "/custom-dashboard",
            Title = "Custom Dashboard",
            EnableAnalytics = false,
            EnableGovernanceUI = false,
            RequireAuthorization = true,
            AuthorizationPolicy = "CustomPolicy"
        };

        // Assert
        Assert.Equal("/custom-dashboard", options.PathBase);
        Assert.Equal("Custom Dashboard", options.Title);
        Assert.False(options.EnableAnalytics);
        Assert.False(options.EnableGovernanceUI);
        Assert.True(options.RequireAuthorization);
        Assert.Equal("CustomPolicy", options.AuthorizationPolicy);
    }

    [Fact]
    public void DashboardOptions_ItemsPerPage_DefaultIsCorrect()
    {
        // Arrange & Act
        var options = new DashboardOptions();

        // Assert
        Assert.Equal(25, options.ItemsPerPage);
    }
}
