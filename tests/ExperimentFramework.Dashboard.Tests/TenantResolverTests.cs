using Microsoft.AspNetCore.Http;
using ExperimentFramework.Dashboard;
using ExperimentFramework.Dashboard.Abstractions;

namespace ExperimentFramework.Dashboard.Tests;

/// <summary>
/// Tests for tenant resolver implementations.
/// </summary>
public class TenantResolverTests
{
    [Fact]
    public void TenantContext_CanBeCreated()
    {
        // Arrange & Act
        var context = new TenantContext
        {
            TenantId = "test-tenant",
            DisplayName = "Test Tenant",
            Environment = "testing"
        };

        // Assert
        Assert.Equal("test-tenant", context.TenantId);
        Assert.Equal("Test Tenant", context.DisplayName);
        Assert.Equal("testing", context.Environment);
    }

    [Fact]
    public void TenantContextAccessor_CanSetAndGetCurrent()
    {
        // Arrange
        var context = new TenantContext
        {
            TenantId = "test-tenant"
        };

        // Act
        TenantContextAccessor.Current = context;
        var result = TenantContextAccessor.Current;

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-tenant", result.TenantId);

        // Cleanup
        TenantContextAccessor.Current = null;
    }
}
