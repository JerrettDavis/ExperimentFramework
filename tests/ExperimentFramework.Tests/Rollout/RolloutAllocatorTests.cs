using ExperimentFramework.Rollout;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Rollout;

[Feature("Rollout allocator provides consistent percentage-based user allocation")]
public sealed class RolloutAllocatorTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("0% rollout includes no users")]
    [Fact]
    public Task Zero_percent_includes_no_users()
        => Given("100 user identities", () => Enumerable.Range(1, 100).Select(i => $"user-{i}").ToList())
            .When("checking inclusion at 0%", identities =>
                identities.Count(id => RolloutAllocator.IsIncluded(id, "test-rollout", 0)))
            .Then("no users are included", count => count == 0)
            .AssertPassed();

    [Scenario("100% rollout includes all users")]
    [Fact]
    public Task Hundred_percent_includes_all_users()
        => Given("100 user identities", () => Enumerable.Range(1, 100).Select(i => $"user-{i}").ToList())
            .When("checking inclusion at 100%", identities =>
                identities.Count(id => RolloutAllocator.IsIncluded(id, "test-rollout", 100)))
            .Then("all users are included", count => count == 100)
            .AssertPassed();

    [Scenario("50% rollout includes approximately half of users")]
    [Fact]
    public Task Fifty_percent_includes_half_of_users()
        => Given("1000 user identities", () => Enumerable.Range(1, 1000).Select(i => $"user-{i}").ToList())
            .When("checking inclusion at 50%", identities =>
                identities.Count(id => RolloutAllocator.IsIncluded(id, "test-rollout", 50)))
            .Then("approximately half are included", count => count is >= 400 and <= 600)
            .AssertPassed();

    [Scenario("Same user always gets same allocation")]
    [Fact]
    public Task Same_user_consistent_allocation()
        => Given("a user identity", () => "user-123")
            .When("checking inclusion multiple times", id =>
            {
                var results = new List<bool>();
                for (var i = 0; i < 10; i++)
                {
                    results.Add(RolloutAllocator.IsIncluded(id, "test-rollout", 50));
                }
                return results;
            })
            .Then("all results are identical", results => results.Distinct().Count() == 1)
            .AssertPassed();

    [Scenario("Different rollout names produce different allocations")]
    [Fact]
    public Task Different_rollout_names_different_allocations()
        => Given("1000 user identities and two rollout names", () =>
            (Identities: Enumerable.Range(1, 1000).Select(i => $"user-{i}").ToList(),
             Rollout1: "rollout-a",
             Rollout2: "rollout-b"))
            .When("comparing allocations", data =>
            {
                var sameCount = data.Identities.Count(id =>
                    RolloutAllocator.IsIncluded(id, data.Rollout1, 50) ==
                    RolloutAllocator.IsIncluded(id, data.Rollout2, 50));
                return sameCount;
            })
            .Then("allocations differ for some users", sameCount => sameCount < 1000)
            .AssertPassed();

    [Scenario("Seed affects allocation")]
    [Fact]
    public Task Seed_affects_allocation()
        => Given("1000 user identities and two seeds", () =>
            (Identities: Enumerable.Range(1, 1000).Select(i => $"user-{i}").ToList(),
             Seed1: "seed-1",
             Seed2: "seed-2"))
            .When("comparing allocations with different seeds", data =>
            {
                var sameCount = data.Identities.Count(id =>
                    RolloutAllocator.IsIncluded(id, "rollout", 50, data.Seed1) ==
                    RolloutAllocator.IsIncluded(id, "rollout", 50, data.Seed2));
                return sameCount;
            })
            .Then("seeds produce different allocations", sameCount => sameCount < 1000)
            .AssertPassed();

    [Scenario("AllocateBucket distributes users across buckets")]
    [Fact]
    public Task Allocate_bucket_distributes_users()
        => Given("1000 user identities and 3 buckets with equal weights", () =>
            (Identities: Enumerable.Range(1, 1000).Select(i => $"user-{i}").ToList(),
             Weights: new[] { 33, 34, 33 }))
            .When("allocating buckets", data =>
            {
                var buckets = data.Identities
                    .Select(id => RolloutAllocator.AllocateBucket(id, "rollout", data.Weights))
                    .GroupBy(b => b)
                    .ToDictionary(g => g.Key, g => g.Count());
                return buckets;
            })
            .Then("all buckets receive users", buckets =>
                buckets.ContainsKey(0) && buckets.ContainsKey(1) && buckets.ContainsKey(2))
            .And("distribution is reasonable", buckets =>
                buckets.Values.All(c => c is >= 250 and <= 450))
            .AssertPassed();

    [Scenario("AllocateBucket throws for empty weights")]
    [Fact]
    public void Allocate_bucket_throws_for_empty_weights()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            RolloutAllocator.AllocateBucket("user-1", "rollout", Array.Empty<int>()));

        Assert.Equal("weights", exception.ParamName);
    }

    [Scenario("AllocateBucket with single weight always returns 0")]
    [Fact]
    public Task Allocate_bucket_single_weight_returns_zero()
        => Given("100 user identities and single bucket", () =>
            Enumerable.Range(1, 100).Select(i => $"user-{i}").ToList())
            .When("allocating to single bucket", identities =>
                identities.Select(id => RolloutAllocator.AllocateBucket(id, "rollout", new[] { 100 })).ToList())
            .Then("all users in bucket 0", buckets => buckets.All(b => b == 0))
            .AssertPassed();
}
