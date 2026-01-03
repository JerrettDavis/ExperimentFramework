using ExperimentFramework.DataPlane.SqlServer;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.DataPlane.SqlServer.Tests;

[Feature("SQL Server backplane configuration options")]
public class SqlServerDataBackplaneOptionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Options have correct default values")]
    [Fact]
    public Task Options_have_default_values()
        => Given("a new SQL Server options instance", () => new SqlServerDataBackplaneOptions
            {
                ConnectionString = "Server=localhost;Database=Test;..."
            })
            .Then("Schema should be dbo", 
                options => options.Schema == "dbo")
            .And("TableName should be ExperimentEvents", 
                options => options.TableName == "ExperimentEvents")
            .And("BatchSize should be 100", 
                options => options.BatchSize == 100)
            .And("EnableIdempotency should be true", 
                options => options.EnableIdempotency)
            .And("AutoMigrate should be false", 
                options => !options.AutoMigrate)
            .And("CommandTimeoutSeconds should be 30", 
                options => options.CommandTimeoutSeconds == 30)
            .AssertPassed();

    [Scenario("Options allow customization")]
    [Fact]
    public Task Options_allow_customization()
        => Given("custom SQL Server options", () => new SqlServerDataBackplaneOptions
            {
                ConnectionString = "Server=localhost;Database=CustomDb;...",
                Schema = "custom",
                TableName = "CustomEvents",
                BatchSize = 200,
                EnableIdempotency = false,
                AutoMigrate = true,
                CommandTimeoutSeconds = 60
            })
            .Then("ConnectionString should not be empty", 
                options => !string.IsNullOrEmpty(options.ConnectionString))
            .And("Schema should be custom", 
                options => options.Schema == "custom")
            .And("TableName should be CustomEvents", 
                options => options.TableName == "CustomEvents")
            .And("BatchSize should be 200", 
                options => options.BatchSize == 200)
            .And("EnableIdempotency should be false", 
                options => !options.EnableIdempotency)
            .And("AutoMigrate should be true", 
                options => options.AutoMigrate)
            .And("CommandTimeoutSeconds should be 60", 
                options => options.CommandTimeoutSeconds == 60)
            .AssertPassed();
}
