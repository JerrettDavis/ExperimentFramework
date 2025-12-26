using ExperimentFramework.Science.Models.Results;
using ExperimentFramework.Science.Statistics;

namespace ExperimentFramework.Tests.Science;

public class TwoSampleTTestTests
{
    [Fact]
    public void Perform_ReturnsValidResult()
    {
        // Arrange
        var control = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var treatment = new double[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 };

        // Act
        var result = TwoSampleTTest.Instance.Perform(control, treatment, 0.05);

        // Assert
        Assert.Equal("Welch's Two-Sample t-Test", result.TestName);
        Assert.NotNull(result.SampleSizes);
        Assert.Equal(10, result.SampleSizes["control"]);
        Assert.Equal(10, result.SampleSizes["treatment"]);
    }

    [Fact]
    public void Perform_WithSignificantDifference_ReturnsSignificant()
    {
        // Arrange - treatment has much higher values
        var control = new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var treatment = new double[] { 20, 21, 22, 23, 24, 25, 26, 27, 28, 29 };

        // Act
        var result = TwoSampleTTest.Instance.Perform(control, treatment);

        // Assert
        Assert.True(result.IsSignificant);
        Assert.True(result.PValue < 0.05);
        Assert.True(result.PointEstimate > 0); // Treatment is higher
    }

    [Fact]
    public void Perform_WithOneSidedGreater_ReturnsCorrectPValue()
    {
        // Arrange
        var control = new double[] { 1, 2, 3, 4, 5 };
        var treatment = new double[] { 10, 11, 12, 13, 14 };

        // Act
        var twoSided = TwoSampleTTest.Instance.Perform(control, treatment, 0.05, AlternativeHypothesisType.TwoSided);
        var oneSided = TwoSampleTTest.Instance.Perform(control, treatment, 0.05, AlternativeHypothesisType.Greater);

        // Assert - one-sided p-value should be approximately half of two-sided
        Assert.True(oneSided.PValue < twoSided.PValue);
    }

    [Fact]
    public void Perform_ReturnsDegreesOfFreedom()
    {
        // Arrange
        var control = new double[] { 1, 2, 3, 4, 5 };
        var treatment = new double[] { 6, 7, 8, 9, 10 };

        // Act
        var result = TwoSampleTTest.Instance.Perform(control, treatment);

        // Assert
        Assert.NotNull(result.DegreesOfFreedom);
        Assert.True(result.DegreesOfFreedom > 0);
    }

    [Fact]
    public void Perform_ThrowsForInsufficientData()
    {
        // Arrange
        var control = new double[] { 1 }; // Only 1 observation
        var treatment = new double[] { 2, 3 };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            TwoSampleTTest.Instance.Perform(control, treatment));
    }
}
