using ExperimentFramework.AutoStop;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.AutoStop;

[Feature("ExperimentData and VariantData provide experiment statistics")]
public sealed class ExperimentDataTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("ExperimentData with required properties")]
    [Fact]
    public Task ExperimentData_with_required_properties()
        => Given("experiment data", () =>
            {
                var startTime = DateTimeOffset.UtcNow.AddDays(-7);
                return new ExperimentData
                {
                    ExperimentName = "test-experiment",
                    StartedAt = startTime,
                    Variants = new List<VariantData>()
                };
            })
            .Then("name is set", data => data.ExperimentName == "test-experiment")
            .And("started at is set", data => data.StartedAt != default)
            .And("variants is set", data => data.Variants != null)
            .AssertPassed();

    [Scenario("VariantData calculates conversion rate")]
    [Fact]
    public Task VariantData_calculates_conversion_rate()
        => Given("variant with samples and successes", () => new VariantData
            {
                Key = "control",
                SampleSize = 1000,
                Successes = 150
            })
            .Then("conversion rate is calculated", v => Math.Abs(v.ConversionRate - 0.15) < 0.001)
            .AssertPassed();

    [Scenario("VariantData conversion rate is 0 with no samples")]
    [Fact]
    public Task VariantData_zero_conversion_with_no_samples()
        => Given("variant with no samples", () => new VariantData
            {
                Key = "test",
                SampleSize = 0,
                Successes = 0
            })
            .Then("conversion rate is 0", v => Math.Abs(v.ConversionRate) < 0.0001)
            .AssertPassed();

    [Scenario("VariantData calculates mean")]
    [Fact]
    public Task VariantData_calculates_mean()
        => Given("variant with value sum", () => new VariantData
            {
                Key = "test",
                SampleSize = 100,
                ValueSum = 500.0
            })
            .Then("mean is calculated", v => Math.Abs(v.Mean - 5.0) < 0.001)
            .AssertPassed();

    [Scenario("VariantData mean is 0 with no samples")]
    [Fact]
    public Task VariantData_zero_mean_with_no_samples()
        => Given("variant with no samples", () => new VariantData
            {
                Key = "test",
                SampleSize = 0,
                ValueSum = 0
            })
            .Then("mean is 0", v => Math.Abs(v.Mean) < 0.0001)
            .AssertPassed();

    [Scenario("VariantData calculates variance")]
    [Fact]
    public Task VariantData_calculates_variance()
        => Given("variant with value statistics", () => new VariantData
            {
                Key = "test",
                SampleSize = 10,
                ValueSum = 50.0,
                ValueSumSquared = 290.0  // Values: 2,3,4,5,5,5,6,7,6,7 sum=50, sumsq=290
            })
            .Then("variance is calculated", v =>
            {
                // Sample variance = (sum_sq - sum^2/n) / (n-1) = (290 - 2500/10) / 9 = (290 - 250) / 9 = 40/9
                var expected = 40.0 / 9.0;
                return Math.Abs(v.Variance - expected) < 0.001;
            })
            .AssertPassed();

    [Scenario("VariantData variance is 0 with single sample")]
    [Fact]
    public Task VariantData_zero_variance_with_single_sample()
        => Given("variant with single sample", () => new VariantData
            {
                Key = "test",
                SampleSize = 1,
                ValueSum = 5.0,
                ValueSumSquared = 25.0
            })
            .Then("variance is 0", v => Math.Abs(v.Variance) < 0.0001)
            .AssertPassed();

    [Scenario("VariantData IsControl property")]
    [Fact]
    public Task VariantData_IsControl_property()
        => Given("control variant", () => new VariantData { Key = "control", IsControl = true })
            .Then("is control", v => v.IsControl)
            .And("non-control variant", _ =>
            {
                var treatment = new VariantData { Key = "treatment", IsControl = false };
                return !treatment.IsControl;
            })
            .AssertPassed();

    [Scenario("VariantData properties are mutable")]
    [Fact]
    public Task VariantData_properties_mutable()
        => Given("a variant", () => new VariantData { Key = "test" })
            .When("modifying properties", v =>
            {
                v.SampleSize = 100;
                v.Successes = 50;
                v.ValueSum = 200.0;
                v.ValueSumSquared = 500.0;
                return v;
            })
            .Then("sample size is updated", v => v.SampleSize == 100)
            .And("successes is updated", v => v.Successes == 50)
            .And("value sum is updated", v => Math.Abs(v.ValueSum - 200.0) < 0.001)
            .And("value sum squared is updated", v => Math.Abs(v.ValueSumSquared - 500.0) < 0.001)
            .AssertPassed();
}
