using ExperimentFramework.Science.Statistics;

namespace ExperimentFramework.Tests.Science;

public class ChiSquareTestTests
{
    [Fact]
    public void Perform_WithSimilarProportions_ReturnsNotSignificant()
    {
        // Arrange - similar success rates
        var control = new double[] { 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 }; // 50% success
        var treatment = new double[] { 1, 1, 1, 1, 1, 0, 0, 0, 0, 0 }; // 50% success

        // Act
        var result = ChiSquareTest.Instance.Perform(control, treatment);

        // Assert
        Assert.False(result.IsSignificant);
    }

    [Fact]
    public void Perform_WithDifferentProportions_ReturnsSignificant()
    {
        // Arrange - very different success rates
        var control = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }; // 5% success
        var treatment = new double[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 }; // 90% success

        // Act
        var result = ChiSquareTest.Instance.Perform(control, treatment);

        // Assert
        Assert.True(result.IsSignificant);
        Assert.True(result.PValue < 0.001);
    }

    [Fact]
    public void Perform_ReturnsProportionDifference()
    {
        // Arrange
        var control = new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // 0%
        var treatment = new double[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }; // 100%

        // Act
        var result = ChiSquareTest.Instance.Perform(control, treatment);

        // Assert
        Assert.Equal(1.0, result.PointEstimate); // 100% - 0% = 100%
    }

    [Fact]
    public void Perform_IncludesSuccessCountsInDetails()
    {
        // Arrange
        var control = new double[] { 1, 1, 0, 0, 0 }; // 2 successes
        var treatment = new double[] { 1, 1, 1, 1, 0 }; // 4 successes

        // Act
        var result = ChiSquareTest.Instance.Perform(control, treatment);

        // Assert
        Assert.NotNull(result.Details);
        Assert.Equal(2, result.Details["control_successes"]);
        Assert.Equal(4, result.Details["treatment_successes"]);
    }
}
