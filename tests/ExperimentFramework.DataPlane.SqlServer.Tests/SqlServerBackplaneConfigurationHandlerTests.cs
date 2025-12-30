using ExperimentFramework.Configuration.Models;
using ExperimentFramework.DataPlane.SqlServer.Configuration;
using Xunit;

namespace ExperimentFramework.DataPlane.SqlServer.Tests;

public class SqlServerBackplaneConfigurationHandlerTests
{
    [Fact]
    public void BackplaneType_ShouldBeSqlServer()
    {
        // Arrange
        var handler = new SqlServerBackplaneConfigurationHandler();

        // Act & Assert
        Assert.Equal("sqlServer", handler.BackplaneType);
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenConnectionStringIsMissing()
    {
        // Arrange
        var handler = new SqlServerBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig
        {
            Type = "sqlServer",
            Options = new Dictionary<string, object>()
        };

        // Act
        var errors = handler.Validate(config, "dataPlane.backplane");

        // Assert
        Assert.Single(errors);
        Assert.Contains("connectionString", errors.First().Message);
    }

    [Fact]
    public void Validate_ShouldReturnNoErrors_WhenConnectionStringIsProvided()
    {
        // Arrange
        var handler = new SqlServerBackplaneConfigurationHandler();
        var config = new DataPlaneBackplaneConfig
        {
            Type = "sqlServer",
            Options = new Dictionary<string, object>
            {
                ["connectionString"] = "Server=localhost;Database=Test;..."
            }
        };

        // Act
        var errors = handler.Validate(config, "dataPlane.backplane");

        // Assert
        Assert.Empty(errors);
    }
}
