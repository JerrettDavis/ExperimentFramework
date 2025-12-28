using ExperimentFramework.Rollout;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Rollout;

[Feature("Staged rollout options provide time-based percentage calculation")]
public sealed class StagedRolloutOptionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Empty stages returns 0 percent")]
    [Fact]
    public Task Empty_stages_returns_zero()
        => Given("an empty staged rollout options", () => new StagedRolloutOptions())
            .When("getting current percentage", options => options.GetCurrentPercentage())
            .Then("returns 0", percentage => percentage == 0)
            .AssertPassed();

    [Scenario("Single stage that has started returns its percentage")]
    [Fact]
    public Task Single_active_stage_returns_percentage()
        => Given("a staged rollout with one active stage", () => new StagedRolloutOptions
            {
                Stages =
                [
                    new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(-1), Percentage = 50 }
                ]
            })
            .When("getting current percentage", options => options.GetCurrentPercentage())
            .Then("returns stage percentage", percentage => percentage == 50)
            .AssertPassed();

    [Scenario("Stage not yet started returns 0")]
    [Fact]
    public Task Future_stage_returns_zero()
        => Given("a staged rollout with only future stages", () => new StagedRolloutOptions
            {
                Stages =
                [
                    new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(1), Percentage = 50 }
                ]
            })
            .When("getting current percentage", options => options.GetCurrentPercentage())
            .Then("returns 0", percentage => percentage == 0)
            .AssertPassed();

    [Scenario("Multiple stages returns most recent active percentage")]
    [Fact]
    public Task Multiple_stages_returns_most_recent()
        => Given("a staged rollout with multiple stages", () => new StagedRolloutOptions
            {
                Stages =
                [
                    new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(-3), Percentage = 10 },
                    new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(-2), Percentage = 25 },
                    new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(-1), Percentage = 50 },
                    new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddDays(1), Percentage = 100 }
                ]
            })
            .When("getting current percentage", options => options.GetCurrentPercentage())
            .Then("returns most recent active stage percentage", percentage => percentage == 50)
            .AssertPassed();

    [Scenario("Specific time returns correct stage")]
    [Fact]
    public Task Specific_time_returns_correct_stage()
        => Given("a staged rollout and a specific time", () =>
            {
                var baseTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
                return (
                    Options: new StagedRolloutOptions
                    {
                        Stages =
                        [
                            new RolloutStage { StartsAt = baseTime, Percentage = 10 },
                            new RolloutStage { StartsAt = baseTime.AddDays(7), Percentage = 50 },
                            new RolloutStage { StartsAt = baseTime.AddDays(14), Percentage = 100 }
                        ]
                    },
                    QueryTime: baseTime.AddDays(10)
                );
            })
            .When("getting percentage at specific time", data => data.Options.GetCurrentPercentage(data.QueryTime))
            .Then("returns correct stage percentage", percentage => percentage == 50)
            .AssertPassed();

    [Scenario("Default options are set correctly")]
    [Fact]
    public Task Default_options_are_set()
        => Given("a new staged rollout options", () => new StagedRolloutOptions())
            .Then("included key is 'true'", options => options.IncludedKey == "true")
            .And("excluded key is null", options => options.ExcludedKey == null)
            .And("seed is null", options => options.Seed == null)
            .And("stages is empty", options => options.Stages.Count == 0)
            .AssertPassed();

    [Scenario("Options properties can be set")]
    [Fact]
    public Task Options_properties_can_be_set()
        => Given("staged rollout options with custom values", () => new StagedRolloutOptions
            {
                IncludedKey = "enabled",
                ExcludedKey = "disabled",
                Seed = "my-seed"
            })
            .Then("included key is set", options => options.IncludedKey == "enabled")
            .And("excluded key is set", options => options.ExcludedKey == "disabled")
            .And("seed is set", options => options.Seed == "my-seed")
            .AssertPassed();

    [Scenario("GetCurrentPercentage with null time uses UTC now")]
    [Fact]
    public Task GetCurrentPercentage_null_time_uses_now()
        => Given("staged rollout with active stage", () => new StagedRolloutOptions
            {
                Stages = [new RolloutStage { StartsAt = DateTimeOffset.UtcNow.AddHours(-1), Percentage = 75 }]
            })
            .When("getting percentage with null time", options => options.GetCurrentPercentage(null))
            .Then("returns correct percentage", percentage => percentage == 75)
            .AssertPassed();

    [Scenario("RolloutStage has all properties")]
    [Fact]
    public Task RolloutStage_has_all_properties()
    {
        var time = new DateTimeOffset(2024, 6, 1, 12, 0, 0, TimeSpan.Zero);
        return Given("a rollout stage with all properties", () => new RolloutStage
            {
                StartsAt = time,
                Percentage = 25,
                Description = "Initial 25% rollout"
            })
            .Then("starts at is set", stage => stage.StartsAt == time)
            .And("percentage is set", stage => stage.Percentage == 25)
            .And("description is set", stage => stage.Description == "Initial 25% rollout")
            .AssertPassed();
    }

    [Scenario("RolloutStage defaults")]
    [Fact]
    public Task RolloutStage_defaults()
        => Given("a default rollout stage", () => new RolloutStage())
            .Then("starts at is default", stage => stage.StartsAt == default)
            .And("percentage is 0", stage => stage.Percentage == 0)
            .And("description is null", stage => stage.Description == null)
            .AssertPassed();

    [Scenario("RolloutModes constant has correct value")]
    [Fact]
    public Task RolloutModes_constant_value()
        => Given("the RolloutModes constant", () => RolloutModes.Rollout)
            .Then("value is 'Rollout'", mode => mode == "Rollout")
            .AssertPassed();

    [Scenario("StagedRolloutModes constant has correct value")]
    [Fact]
    public Task StagedRolloutModes_constant_value()
        => Given("the StagedRolloutModes constant", () => StagedRolloutModes.StagedRollout)
            .Then("value is 'StagedRollout'", mode => mode == "StagedRollout")
            .AssertPassed();

    [Scenario("Stages at exact current time are included")]
    [Fact]
    public Task Stages_at_exact_time_included()
    {
        var now = DateTimeOffset.UtcNow;
        return Given("staged rollout with stage starting now", () => new StagedRolloutOptions
            {
                Stages = [new RolloutStage { StartsAt = now, Percentage = 50 }]
            })
            .When("getting percentage at exact start time", options => options.GetCurrentPercentage(now))
            .Then("returns stage percentage", percentage => percentage == 50)
            .AssertPassed();
    }

    [Scenario("Unordered stages are handled correctly")]
    [Fact]
    public Task Unordered_stages_handled()
    {
        var baseTime = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return Given("staged rollout with unordered stages", () => new StagedRolloutOptions
            {
                Stages =
                [
                    new RolloutStage { StartsAt = baseTime.AddDays(7), Percentage = 50 },
                    new RolloutStage { StartsAt = baseTime, Percentage = 10 },
                    new RolloutStage { StartsAt = baseTime.AddDays(14), Percentage = 100 }
                ]
            })
            .When("getting percentage", options => options.GetCurrentPercentage(baseTime.AddDays(10)))
            .Then("returns correct most recent stage", percentage => percentage == 50)
            .AssertPassed();
    }
}
