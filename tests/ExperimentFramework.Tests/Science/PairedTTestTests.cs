using ExperimentFramework.Science.Statistics;

namespace ExperimentFramework.Tests.Science;

public class PairedTTestTests
{
    [Fact]
    public void Perform_WithNoChange_ReturnsNotSignificant()
    {
        // Arrange - before and after are the same
        var before = new double[] { 10, 20, 30, 40, 50 };
        var after = new double[] { 10, 20, 30, 40, 50 };

        // Act
        var result = PairedTTest.Instance.Perform(before, after);

        // Assert
        Assert.False(result.IsSignificant);
        Assert.Equal(0, result.PointEstimate, precision: 10);
    }

    [Fact]
    public void Perform_WithImprovement_ReturnsSignificant()
    {
        // Arrange - consistent improvement
        var before = new double[] { 10, 20, 30, 40, 50 };
        var after = new double[] { 20, 30, 40, 50, 60 }; // All improved by 10

        // Act
        var result = PairedTTest.Instance.Perform(before, after);

        // Assert
        Assert.True(result.IsSignificant);
        Assert.Equal(10.0, result.PointEstimate);
    }

    [Fact]
    public void Perform_ReturnsPairsInSampleSize()
    {
        // Arrange
        var before = new double[] { 1, 2, 3, 4, 5 };
        var after = new double[] { 2, 3, 4, 5, 6 };

        // Act
        var result = PairedTTest.Instance.Perform(before, after);

        // Assert
        Assert.Equal(5, result.SampleSizes["pairs"]);
    }

    [Fact]
    public void Perform_ThrowsForMismatchedLengths()
    {
        // Arrange
        var before = new double[] { 1, 2, 3 };
        var after = new double[] { 1, 2 }; // Different length

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            PairedTTest.Instance.Perform(before, after));
    }
}
