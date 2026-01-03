using ExperimentFramework.Configuration.Models;
using ExperimentFramework.DataPlane.SqlServer.Configuration;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.DataPlane.SqlServer.Tests;

[Feature("SQL Server backplane DSL configuration handler")]
public class SqlServerBackplaneConfigurationHandlerTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Handler has correct backplane type")]
    [Fact]
    public Task Handler_has_sql_server_backplane_type()
        => Given("a SQL Server backplane configuration handler", () => new SqlServerBackplaneConfigurationHandler())
            .Then("backplane type should be sqlServer", 
                handler => handler.BackplaneType == "sqlServer")
            .AssertPassed();

    [Scenario("Validation returns error when connection string is missing")]
    [Fact]
    public Task Validation_returns_error_when_connection_string_missing()
        => Given("a handler and config without connection string", () =>
            {
                var handler = new SqlServerBackplaneConfigurationHandler();
                var config = new DataPlaneBackplaneConfig
                {
                    Type = "sqlServer",
                    Options = new Dictionary<string, object>()
                };
                return (handler, config);
            })
            .When("validating the configuration", 
                state => state.handler.Validate(state.config, "dataPlane.backplane"))
            .Then("should have one error", 
                errors => errors.Count() == 1)
            .And("error message should mention connectionString", 
                errors => errors.First().Message.Contains("connectionString"))
            .AssertPassed();

    [Scenario("Validation returns no errors when connection string is provided")]
    [Fact]
    public Task Validation_returns_no_errors_when_connection_string_provided()
        => Given("a handler and config with connection string", () =>
            {
                var handler = new SqlServerBackplaneConfigurationHandler();
                var config = new DataPlaneBackplaneConfig
                {
                    Type = "sqlServer",
                    Options = new Dictionary<string, object>
                    {
                        ["connectionString"] = "Server=localhost;Database=Test;..."
                    }
                };
                return (handler, config);
            })
            .When("validating the configuration", 
                state => state.handler.Validate(state.config, "dataPlane.backplane"))
            .Then("should have no errors", 
                errors => !errors.Any())
            .AssertPassed();
}
