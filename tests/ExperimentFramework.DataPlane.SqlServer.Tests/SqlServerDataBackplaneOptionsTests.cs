using ExperimentFramework.DataPlane.SqlServer;
using Xunit;

namespace ExperimentFramework.DataPlane.SqlServer.Tests;

public class SqlServerDataBackplaneOptionsTests
{
    [Fact]
    public void Options_ShouldHaveDefaultValues()
    {
        // Arrange & Act
        var options = new SqlServerDataBackplaneOptions
        {
            ConnectionString = "Server=localhost;Database=Test;..."
        };

        // Assert
        Assert.Equal("dbo", options.Schema);
        Assert.Equal("ExperimentEvents", options.TableName);
        Assert.Equal(100, options.BatchSize);
        Assert.True(options.EnableIdempotency);
        Assert.False(options.AutoMigrate);
        Assert.Equal(30, options.CommandTimeoutSeconds);
    }

    [Fact]
    public void Options_ShouldAllowCustomization()
    {
        // Arrange & Act
        var options = new SqlServerDataBackplaneOptions
        {
            ConnectionString = "Server=localhost;Database=CustomDb;...",
            Schema = "custom",
            TableName = "CustomEvents",
            BatchSize = 200,
            EnableIdempotency = false,
            AutoMigrate = true,
            CommandTimeoutSeconds = 60
        };

        // Assert
        Assert.NotEmpty(options.ConnectionString);
        Assert.Equal("custom", options.Schema);
        Assert.Equal("CustomEvents", options.TableName);
        Assert.Equal(200, options.BatchSize);
        Assert.False(options.EnableIdempotency);
        Assert.True(options.AutoMigrate);
        Assert.Equal(60, options.CommandTimeoutSeconds);
    }
}
