using ExperimentFramework.Science.Statistics;

namespace ExperimentFramework.Tests.Science;

public class OneWayAnovaTests
{
    [Fact]
    public void Perform_WithIdenticalGroups_ReturnsNotSignificant()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [5, 5, 5, 5, 5],
            ["group2"] = [5, 5, 5, 5, 5],
            ["group3"] = [5, 5, 5, 5, 5]
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups, 0.05);

        // Assert
        Assert.Equal("One-Way ANOVA", result.TestName);
        Assert.False(result.IsSignificant);
    }

    [Fact]
    public void Perform_WithSignificantDifference_ReturnsSignificant()
    {
        // Arrange - groups with clearly different means
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [1, 2, 3, 4, 5],
            ["group2"] = [10, 11, 12, 13, 14],
            ["group3"] = [20, 21, 22, 23, 24]
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups, 0.05);

        // Assert
        Assert.True(result.IsSignificant);
        Assert.True(result.PValue < 0.05);
    }

    [Fact]
    public void Perform_ReturnsFStatistic()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [1, 2, 3, 4, 5],
            ["group2"] = [6, 7, 8, 9, 10],
            ["group3"] = [11, 12, 13, 14, 15]
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups);

        // Assert
        Assert.True(result.TestStatistic >= 0); // F-statistic is always non-negative
    }

    [Fact]
    public void Perform_ReturnsDegreesOfFreedom()
    {
        // Arrange - 3 groups with 5 observations each
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [1, 2, 3, 4, 5],
            ["group2"] = [6, 7, 8, 9, 10],
            ["group3"] = [11, 12, 13, 14, 15]
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups);

        // Assert
        // Degrees of freedom between = k - 1 = 3 - 1 = 2
        // Degrees of freedom within = N - k = 15 - 3 = 12
        Assert.NotNull(result.DegreesOfFreedom);
    }

    [Fact]
    public void Perform_ReturnsSampleSizes()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["control"] = [1, 2, 3],
            ["variant-a"] = [4, 5, 6, 7],
            ["variant-b"] = [8, 9]
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups);

        // Assert
        Assert.Equal(3, result.SampleSizes["control"]);
        Assert.Equal(4, result.SampleSizes["variant-a"]);
        Assert.Equal(2, result.SampleSizes["variant-b"]);
    }

    [Fact]
    public void Perform_ThrowsForLessThanTwoGroups()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [1, 2, 3, 4, 5]
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => OneWayAnova.Instance.Perform(groups));
    }

    [Fact]
    public void Perform_RequiresMultipleObservationsForValidFDistribution()
    {
        // Arrange - with only 1 observation per group, dfWithin = 0 which is invalid for F-distribution
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [1],
            ["group2"] = [5]
        };

        // Act & Assert - F-distribution requires dfWithin > 0
        Assert.Throws<ArgumentException>(() => OneWayAnova.Instance.Perform(groups));
    }

    [Fact]
    public void Perform_WorksWithMinimumValidSampleSize()
    {
        // Arrange - minimum valid case: 2 groups with 2 observations each (dfWithin = 4 - 2 = 2)
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [1, 2],
            ["group2"] = [5, 6]
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PValue is >= 0 and <= 1);
    }

    [Fact]
    public void Perform_ThrowsForEmptyGroup()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [],
            ["group2"] = [2, 3, 4]
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => OneWayAnova.Instance.Perform(groups));
    }

    [Fact]
    public void Perform_ThrowsForNullGroups()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OneWayAnova.Instance.Perform(null!));
    }

    [Fact]
    public void Perform_ThrowsForEmptyGroups()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => OneWayAnova.Instance.Perform(groups));
    }

    [Fact]
    public void Perform_WithTwoGroups_ReturnsValidResult()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["control"] = [1, 2, 3, 4, 5],
            ["treatment"] = [6, 7, 8, 9, 10]
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.PValue is >= 0 and <= 1);
    }

    [Fact]
    public void Perform_WithManyGroups_ReturnsValidResult()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [1, 2, 3],
            ["group2"] = [4, 5, 6],
            ["group3"] = [7, 8, 9],
            ["group4"] = [10, 11, 12],
            ["group5"] = [13, 14, 15]
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSignificant);
    }

    [Fact]
    public void Perform_WithDefaultAlpha()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [1, 2, 3, 4, 5],
            ["group2"] = [6, 7, 8, 9, 10]
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups);

        // Assert
        Assert.Equal(0.05, result.Alpha);
    }

    [Fact]
    public void Perform_WithCustomAlpha()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [1, 2, 3, 4, 5],
            ["group2"] = [6, 7, 8, 9, 10]
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups, 0.01);

        // Assert
        Assert.Equal(0.01, result.Alpha);
    }

    [Fact]
    public void Perform_PointEstimateIsEtaSquared()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [2, 2, 2], // mean = 2
            ["group2"] = [4, 4, 4]  // mean = 4
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups);

        // Assert
        // PointEstimate is eta-squared (effect size), not grand mean
        // For this data: SSB = 3*(2-3)^2 + 3*(4-3)^2 = 3 + 3 = 6
        // SSW = 0 (no variance within groups)
        // SST = 6, eta-squared = SSB/SST = 6/6 = 1.0
        Assert.Equal(1.0, result.PointEstimate, precision: 10);
    }

    [Fact]
    public void Perform_GrandMeanAvailableInDetails()
    {
        // Arrange
        var groups = new Dictionary<string, IReadOnlyList<double>>
        {
            ["group1"] = [2, 2, 2], // mean = 2
            ["group2"] = [4, 4, 4]  // mean = 4
        };

        // Act
        var result = OneWayAnova.Instance.Perform(groups);

        // Assert
        // Grand mean should be available in Details
        Assert.NotNull(result.Details);
        Assert.True(result.Details.ContainsKey("grand_mean"));
        Assert.Equal(3.0, (double)result.Details["grand_mean"], precision: 10);
    }

    [Fact]
    public void Instance_ReturnsSingleton()
    {
        // Act
        var instance1 = OneWayAnova.Instance;
        var instance2 = OneWayAnova.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void Name_ReturnsOneWayAnova()
    {
        // Assert
        Assert.Equal("One-Way ANOVA", OneWayAnova.Instance.Name);
    }
}
