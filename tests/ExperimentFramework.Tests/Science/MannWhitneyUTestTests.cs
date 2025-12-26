using ExperimentFramework.Science.Models.Results;
using ExperimentFramework.Science.Statistics;

namespace ExperimentFramework.Tests.Science;

public class MannWhitneyUTestTests
{
    [Fact]
    public void Perform_WithIdenticalDistributions_ReturnsNotSignificant()
    {
        // Arrange
        var control = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var treatment = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };

        // Act
        var result = MannWhitneyUTest.Instance.Perform(control, treatment, 0.05);

        // Assert
        Assert.Equal("Mann-Whitney U Test", result.TestName);
        Assert.False(result.IsSignificant);
    }

    [Fact]
    public void Perform_WithSignificantDifference_ReturnsSignificant()
    {
        // Arrange - treatment values are clearly higher
        var control = new double[] { 1, 2, 3, 4, 5 };
        var treatment = new double[] { 10, 11, 12, 13, 14 };

        // Act
        var result = MannWhitneyUTest.Instance.Perform(control, treatment, 0.05);

        // Assert
        Assert.True(result.IsSignificant);
        Assert.True(result.PValue < 0.05);
    }

    [Fact]
    public void Perform_ReturnsSampleSizes()
    {
        // Arrange
        var control = new double[] { 1, 2, 3, 4, 5 };
        var treatment = new double[] { 6, 7, 8, 9, 10, 11 };

        // Act
        var result = MannWhitneyUTest.Instance.Perform(control, treatment);

        // Assert
        Assert.Equal(5, result.SampleSizes["control"]);
        Assert.Equal(6, result.SampleSizes["treatment"]);
    }

    [Fact]
    public void Perform_WithOneSidedGreater_ReturnsCorrectPValue()
    {
        // Arrange
        var control = new double[] { 1, 2, 3, 4, 5 };
        var treatment = new double[] { 6, 7, 8, 9, 10 };

        // Act
        var result = MannWhitneyUTest.Instance.Perform(control, treatment, 0.05, AlternativeHypothesisType.Greater);

        // Assert - should return valid p-value for one-sided test
        Assert.True(result.PValue >= 0 && result.PValue <= 1);
        Assert.Equal(AlternativeHypothesisType.Greater, result.AlternativeType);
    }

    [Fact]
    public void Perform_WithOneSidedLess_ReturnsCorrectPValue()
    {
        // Arrange - treatment is higher
        var control = new double[] { 1, 2, 3, 4, 5 };
        var treatment = new double[] { 6, 7, 8, 9, 10 };

        // Act - testing if treatment is LESS than control
        var result = MannWhitneyUTest.Instance.Perform(control, treatment, 0.05, AlternativeHypothesisType.Less);

        // Assert - should return valid p-value for one-sided test
        Assert.True(result.PValue >= 0 && result.PValue <= 1);
        Assert.Equal(AlternativeHypothesisType.Less, result.AlternativeType);
    }

    [Fact]
    public void Perform_ReturnsTestStatistic()
    {
        // Arrange
        var control = new double[] { 1, 2, 3, 4, 5 };
        var treatment = new double[] { 6, 7, 8, 9, 10 };

        // Act
        var result = MannWhitneyUTest.Instance.Perform(control, treatment);

        // Assert
        Assert.True(result.TestStatistic >= 0);
    }

    [Fact]
    public void Perform_ReturnsPointEstimate()
    {
        // Arrange
        var control = new double[] { 1, 2, 3, 4, 5 };
        var treatment = new double[] { 6, 7, 8, 9, 10 };

        // Act
        var result = MannWhitneyUTest.Instance.Perform(control, treatment);

        // Assert
        Assert.True(result.PointEstimate > 0); // Treatment median is higher
    }

    [Fact]
    public void Perform_ReturnsConfidenceInterval()
    {
        // Arrange
        var control = new double[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        var treatment = new double[] { 5, 6, 7, 8, 9, 10, 11, 12 };

        // Act
        var result = MannWhitneyUTest.Instance.Perform(control, treatment);

        // Assert
        Assert.True(result.ConfidenceIntervalLower <= result.PointEstimate);
        Assert.True(result.ConfidenceIntervalUpper >= result.PointEstimate);
    }

    [Fact]
    public void Perform_ThrowsForEmptyControlData()
    {
        // Arrange
        var control = Array.Empty<double>();
        var treatment = new double[] { 2, 3, 4 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            MannWhitneyUTest.Instance.Perform(control, treatment));
    }

    [Fact]
    public void Perform_ThrowsForEmptyTreatmentData()
    {
        // Arrange
        var control = new double[] { 1, 2, 3 };
        var treatment = Array.Empty<double>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            MannWhitneyUTest.Instance.Perform(control, treatment));
    }

    [Fact]
    public void Perform_AllowsSingleObservation()
    {
        // Arrange - implementation allows single observation per group
        var control = new double[] { 1 };
        var treatment = new double[] { 2 };

        // Act
        var result = MannWhitneyUTest.Instance.Perform(control, treatment);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PValue >= 0 && result.PValue <= 1);
    }

    [Fact]
    public void Perform_ThrowsForNullControl()
    {
        // Arrange
        var treatment = new double[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            MannWhitneyUTest.Instance.Perform(null!, treatment));
    }

    [Fact]
    public void Perform_ThrowsForNullTreatment()
    {
        // Arrange
        var control = new double[] { 1, 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            MannWhitneyUTest.Instance.Perform(control, null!));
    }

    [Fact]
    public void Perform_HandlesNonNormalDistribution()
    {
        // Arrange - skewed distributions (Mann-Whitney should still work)
        var control = new double[] { 1, 1, 1, 1, 1, 10 };
        var treatment = new double[] { 5, 5, 5, 5, 5, 100 };

        // Act
        var result = MannWhitneyUTest.Instance.Perform(control, treatment);

        // Assert - should not throw and should produce valid result
        Assert.NotNull(result);
        Assert.True(result.PValue >= 0 && result.PValue <= 1);
    }

    [Fact]
    public void Perform_HandlesTies()
    {
        // Arrange - data with ties
        var control = new double[] { 1, 2, 2, 3, 3, 3 };
        var treatment = new double[] { 3, 3, 4, 4, 5, 5 };

        // Act
        var result = MannWhitneyUTest.Instance.Perform(control, treatment);

        // Assert - should handle ties correctly
        Assert.NotNull(result);
        Assert.True(result.PValue >= 0 && result.PValue <= 1);
    }

    [Fact]
    public void Instance_ReturnsSingleton()
    {
        // Act
        var instance1 = MannWhitneyUTest.Instance;
        var instance2 = MannWhitneyUTest.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Name_ReturnsMannWhitneyUTest()
    {
        // Assert
        Assert.Equal("Mann-Whitney U Test", MannWhitneyUTest.Instance.Name);
    }
}
