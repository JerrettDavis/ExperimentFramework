using ExperimentFramework.Data.Recording;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Data;

[Feature("OutcomeRecorderOptions configures outcome recording behavior")]
public sealed class OutcomeRecorderOptionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Default values are set correctly")]
    [Fact]
    public Task Default_values_set()
        => Given("default outcome recorder options", () => new OutcomeRecorderOptions())
            .Then("AutoGenerateIds is true", opts => opts.AutoGenerateIds)
            .And("AutoSetTimestamps is true", opts => opts.AutoSetTimestamps)
            .And("CollectDuration is true", opts => opts.CollectDuration)
            .And("CollectErrors is true", opts => opts.CollectErrors)
            .And("DurationMetricName is 'duration_seconds'", opts => opts.DurationMetricName == "duration_seconds")
            .And("ErrorMetricName is 'error'", opts => opts.ErrorMetricName == "error")
            .And("SuccessMetricName is 'success'", opts => opts.SuccessMetricName == "success")
            .And("EnableBatching is false", opts => !opts.EnableBatching)
            .And("MaxBatchSize is 100", opts => opts.MaxBatchSize == 100)
            .And("BatchFlushInterval is 5 seconds", opts => opts.BatchFlushInterval == TimeSpan.FromSeconds(5))
            .AssertPassed();

    [Scenario("AutoGenerateIds can be disabled")]
    [Fact]
    public Task AutoGenerateIds_can_be_disabled()
        => Given("options with AutoGenerateIds disabled", () => new OutcomeRecorderOptions
            {
                AutoGenerateIds = false
            })
            .Then("AutoGenerateIds is false", opts => !opts.AutoGenerateIds)
            .AssertPassed();

    [Scenario("AutoSetTimestamps can be disabled")]
    [Fact]
    public Task AutoSetTimestamps_can_be_disabled()
        => Given("options with AutoSetTimestamps disabled", () => new OutcomeRecorderOptions
            {
                AutoSetTimestamps = false
            })
            .Then("AutoSetTimestamps is false", opts => !opts.AutoSetTimestamps)
            .AssertPassed();

    [Scenario("CollectDuration can be disabled")]
    [Fact]
    public Task CollectDuration_can_be_disabled()
        => Given("options with CollectDuration disabled", () => new OutcomeRecorderOptions
            {
                CollectDuration = false
            })
            .Then("CollectDuration is false", opts => !opts.CollectDuration)
            .AssertPassed();

    [Scenario("CollectErrors can be disabled")]
    [Fact]
    public Task CollectErrors_can_be_disabled()
        => Given("options with CollectErrors disabled", () => new OutcomeRecorderOptions
            {
                CollectErrors = false
            })
            .Then("CollectErrors is false", opts => !opts.CollectErrors)
            .AssertPassed();

    [Scenario("DurationMetricName can be customized")]
    [Fact]
    public Task DurationMetricName_can_be_customized()
        => Given("options with custom duration metric name", () => new OutcomeRecorderOptions
            {
                DurationMetricName = "latency_ms"
            })
            .Then("DurationMetricName is customized", opts => opts.DurationMetricName == "latency_ms")
            .AssertPassed();

    [Scenario("ErrorMetricName can be customized")]
    [Fact]
    public Task ErrorMetricName_can_be_customized()
        => Given("options with custom error metric name", () => new OutcomeRecorderOptions
            {
                ErrorMetricName = "failure"
            })
            .Then("ErrorMetricName is customized", opts => opts.ErrorMetricName == "failure")
            .AssertPassed();

    [Scenario("SuccessMetricName can be customized")]
    [Fact]
    public Task SuccessMetricName_can_be_customized()
        => Given("options with custom success metric name", () => new OutcomeRecorderOptions
            {
                SuccessMetricName = "completed"
            })
            .Then("SuccessMetricName is customized", opts => opts.SuccessMetricName == "completed")
            .AssertPassed();

    [Scenario("EnableBatching can be enabled")]
    [Fact]
    public Task EnableBatching_can_be_enabled()
        => Given("options with batching enabled", () => new OutcomeRecorderOptions
            {
                EnableBatching = true
            })
            .Then("EnableBatching is true", opts => opts.EnableBatching)
            .AssertPassed();

    [Scenario("MaxBatchSize can be customized")]
    [Fact]
    public Task MaxBatchSize_can_be_customized()
        => Given("options with custom batch size", () => new OutcomeRecorderOptions
            {
                MaxBatchSize = 500
            })
            .Then("MaxBatchSize is customized", opts => opts.MaxBatchSize == 500)
            .AssertPassed();

    [Scenario("BatchFlushInterval can be customized")]
    [Fact]
    public Task BatchFlushInterval_can_be_customized()
        => Given("options with custom flush interval", () => new OutcomeRecorderOptions
            {
                BatchFlushInterval = TimeSpan.FromSeconds(10)
            })
            .Then("BatchFlushInterval is customized", opts => opts.BatchFlushInterval == TimeSpan.FromSeconds(10))
            .AssertPassed();

    [Scenario("All options can be set together")]
    [Fact]
    public Task All_options_can_be_set()
        => Given("fully configured options", () => new OutcomeRecorderOptions
            {
                AutoGenerateIds = false,
                AutoSetTimestamps = false,
                CollectDuration = false,
                CollectErrors = false,
                DurationMetricName = "custom_duration",
                ErrorMetricName = "custom_error",
                SuccessMetricName = "custom_success",
                EnableBatching = true,
                MaxBatchSize = 200,
                BatchFlushInterval = TimeSpan.FromMinutes(1)
            })
            .Then("AutoGenerateIds is false", opts => !opts.AutoGenerateIds)
            .And("AutoSetTimestamps is false", opts => !opts.AutoSetTimestamps)
            .And("CollectDuration is false", opts => !opts.CollectDuration)
            .And("CollectErrors is false", opts => !opts.CollectErrors)
            .And("DurationMetricName is custom", opts => opts.DurationMetricName == "custom_duration")
            .And("ErrorMetricName is custom", opts => opts.ErrorMetricName == "custom_error")
            .And("SuccessMetricName is custom", opts => opts.SuccessMetricName == "custom_success")
            .And("EnableBatching is true", opts => opts.EnableBatching)
            .And("MaxBatchSize is 200", opts => opts.MaxBatchSize == 200)
            .And("BatchFlushInterval is 1 minute", opts => opts.BatchFlushInterval == TimeSpan.FromMinutes(1))
            .AssertPassed();

    [Scenario("Options can be modified after creation")]
    [Fact]
    public Task Options_can_be_modified()
        => Given("default options", () => new OutcomeRecorderOptions())
            .When("modifying properties", opts =>
            {
                opts.AutoGenerateIds = false;
                opts.MaxBatchSize = 50;
                return opts;
            })
            .Then("AutoGenerateIds is updated", opts => !opts.AutoGenerateIds)
            .And("MaxBatchSize is updated", opts => opts.MaxBatchSize == 50)
            .AssertPassed();

    [Scenario("Small batch size is valid")]
    [Fact]
    public Task Small_batch_size_is_valid()
        => Given("options with small batch size", () => new OutcomeRecorderOptions
            {
                MaxBatchSize = 1
            })
            .Then("MaxBatchSize is 1", opts => opts.MaxBatchSize == 1)
            .AssertPassed();

    [Scenario("Large batch size is valid")]
    [Fact]
    public Task Large_batch_size_is_valid()
        => Given("options with large batch size", () => new OutcomeRecorderOptions
            {
                MaxBatchSize = 10000
            })
            .Then("MaxBatchSize is 10000", opts => opts.MaxBatchSize == 10000)
            .AssertPassed();

    [Scenario("Very short flush interval is valid")]
    [Fact]
    public Task Short_flush_interval_is_valid()
        => Given("options with short flush interval", () => new OutcomeRecorderOptions
            {
                BatchFlushInterval = TimeSpan.FromMilliseconds(100)
            })
            .Then("BatchFlushInterval is 100ms", opts => opts.BatchFlushInterval == TimeSpan.FromMilliseconds(100))
            .AssertPassed();

    [Scenario("Long flush interval is valid")]
    [Fact]
    public Task Long_flush_interval_is_valid()
        => Given("options with long flush interval", () => new OutcomeRecorderOptions
            {
                BatchFlushInterval = TimeSpan.FromMinutes(10)
            })
            .Then("BatchFlushInterval is 10 minutes", opts => opts.BatchFlushInterval == TimeSpan.FromMinutes(10))
            .AssertPassed();

    [Scenario("Empty metric name is technically valid")]
    [Fact]
    public Task Empty_metric_name_is_valid()
        => Given("options with empty metric name", () => new OutcomeRecorderOptions
            {
                DurationMetricName = ""
            })
            .Then("DurationMetricName is empty", opts => opts.DurationMetricName == "")
            .AssertPassed();

    [Scenario("Metric names with special characters")]
    [Fact]
    public Task Metric_names_with_special_characters()
        => Given("options with special character metric names", () => new OutcomeRecorderOptions
            {
                DurationMetricName = "api.latency_p99",
                ErrorMetricName = "error.rate",
                SuccessMetricName = "success-count"
            })
            .Then("DurationMetricName contains dot and underscore", opts => opts.DurationMetricName == "api.latency_p99")
            .And("ErrorMetricName contains dot", opts => opts.ErrorMetricName == "error.rate")
            .And("SuccessMetricName contains hyphen", opts => opts.SuccessMetricName == "success-count")
            .AssertPassed();
}
