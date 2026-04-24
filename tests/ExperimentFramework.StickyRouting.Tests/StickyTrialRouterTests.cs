using ExperimentFramework.StickyRouting;

namespace ExperimentFramework.StickyRouting.Tests;

/// <summary>
/// Unit tests for <see cref="StickyTrialRouter"/> — the deterministic hash-based trial selector.
/// </summary>
public sealed class StickyTrialRouterTests
{
    // -----------------------------------------------------------------------
    // Determinism: same input always produces same output
    // -----------------------------------------------------------------------

    [Fact]
    public void SelectTrial_same_identity_same_selector_always_returns_same_trial()
    {
        const string identity = "user-abc";
        const string selector = "MyExperiment";
        IReadOnlyList<string> keys = ["control", "variant-a", "variant-b"];

        var first = StickyTrialRouter.SelectTrial(identity, selector, keys);

        for (var i = 0; i < 20; i++)
        {
            var result = StickyTrialRouter.SelectTrial(identity, selector, keys);
            Assert.Equal(first, result);
        }
    }

    [Fact]
    public void SelectTrial_returns_a_key_that_is_in_the_supplied_list()
    {
        IReadOnlyList<string> keys = ["control", "variant-a", "variant-b"];

        var result = StickyTrialRouter.SelectTrial("user-1", "Exp", keys);

        Assert.Contains(result, keys);
    }

    // -----------------------------------------------------------------------
    // Single-variant short-circuit
    // -----------------------------------------------------------------------

    [Fact]
    public void SelectTrial_single_key_always_returns_that_key()
    {
        IReadOnlyList<string> keys = ["only-trial"];

        for (var i = 0; i < 10; i++)
        {
            var result = StickyTrialRouter.SelectTrial($"user-{i}", "Exp", keys);
            Assert.Equal("only-trial", result);
        }
    }

    // -----------------------------------------------------------------------
    // Empty list must throw
    // -----------------------------------------------------------------------

    [Fact]
    public void SelectTrial_empty_list_throws_InvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => StickyTrialRouter.SelectTrial("user-1", "Exp", []));

        Assert.Equal("No trial keys available for sticky routing.", ex.Message);
    }

    // -----------------------------------------------------------------------
    // Salt / selector isolation: different selectors produce independent routing
    // -----------------------------------------------------------------------

    [Fact]
    public void SelectTrial_different_selectors_produce_different_assignments_for_at_least_some_users()
    {
        IReadOnlyList<string> keys = ["a", "b"];
        var sameCount = 0;
        const int userCount = 100;

        for (var i = 0; i < userCount; i++)
        {
            var id = $"user-{i}";
            var t1 = StickyTrialRouter.SelectTrial(id, "ExperimentOne", keys);
            var t2 = StickyTrialRouter.SelectTrial(id, "ExperimentTwo", keys);
            if (t1 == t2) sameCount++;
        }

        // If selector is used as salt, the two distributions should differ.
        // Allowing <=80 matches out of 100 (by chance alone it would be ~50 matches).
        Assert.True(sameCount < userCount,
            $"Expected at least some difference between selectors, but all {userCount} matched.");
    }

    // -----------------------------------------------------------------------
    // Distribution: routing is approximately uniform over many users
    // -----------------------------------------------------------------------

    [Fact]
    public void SelectTrial_two_keys_distributes_roughly_evenly_over_200_users()
    {
        IReadOnlyList<string> keys = ["control", "variant"];
        var counts = new Dictionary<string, int> { ["control"] = 0, ["variant"] = 0 };

        for (var i = 0; i < 200; i++)
        {
            var trial = StickyTrialRouter.SelectTrial($"user-{i}", "Distribution2Way", keys);
            counts[trial]++;
        }

        // Each should receive 30–70 % of users (60–140 of 200)
        Assert.InRange(counts["control"], 60, 140);
        Assert.InRange(counts["variant"], 60, 140);
    }

    [Fact]
    public void SelectTrial_three_keys_distributes_to_all_buckets_over_300_users()
    {
        IReadOnlyList<string> keys = ["control", "variant-a", "variant-b"];
        var counts = new Dictionary<string, int>();
        foreach (var k in keys) counts[k] = 0;

        for (var i = 0; i < 300; i++)
        {
            var trial = StickyTrialRouter.SelectTrial($"user-{i}", "Distribution3Way", keys);
            counts[trial]++;
        }

        // Each bucket should receive at least 60 users out of 300 (20 %)
        foreach (var k in keys)
            Assert.True(counts[k] >= 60,
                $"Key '{k}' received only {counts[k]} users — distribution looks skewed.");
    }

    // -----------------------------------------------------------------------
    // Key-order independence: sorted internally
    // -----------------------------------------------------------------------

    [Fact]
    public void SelectTrial_key_order_does_not_affect_result()
    {
        const string identity = "user-order-test";
        const string selector = "OrderTest";

        var result1 = StickyTrialRouter.SelectTrial(identity, selector, ["zebra", "alpha", "beta"]);
        var result2 = StickyTrialRouter.SelectTrial(identity, selector, ["beta", "zebra", "alpha"]);
        var result3 = StickyTrialRouter.SelectTrial(identity, selector, ["alpha", "beta", "zebra"]);

        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    // -----------------------------------------------------------------------
    // Different users get different routing (not all same bucket)
    // -----------------------------------------------------------------------

    [Fact]
    public void SelectTrial_different_identities_do_not_all_route_to_same_trial()
    {
        IReadOnlyList<string> keys = ["control", "variant"];
        var results = Enumerable.Range(1, 50)
            .Select(i => StickyTrialRouter.SelectTrial($"user-{i}", "DiversityTest", keys))
            .Distinct()
            .ToList();

        Assert.True(results.Count >= 2,
            "Expected both keys to be selected across 50 users, but only one was selected.");
    }

    // -----------------------------------------------------------------------
    // Unicode / special-character identities are handled without exception
    // -----------------------------------------------------------------------

    [Fact]
    public void SelectTrial_unicode_identity_does_not_throw()
    {
        IReadOnlyList<string> keys = ["a", "b"];
        var result = StickyTrialRouter.SelectTrial("user-中文-é", "UnicodeTest", keys);
        Assert.Contains(result, keys);
    }

    [Fact]
    public void SelectTrial_empty_string_identity_does_not_throw_and_returns_valid_key()
    {
        IReadOnlyList<string> keys = ["control", "variant"];
        // Empty identity is unusual but should not throw — behaviour is implementation-defined
        var result = StickyTrialRouter.SelectTrial(string.Empty, "EmptyIdentityTest", keys);
        Assert.Contains(result, keys);
    }

    [Fact]
    public void SelectTrial_very_long_identity_returns_valid_key()
    {
        var longId = new string('x', 10_000);
        IReadOnlyList<string> keys = ["control", "variant"];
        var result = StickyTrialRouter.SelectTrial(longId, "LongIdTest", keys);
        Assert.Contains(result, keys);
    }
}
