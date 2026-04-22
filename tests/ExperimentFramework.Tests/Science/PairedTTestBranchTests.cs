using ExperimentFramework.Science.Statistics;
using ExperimentFramework.Science.Models.Results;

namespace ExperimentFramework.Tests.Science;

/// <summary>
/// Additional PairedTTest tests covering one-sided alternatives and validation branches.
/// </summary>
public class PairedTTestBranchTests
{
    // ───────────────────────── Argument validation ─────────────────────────

    [Fact]
    public void Perform_ThrowsForNullBefore()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PairedTTest.Instance.Perform(null!, new double[] { 1, 2, 3 }));
    }

    [Fact]
    public void Perform_ThrowsForNullAfter()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PairedTTest.Instance.Perform(new double[] { 1, 2, 3 }, null!));
    }

    [Fact]
    public void Perform_ThrowsWhenBeforeHasFewerThan2Observations()
    {
        Assert.Throws<ArgumentException>(() =>
            PairedTTest.Instance.Perform(new double[] { 1.0 }, new double[] { 2.0 }));
    }

    [Fact]
    public void Perform_ThrowsForAlphaAtBoundary()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PairedTTest.Instance.Perform(
                new double[] { 1, 2, 3 }, new double[] { 2, 3, 4 }, alpha: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            PairedTTest.Instance.Perform(
                new double[] { 1, 2, 3 }, new double[] { 2, 3, 4 }, alpha: 1));
    }

    // ───────────────────────── One-sided: Greater ─────────────────────────

    [Fact]
    public void Perform_Greater_WithPositiveDifference_IsSignificant()
    {
        // After is consistently higher — strong improvement
        var before = new double[] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };
        var after = new double[] { 20, 20, 20, 20, 20, 20, 20, 20, 20, 20 };

        var result = PairedTTest.Instance.Perform(before, after,
            alternativeType: AlternativeHypothesisType.Greater);

        Assert.True(result.IsSignificant);
        Assert.Equal(AlternativeHypothesisType.Greater, result.AlternativeType);
        // CI: [lower, +inf)
        Assert.True(double.IsPositiveInfinity(result.ConfidenceIntervalUpper));
    }

    [Fact]
    public void Perform_Greater_WithNegativeDifference_IsNotSignificant()
    {
        // After is lower — negative mean difference
        var before = new double[] { 20, 20, 20, 20, 20 };
        var after = new double[] { 10, 10, 10, 10, 10 };

        var result = PairedTTest.Instance.Perform(before, after,
            alternativeType: AlternativeHypothesisType.Greater);

        Assert.False(result.IsSignificant);
    }

    // ───────────────────────── One-sided: Less ─────────────────────────

    [Fact]
    public void Perform_Less_WithNegativeDifference_IsSignificant()
    {
        // After is consistently lower
        var before = new double[] { 20, 20, 20, 20, 20, 20, 20, 20, 20, 20 };
        var after = new double[] { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 };

        var result = PairedTTest.Instance.Perform(before, after,
            alternativeType: AlternativeHypothesisType.Less);

        Assert.True(result.IsSignificant);
        Assert.Equal(AlternativeHypothesisType.Less, result.AlternativeType);
        // CI: (-inf, upper]
        Assert.True(double.IsNegativeInfinity(result.ConfidenceIntervalLower));
    }

    [Fact]
    public void Perform_Less_WithPositiveDifference_IsNotSignificant()
    {
        var before = new double[] { 10, 10, 10, 10, 10 };
        var after = new double[] { 20, 20, 20, 20, 20 };

        var result = PairedTTest.Instance.Perform(before, after,
            alternativeType: AlternativeHypothesisType.Less);

        Assert.False(result.IsSignificant);
    }

    // ───────────────────────── Details ─────────────────────────

    [Fact]
    public void Perform_IncludesDetailsInResult()
    {
        var before = new double[] { 10, 20, 30, 40, 50 };
        var after = new double[] { 15, 25, 35, 45, 55 };

        var result = PairedTTest.Instance.Perform(before, after);

        Assert.NotNull(result.Details);
        Assert.True(result.Details.ContainsKey("mean_difference"));
        Assert.True(result.Details.ContainsKey("std_difference"));
        Assert.True(result.Details.ContainsKey("standard_error"));
        Assert.True(result.Details.ContainsKey("before_mean"));
        Assert.True(result.Details.ContainsKey("after_mean"));
    }

    // ───────────────────────── Singleton ─────────────────────────

    [Fact]
    public void Instance_IsSingleton()
    {
        Assert.Same(PairedTTest.Instance, PairedTTest.Instance);
    }

    [Fact]
    public void Name_IsExpected()
    {
        Assert.Equal("Paired t-Test", PairedTTest.Instance.Name);
    }
}
