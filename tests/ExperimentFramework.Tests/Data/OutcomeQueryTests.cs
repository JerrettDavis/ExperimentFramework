using ExperimentFramework.Data.Models;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Data;

[Feature("OutcomeQuery provides flexible query parameters for outcomes")]
public sealed class OutcomeQueryTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Default query has all filters null")]
    [Fact]
    public Task Default_query_has_null_filters()
        => Given("a default outcome query", () => new OutcomeQuery())
            .Then("ExperimentName is null", q => q.ExperimentName == null)
            .And("TrialKey is null", q => q.TrialKey == null)
            .And("MetricName is null", q => q.MetricName == null)
            .And("SubjectId is null", q => q.SubjectId == null)
            .And("OutcomeType is null", q => q.OutcomeType == null)
            .And("FromTimestamp is null", q => q.FromTimestamp == null)
            .And("ToTimestamp is null", q => q.ToTimestamp == null)
            .And("Limit is null", q => q.Limit == null)
            .And("Offset is null", q => q.Offset == null)
            .And("OrderByTimestampDescending is false", q => !q.OrderByTimestampDescending)
            .AssertPassed();

    [Scenario("ForExperiment factory creates query with experiment name")]
    [Fact]
    public Task ForExperiment_creates_filtered_query()
        => Given("ForExperiment factory method", () => OutcomeQuery.ForExperiment("my-experiment"))
            .Then("ExperimentName is set", q => q.ExperimentName == "my-experiment")
            .And("other filters are null", q =>
                q.TrialKey == null &&
                q.MetricName == null &&
                q.SubjectId == null)
            .AssertPassed();

    [Scenario("ForMetric factory creates query with experiment and metric")]
    [Fact]
    public Task ForMetric_creates_filtered_query()
        => Given("ForMetric factory method", () => OutcomeQuery.ForMetric("my-experiment", "conversion"))
            .Then("ExperimentName is set", q => q.ExperimentName == "my-experiment")
            .And("MetricName is set", q => q.MetricName == "conversion")
            .And("other filters are null", q =>
                q.TrialKey == null &&
                q.SubjectId == null)
            .AssertPassed();

    [Scenario("ForSubject factory creates query with subject ID")]
    [Fact]
    public Task ForSubject_creates_filtered_query()
        => Given("ForSubject factory method", () => OutcomeQuery.ForSubject("user-123"))
            .Then("SubjectId is set", q => q.SubjectId == "user-123")
            .And("ExperimentName is null", q => q.ExperimentName == null)
            .AssertPassed();

    [Scenario("All filter properties can be set")]
    [Fact]
    public Task All_filter_properties_settable()
        => Given("a fully configured query", () => new OutcomeQuery
            {
                ExperimentName = "test-exp",
                TrialKey = "control",
                MetricName = "revenue",
                SubjectId = "user-1",
                OutcomeType = OutcomeType.Continuous,
                FromTimestamp = DateTimeOffset.UtcNow.AddDays(-7),
                ToTimestamp = DateTimeOffset.UtcNow,
                Limit = 100,
                Offset = 50,
                OrderByTimestampDescending = true
            })
            .Then("ExperimentName is set", q => q.ExperimentName == "test-exp")
            .And("TrialKey is set", q => q.TrialKey == "control")
            .And("MetricName is set", q => q.MetricName == "revenue")
            .And("SubjectId is set", q => q.SubjectId == "user-1")
            .And("OutcomeType is set", q => q.OutcomeType == OutcomeType.Continuous)
            .And("FromTimestamp is set", q => q.FromTimestamp.HasValue)
            .And("ToTimestamp is set", q => q.ToTimestamp.HasValue)
            .And("Limit is set", q => q.Limit == 100)
            .And("Offset is set", q => q.Offset == 50)
            .And("OrderByTimestampDescending is true", q => q.OrderByTimestampDescending)
            .AssertPassed();

    [Scenario("Timestamp range can span multiple days")]
    [Fact]
    public Task Timestamp_range_spans_multiple_days()
    {
        var now = DateTimeOffset.UtcNow;
        return Given("a query with week-long timestamp range", () => new OutcomeQuery
            {
                FromTimestamp = now.AddDays(-7),
                ToTimestamp = now
            })
            .Then("time span is approximately 7 days", q =>
                (q.ToTimestamp!.Value - q.FromTimestamp!.Value).TotalDays >= 6.9)
            .AssertPassed();
    }

    [Scenario("Pagination parameters work together")]
    [Fact]
    public Task Pagination_parameters_work_together()
        => Given("a query with pagination", () => new OutcomeQuery
            {
                ExperimentName = "test",
                Offset = 20,
                Limit = 10
            })
            .Then("Offset is set", q => q.Offset == 20)
            .And("Limit is set", q => q.Limit == 10)
            .AssertPassed();

    [Scenario("Zero offset is valid")]
    [Fact]
    public Task Zero_offset_is_valid()
        => Given("a query with zero offset", () => new OutcomeQuery { Offset = 0 })
            .Then("Offset is 0", q => q.Offset == 0)
            .AssertPassed();

    [Scenario("Limit of 1 is valid for single result")]
    [Fact]
    public Task Limit_of_one_is_valid()
        => Given("a query with limit of 1", () => new OutcomeQuery { Limit = 1 })
            .Then("Limit is 1", q => q.Limit == 1)
            .AssertPassed();

    [Scenario("OutcomeType filter for each type")]
    [Theory]
    [InlineData(OutcomeType.Binary)]
    [InlineData(OutcomeType.Continuous)]
    [InlineData(OutcomeType.Count)]
    [InlineData(OutcomeType.Duration)]
    public Task OutcomeType_filter_for_each_type(OutcomeType type)
        => Given("a query with outcome type filter", () => new OutcomeQuery { OutcomeType = type })
            .Then("OutcomeType is set", q => q.OutcomeType == type)
            .AssertPassed();

    [Scenario("Empty string filters are distinct from null")]
    [Fact]
    public Task Empty_string_filters_distinct_from_null()
        => Given("a query with empty string experiment name", () => new OutcomeQuery { ExperimentName = "" })
            .Then("ExperimentName is empty string", q => q.ExperimentName == "")
            .And("ExperimentName is not null", q => q.ExperimentName != null)
            .AssertPassed();

    [Scenario("Descending order flag defaults to false")]
    [Fact]
    public Task Descending_order_defaults_to_false()
        => Given("a default query", () => new OutcomeQuery())
            .Then("OrderByTimestampDescending is false", q => !q.OrderByTimestampDescending)
            .AssertPassed();

    [Scenario("Descending order can be enabled")]
    [Fact]
    public Task Descending_order_can_be_enabled()
        => Given("a query with descending order", () => new OutcomeQuery { OrderByTimestampDescending = true })
            .Then("OrderByTimestampDescending is true", q => q.OrderByTimestampDescending)
            .AssertPassed();

    [Scenario("Large limit value is valid")]
    [Fact]
    public Task Large_limit_value_is_valid()
        => Given("a query with large limit", () => new OutcomeQuery { Limit = 10000 })
            .Then("Limit is set to large value", q => q.Limit == 10000)
            .AssertPassed();

    [Scenario("FromTimestamp can equal ToTimestamp for point-in-time query")]
    [Fact]
    public Task Point_in_time_timestamp_query()
    {
        var pointInTime = DateTimeOffset.UtcNow;
        return Given("a point-in-time query", () => new OutcomeQuery
            {
                FromTimestamp = pointInTime,
                ToTimestamp = pointInTime
            })
            .Then("FromTimestamp equals ToTimestamp", q => q.FromTimestamp == q.ToTimestamp)
            .AssertPassed();
    }

    [Scenario("Query can filter by trial key only")]
    [Fact]
    public Task Filter_by_trial_key_only()
        => Given("a query with only trial key", () => new OutcomeQuery { TrialKey = "variant-a" })
            .Then("TrialKey is set", q => q.TrialKey == "variant-a")
            .And("ExperimentName is null", q => q.ExperimentName == null)
            .AssertPassed();

    [Scenario("Query can combine multiple filters")]
    [Fact]
    public Task Combine_multiple_filters()
        => Given("a query with multiple filters", () => new OutcomeQuery
            {
                ExperimentName = "checkout-exp",
                TrialKey = "treatment",
                MetricName = "conversion",
                OutcomeType = OutcomeType.Binary
            })
            .Then("all filters are set", q =>
                q.ExperimentName == "checkout-exp" &&
                q.TrialKey == "treatment" &&
                q.MetricName == "conversion" &&
                q.OutcomeType == OutcomeType.Binary)
            .AssertPassed();
}
