using ExperimentFramework.Science.Statistics;
using ExperimentFramework.Science.Models.Results;

namespace ExperimentFramework.Tests.Science;

/// <summary>
/// Tests for ChiSquareTest branches not covered by ChiSquareTestTests.cs —
/// primarily one-sided alternatives and argument validation.
/// </summary>
public class ChiSquareTestBranchTests
{
    // ───────────────────────── Argument validation ─────────────────────────

    [Fact]
    public void Perform_ThrowsForNullControlData()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ChiSquareTest.Instance.Perform(null!, new double[] { 1, 0 }));
    }

    [Fact]
    public void Perform_ThrowsForNullTreatmentData()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ChiSquareTest.Instance.Perform(new double[] { 1, 0 }, null!));
    }

    [Fact]
    public void Perform_ThrowsForEmptyControlData()
    {
        Assert.Throws<ArgumentException>(() =>
            ChiSquareTest.Instance.Perform(Array.Empty<double>(), new double[] { 1 }));
    }

    [Fact]
    public void Perform_ThrowsForEmptyTreatmentData()
    {
        Assert.Throws<ArgumentException>(() =>
            ChiSquareTest.Instance.Perform(new double[] { 1 }, Array.Empty<double>()));
    }

    [Fact]
    public void Perform_ThrowsForAlphaAtZero()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChiSquareTest.Instance.Perform(new double[] { 1, 0 }, new double[] { 1, 0 }, alpha: 0));
    }

    [Fact]
    public void Perform_ThrowsForAlphaAtOne()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ChiSquareTest.Instance.Perform(new double[] { 1, 0 }, new double[] { 1, 0 }, alpha: 1));
    }

    // ───────────────────────── One-sided: Greater ─────────────────────────

    [Fact]
    public void Perform_Greater_WhenTreatmentIsHigher_PValueIsLowHalf()
    {
        // Treatment clearly higher proportion
        var control = Enumerable.Repeat(0.0, 18).Concat(Enumerable.Repeat(1.0, 2)).ToArray(); // 10%
        var treatment = Enumerable.Repeat(1.0, 18).Concat(Enumerable.Repeat(0.0, 2)).ToArray(); // 90%

        var twoSided = ChiSquareTest.Instance.Perform(control, treatment,
            alternativeType: AlternativeHypothesisType.TwoSided);
        var greaterAlt = ChiSquareTest.Instance.Perform(control, treatment,
            alternativeType: AlternativeHypothesisType.Greater);

        // When diff > 0 and alt=Greater, p-value should be about half of two-sided
        Assert.True(greaterAlt.PValue < twoSided.PValue);
        Assert.Equal(AlternativeHypothesisType.Greater, greaterAlt.AlternativeType);
    }

    [Fact]
    public void Perform_Greater_WhenTreatmentIsLower_PValueIsLargeHalf()
    {
        // Treatment clearly lower proportion than control
        var control = Enumerable.Repeat(1.0, 18).Concat(Enumerable.Repeat(0.0, 2)).ToArray(); // 90%
        var treatment = Enumerable.Repeat(0.0, 18).Concat(Enumerable.Repeat(1.0, 2)).ToArray(); // 10%

        var result = ChiSquareTest.Instance.Perform(control, treatment,
            alternativeType: AlternativeHypothesisType.Greater);

        // diff < 0 and alt=Greater: p-value = 1 - pValue/2 (large)
        Assert.True(result.PValue > 0.5);
        // CI upper bound = 1.0 for Greater
        Assert.Equal(1.0, result.ConfidenceIntervalUpper);
    }

    // ───────────────────────── One-sided: Less ─────────────────────────

    [Fact]
    public void Perform_Less_WhenTreatmentIsLower_PValueIsLowHalf()
    {
        // Treatment clearly lower
        var control = Enumerable.Repeat(1.0, 18).Concat(Enumerable.Repeat(0.0, 2)).ToArray(); // 90%
        var treatment = Enumerable.Repeat(0.0, 18).Concat(Enumerable.Repeat(1.0, 2)).ToArray(); // 10%

        var twoSided = ChiSquareTest.Instance.Perform(control, treatment,
            alternativeType: AlternativeHypothesisType.TwoSided);
        var lessAlt = ChiSquareTest.Instance.Perform(control, treatment,
            alternativeType: AlternativeHypothesisType.Less);

        Assert.True(lessAlt.PValue < twoSided.PValue);
        Assert.Equal(AlternativeHypothesisType.Less, lessAlt.AlternativeType);
    }

    [Fact]
    public void Perform_Less_WhenTreatmentIsHigher_PValueIsLargeHalf()
    {
        var control = Enumerable.Repeat(0.0, 18).Concat(Enumerable.Repeat(1.0, 2)).ToArray(); // 10%
        var treatment = Enumerable.Repeat(1.0, 18).Concat(Enumerable.Repeat(0.0, 2)).ToArray(); // 90%

        var result = ChiSquareTest.Instance.Perform(control, treatment,
            alternativeType: AlternativeHypothesisType.Less);

        // diff > 0 and alt=Less: p-value = 1 - pValue/2 (large)
        Assert.True(result.PValue > 0.5);
        // CI lower bound = -1.0 for Less
        Assert.Equal(-1.0, result.ConfidenceIntervalLower);
    }

    // ───────────────────────── Edge case: zero denominator ─────────────────────────

    [Fact]
    public void Perform_AllSameOutcome_DoesNotThrow()
    {
        // All successes in both groups → denominator of chi-square is zero
        var control = new double[] { 1.0, 1.0, 1.0, 1.0, 1.0 };
        var treatment = new double[] { 1.0, 1.0, 1.0, 1.0, 1.0 };

        var result = ChiSquareTest.Instance.Perform(control, treatment);

        // Should return 0 chi-square (no variance)
        Assert.Equal(0.0, result.TestStatistic, precision: 10);
    }

    // ───────────────────────── Singleton ─────────────────────────

    [Fact]
    public void Instance_IsSingleton()
    {
        Assert.Same(ChiSquareTest.Instance, ChiSquareTest.Instance);
    }

    [Fact]
    public void Name_IsExpected()
    {
        Assert.Equal("Chi-Square Test for Independence", ChiSquareTest.Instance.Name);
    }
}
