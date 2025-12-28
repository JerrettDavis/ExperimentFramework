using ExperimentFramework.Data.Models;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Data;

[Feature("ExperimentOutcome captures experiment outcome data")]
public sealed class ExperimentOutcomeTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Creating outcome with all required properties")]
    [Fact]
    public Task Create_outcome_with_required_properties()
        => Given("an experiment outcome with all required properties", () => new ExperimentOutcome
            {
                Id = "outcome-123",
                ExperimentName = "checkout-experiment",
                TrialKey = "variant-a",
                SubjectId = "user-456",
                OutcomeType = OutcomeType.Binary,
                MetricName = "conversion",
                Value = 1.0,
                Timestamp = DateTimeOffset.UtcNow
            })
            .Then("Id is set correctly", o => o.Id == "outcome-123")
            .And("ExperimentName is set correctly", o => o.ExperimentName == "checkout-experiment")
            .And("TrialKey is set correctly", o => o.TrialKey == "variant-a")
            .And("SubjectId is set correctly", o => o.SubjectId == "user-456")
            .And("OutcomeType is Binary", o => o.OutcomeType == OutcomeType.Binary)
            .And("MetricName is set correctly", o => o.MetricName == "conversion")
            .And("Value is set correctly", o => o.Value == 1.0)
            .And("Metadata is null by default", o => o.Metadata == null)
            .AssertPassed();

    [Scenario("Creating outcome with metadata")]
    [Fact]
    public Task Create_outcome_with_metadata()
        => Given("an experiment outcome with metadata", () => new ExperimentOutcome
            {
                Id = "outcome-123",
                ExperimentName = "test-exp",
                TrialKey = "control",
                SubjectId = "user-1",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "revenue",
                Value = 99.99,
                Timestamp = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["device"] = "mobile",
                    ["region"] = "us-west",
                    ["browser"] = "chrome"
                }
            })
            .Then("Metadata is not null", o => o.Metadata != null)
            .And("Metadata has 3 entries", o => o.Metadata!.Count == 3)
            .And("device metadata is accessible", o => o.Metadata!["device"].ToString() == "mobile")
            .And("region metadata is accessible", o => o.Metadata!["region"].ToString() == "us-west")
            .And("browser metadata is accessible", o => o.Metadata!["browser"].ToString() == "chrome")
            .AssertPassed();

    [Scenario("Binary outcome with success value")]
    [Fact]
    public Task Binary_outcome_success_value()
        => Given("a successful binary outcome", () => new ExperimentOutcome
            {
                Id = "id",
                ExperimentName = "exp",
                TrialKey = "trial",
                SubjectId = "user",
                OutcomeType = OutcomeType.Binary,
                MetricName = "conversion",
                Value = 1.0,
                Timestamp = DateTimeOffset.UtcNow
            })
            .Then("Value is 1.0 for success", o => o.Value == 1.0)
            .AssertPassed();

    [Scenario("Binary outcome with failure value")]
    [Fact]
    public Task Binary_outcome_failure_value()
        => Given("a failed binary outcome", () => new ExperimentOutcome
            {
                Id = "id",
                ExperimentName = "exp",
                TrialKey = "trial",
                SubjectId = "user",
                OutcomeType = OutcomeType.Binary,
                MetricName = "conversion",
                Value = 0.0,
                Timestamp = DateTimeOffset.UtcNow
            })
            .Then("Value is 0.0 for failure", o => o.Value == 0.0)
            .AssertPassed();

    [Scenario("Continuous outcome stores measurement value")]
    [Fact]
    public Task Continuous_outcome_measurement_value()
        => Given("a continuous outcome", () => new ExperimentOutcome
            {
                Id = "id",
                ExperimentName = "exp",
                TrialKey = "trial",
                SubjectId = "user",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "revenue",
                Value = 149.99,
                Timestamp = DateTimeOffset.UtcNow
            })
            .Then("Value stores the measurement", o => o.Value == 149.99)
            .AssertPassed();

    [Scenario("Count outcome stores count as double")]
    [Fact]
    public Task Count_outcome_stores_count()
        => Given("a count outcome", () => new ExperimentOutcome
            {
                Id = "id",
                ExperimentName = "exp",
                TrialKey = "trial",
                SubjectId = "user",
                OutcomeType = OutcomeType.Count,
                MetricName = "page_views",
                Value = 42,
                Timestamp = DateTimeOffset.UtcNow
            })
            .Then("Value stores the count", o => o.Value == 42)
            .AssertPassed();

    [Scenario("Duration outcome stores seconds")]
    [Fact]
    public Task Duration_outcome_stores_seconds()
        => Given("a duration outcome", () => new ExperimentOutcome
            {
                Id = "id",
                ExperimentName = "exp",
                TrialKey = "trial",
                SubjectId = "user",
                OutcomeType = OutcomeType.Duration,
                MetricName = "session_length",
                Value = 120.5,
                Timestamp = DateTimeOffset.UtcNow
            })
            .Then("Value stores duration in seconds", o => o.Value == 120.5)
            .AssertPassed();

    [Scenario("ToString returns formatted representation")]
    [Fact]
    public Task ToString_returns_formatted_representation()
        => Given("an experiment outcome", () => new ExperimentOutcome
            {
                Id = "id",
                ExperimentName = "my-experiment",
                TrialKey = "variant-a",
                SubjectId = "user-123",
                OutcomeType = OutcomeType.Binary,
                MetricName = "conversion",
                Value = 1.0,
                Timestamp = DateTimeOffset.UtcNow
            })
            .When("calling ToString", o => o.ToString())
            .Then("contains experiment name", s => s.Contains("my-experiment"))
            .And("contains trial key", s => s.Contains("variant-a"))
            .And("contains metric name", s => s.Contains("conversion"))
            .And("contains subject id", s => s.Contains("user-123"))
            .And("contains outcome type", s => s.Contains("Binary"))
            .AssertPassed();

    [Scenario("Outcome preserves timestamp exactly")]
    [Fact]
    public Task Outcome_preserves_timestamp()
        => Given("a specific timestamp", () => new DateTimeOffset(2024, 6, 15, 10, 30, 0, TimeSpan.Zero))
            .When("creating an outcome with that timestamp", ts => new ExperimentOutcome
            {
                Id = "id",
                ExperimentName = "exp",
                TrialKey = "trial",
                SubjectId = "user",
                OutcomeType = OutcomeType.Binary,
                MetricName = "metric",
                Value = 1.0,
                Timestamp = ts
            })
            .Then("Timestamp is preserved exactly", o =>
                o.Timestamp.Year == 2024 &&
                o.Timestamp.Month == 6 &&
                o.Timestamp.Day == 15 &&
                o.Timestamp.Hour == 10 &&
                o.Timestamp.Minute == 30)
            .AssertPassed();

    [Scenario("Negative value is allowed for continuous outcomes")]
    [Fact]
    public Task Negative_value_allowed_for_continuous()
        => Given("a continuous outcome with negative value", () => new ExperimentOutcome
            {
                Id = "id",
                ExperimentName = "exp",
                TrialKey = "trial",
                SubjectId = "user",
                OutcomeType = OutcomeType.Continuous,
                MetricName = "temperature_delta",
                Value = -5.5,
                Timestamp = DateTimeOffset.UtcNow
            })
            .Then("negative value is stored", o => o.Value == -5.5)
            .AssertPassed();

    [Scenario("Zero value is valid")]
    [Fact]
    public Task Zero_value_is_valid()
        => Given("an outcome with zero value", () => new ExperimentOutcome
            {
                Id = "id",
                ExperimentName = "exp",
                TrialKey = "trial",
                SubjectId = "user",
                OutcomeType = OutcomeType.Count,
                MetricName = "errors",
                Value = 0,
                Timestamp = DateTimeOffset.UtcNow
            })
            .Then("zero value is stored", o => o.Value == 0)
            .AssertPassed();

    [Scenario("Empty metadata dictionary is allowed")]
    [Fact]
    public Task Empty_metadata_dictionary_allowed()
        => Given("an outcome with empty metadata", () => new ExperimentOutcome
            {
                Id = "id",
                ExperimentName = "exp",
                TrialKey = "trial",
                SubjectId = "user",
                OutcomeType = OutcomeType.Binary,
                MetricName = "metric",
                Value = 1.0,
                Timestamp = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>()
            })
            .Then("Metadata is empty but not null", o => o.Metadata != null && o.Metadata.Count == 0)
            .AssertPassed();

    [Scenario("Metadata supports various value types")]
    [Fact]
    public Task Metadata_supports_various_types()
        => Given("an outcome with mixed metadata types", () => new ExperimentOutcome
            {
                Id = "id",
                ExperimentName = "exp",
                TrialKey = "trial",
                SubjectId = "user",
                OutcomeType = OutcomeType.Binary,
                MetricName = "metric",
                Value = 1.0,
                Timestamp = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["string_val"] = "test",
                    ["int_val"] = 42,
                    ["double_val"] = 3.14,
                    ["bool_val"] = true,
                    ["list_val"] = new List<string> { "a", "b" }
                }
            })
            .Then("string value is accessible", o => o.Metadata!["string_val"].ToString() == "test")
            .And("int value is accessible", o => (int)o.Metadata!["int_val"] == 42)
            .And("double value is accessible", o => Math.Abs((double)o.Metadata!["double_val"] - 3.14) < 0.001)
            .And("bool value is accessible", o => (bool)o.Metadata!["bool_val"])
            .And("list value is accessible", o => ((List<string>)o.Metadata!["list_val"]).Count == 2)
            .AssertPassed();
}
