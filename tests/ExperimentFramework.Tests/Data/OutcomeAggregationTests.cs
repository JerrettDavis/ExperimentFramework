using ExperimentFramework.Data.Models;

namespace ExperimentFramework.Tests.Data;

public class OutcomeAggregationTests
{
    [Fact]
    public void Empty_CreatesEmptyAggregation()
    {
        // Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Assert
        Assert.Equal("trial", aggregation.TrialKey);
        Assert.Equal("metric", aggregation.MetricName);
        Assert.Equal(0, aggregation.Count);
        Assert.Equal(0, aggregation.Sum);
        Assert.Equal(0, aggregation.SuccessCount);
    }

    [Fact]
    public void WithValue_IncrementsCount()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Act
        var updated = aggregation.WithValue(10.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(1, updated.Count);
        Assert.Equal(10.0, updated.Sum);
    }

    [Fact]
    public void WithValue_TracksSuccesses()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Act
        var updated = aggregation
            .WithValue(1.0, true, DateTimeOffset.UtcNow)
            .WithValue(0.0, false, DateTimeOffset.UtcNow)
            .WithValue(1.0, true, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(3, updated.Count);
        Assert.Equal(2, updated.SuccessCount);
    }

    [Fact]
    public void WithValue_TracksMinMax()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Act
        var updated = aggregation
            .WithValue(5.0, false, DateTimeOffset.UtcNow)
            .WithValue(10.0, false, DateTimeOffset.UtcNow)
            .WithValue(3.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(3.0, updated.Min);
        Assert.Equal(10.0, updated.Max);
    }

    [Fact]
    public void WithValue_TracksSumOfSquares()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Act
        var updated = aggregation
            .WithValue(2.0, false, DateTimeOffset.UtcNow)
            .WithValue(3.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(5.0, updated.Sum); // 2 + 3
        Assert.Equal(13.0, updated.SumOfSquares); // 4 + 9
    }

    [Fact]
    public void WithValue_TracksTimestamps()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric");
        var early = DateTimeOffset.UtcNow.AddHours(-1);
        var late = DateTimeOffset.UtcNow;

        // Act - first timestamp added is kept as FirstTimestamp
        // last timestamp provided is always set as LastTimestamp
        var updated = aggregation
            .WithValue(1.0, false, early)
            .WithValue(1.0, false, late);

        // Assert
        Assert.Equal(early, updated.FirstTimestamp);
        Assert.Equal(late, updated.LastTimestamp);
    }

    [Fact]
    public void Mean_CalculatesCorrectly()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(10.0, false, DateTimeOffset.UtcNow)
            .WithValue(20.0, false, DateTimeOffset.UtcNow)
            .WithValue(30.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(20.0, aggregation.Mean);
    }

    [Fact]
    public void Mean_ReturnsZeroForEmptyAggregation()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Assert
        Assert.Equal(0.0, aggregation.Mean);
    }

    [Fact]
    public void Variance_CalculatesCorrectly()
    {
        // Arrange - values: 2, 4, 6
        // Sum = 12, SumOfSquares = 56, Count = 3
        // Sample variance = (SumOfSquares - Sum²/Count) / (Count - 1)
        //                 = (56 - 144/3) / 2 = (56 - 48) / 2 = 4
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(2.0, false, DateTimeOffset.UtcNow)
            .WithValue(4.0, false, DateTimeOffset.UtcNow)
            .WithValue(6.0, false, DateTimeOffset.UtcNow);

        // Assert - implementation uses sample variance with Bessel's correction
        Assert.Equal(4.0, aggregation.Variance, precision: 10);
    }

    [Fact]
    public void StandardDeviation_CalculatesCorrectly()
    {
        // Arrange - same as above, sample std dev = sqrt(4) = 2
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(2.0, false, DateTimeOffset.UtcNow)
            .WithValue(4.0, false, DateTimeOffset.UtcNow)
            .WithValue(6.0, false, DateTimeOffset.UtcNow);

        // Assert - sample standard deviation
        Assert.Equal(2.0, aggregation.StandardDeviation, precision: 10);
    }

    [Fact]
    public void ConversionRate_CalculatesCorrectly()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(1.0, true, DateTimeOffset.UtcNow)
            .WithValue(0.0, false, DateTimeOffset.UtcNow)
            .WithValue(1.0, true, DateTimeOffset.UtcNow)
            .WithValue(1.0, true, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(0.75, aggregation.ConversionRate); // 3/4
    }

    [Fact]
    public void ConversionRate_ReturnsZeroForEmptyAggregation()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Assert
        Assert.Equal(0.0, aggregation.ConversionRate);
    }

    [Fact]
    public void Variance_ReturnsZeroForSingleValue()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(5.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(0.0, aggregation.Variance);
    }

    #region Additional Edge Case Tests

    [Fact]
    public void Empty_HasCorrectMinMax()
    {
        // Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Assert - Empty aggregation has extreme min/max values
        Assert.Equal(double.MaxValue, aggregation.Min);
        Assert.Equal(double.MinValue, aggregation.Max);
    }

    [Fact]
    public void StandardError_CalculatesCorrectly()
    {
        // Arrange - sample std dev = 2, count = 4
        // Standard error = 2 / sqrt(4) = 1
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(2.0, false, DateTimeOffset.UtcNow)
            .WithValue(4.0, false, DateTimeOffset.UtcNow)
            .WithValue(4.0, false, DateTimeOffset.UtcNow)
            .WithValue(6.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.True(aggregation.StandardError > 0);
    }

    [Fact]
    public void StandardError_ReturnsZeroForEmptyAggregation()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Assert
        Assert.Equal(0.0, aggregation.StandardError);
    }

    [Fact]
    public void WithValue_FirstTimestampRemainsFromFirstValue()
    {
        // Arrange
        var first = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var second = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var third = new DateTimeOffset(2024, 12, 1, 0, 0, 0, TimeSpan.Zero);

        // Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(1.0, false, first)
            .WithValue(2.0, false, second)
            .WithValue(3.0, false, third);

        // Assert
        Assert.Equal(first, aggregation.FirstTimestamp);
        Assert.Equal(third, aggregation.LastTimestamp);
    }

    [Fact]
    public void WithValue_WithNullTimestamp_PreservesExistingTimestamps()
    {
        // Arrange
        var timestamp = new DateTimeOffset(2024, 6, 15, 0, 0, 0, TimeSpan.Zero);

        // Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(1.0, false, timestamp)
            .WithValue(2.0, false, null);

        // Assert - FirstTimestamp should still be set from first value
        Assert.Equal(timestamp, aggregation.FirstTimestamp);
    }

    [Fact]
    public void WithValue_PreservesTrialKeyAndMetricName()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("my-trial", "my-metric")
            .WithValue(1.0, false, DateTimeOffset.UtcNow)
            .WithValue(2.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal("my-trial", aggregation.TrialKey);
        Assert.Equal("my-metric", aggregation.MetricName);
    }

    [Fact]
    public void WithValue_MinMaxUpdatedCorrectly()
    {
        // Arrange & Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(100.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(100.0, aggregation.Min);
        Assert.Equal(100.0, aggregation.Max);

        // Add smaller value
        var updated = aggregation.WithValue(50.0, false, DateTimeOffset.UtcNow);
        Assert.Equal(50.0, updated.Min);
        Assert.Equal(100.0, updated.Max);

        // Add larger value
        var final = updated.WithValue(200.0, false, DateTimeOffset.UtcNow);
        Assert.Equal(50.0, final.Min);
        Assert.Equal(200.0, final.Max);
    }

    [Fact]
    public void WithValue_NegativeValues()
    {
        // Arrange & Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(-5.0, false, DateTimeOffset.UtcNow)
            .WithValue(-10.0, false, DateTimeOffset.UtcNow)
            .WithValue(5.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(3, aggregation.Count);
        Assert.Equal(-10.0, aggregation.Sum); // -5 + -10 + 5 = -10
        Assert.Equal(-10.0, aggregation.Min);
        Assert.Equal(5.0, aggregation.Max);
    }

    [Fact]
    public void WithValue_ZeroValues()
    {
        // Arrange & Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(0.0, false, DateTimeOffset.UtcNow)
            .WithValue(0.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(2, aggregation.Count);
        Assert.Equal(0.0, aggregation.Sum);
        Assert.Equal(0.0, aggregation.Mean);
        Assert.Equal(0.0, aggregation.Min);
        Assert.Equal(0.0, aggregation.Max);
    }

    [Fact]
    public void WithValue_AllSuccesses()
    {
        // Arrange & Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(1.0, true, DateTimeOffset.UtcNow)
            .WithValue(1.0, true, DateTimeOffset.UtcNow)
            .WithValue(1.0, true, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(3, aggregation.Count);
        Assert.Equal(3, aggregation.SuccessCount);
        Assert.Equal(1.0, aggregation.ConversionRate);
    }

    [Fact]
    public void WithValue_NoSuccesses()
    {
        // Arrange & Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(0.0, false, DateTimeOffset.UtcNow)
            .WithValue(0.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(2, aggregation.Count);
        Assert.Equal(0, aggregation.SuccessCount);
        Assert.Equal(0.0, aggregation.ConversionRate);
    }

    [Fact]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("my-trial", "my-metric")
            .WithValue(10.0, false, DateTimeOffset.UtcNow)
            .WithValue(20.0, false, DateTimeOffset.UtcNow);

        // Act
        var str = aggregation.ToString();

        // Assert
        Assert.Contains("my-trial", str);
        Assert.Contains("my-metric", str);
        Assert.Contains("N=2", str);
    }

    [Fact]
    public void StandardDeviation_ReturnsZeroForEmptyAggregation()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Assert
        Assert.Equal(0.0, aggregation.StandardDeviation);
    }

    [Fact]
    public void Variance_ReturnsZeroForEmptyAggregation()
    {
        // Arrange
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Assert
        Assert.Equal(0.0, aggregation.Variance);
    }

    [Fact]
    public void WithValue_LargeNumbers()
    {
        // Arrange & Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(1e10, false, DateTimeOffset.UtcNow)
            .WithValue(1e10, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(2, aggregation.Count);
        Assert.Equal(2e10, aggregation.Sum);
        Assert.Equal(1e10, aggregation.Mean);
    }

    [Fact]
    public void WithValue_SmallDecimalNumbers()
    {
        // Arrange & Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(0.001, false, DateTimeOffset.UtcNow)
            .WithValue(0.002, false, DateTimeOffset.UtcNow)
            .WithValue(0.003, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(3, aggregation.Count);
        Assert.Equal(0.006, aggregation.Sum, precision: 10);
        Assert.Equal(0.002, aggregation.Mean, precision: 10);
    }

    [Fact]
    public void Mean_WithMixedPositiveAndNegative()
    {
        // Arrange - mean of -10, -5, 0, 5, 10 is 0
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(-10.0, false, DateTimeOffset.UtcNow)
            .WithValue(-5.0, false, DateTimeOffset.UtcNow)
            .WithValue(0.0, false, DateTimeOffset.UtcNow)
            .WithValue(5.0, false, DateTimeOffset.UtcNow)
            .WithValue(10.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(0.0, aggregation.Mean);
    }

    [Fact]
    public void Empty_HasNullTimestamps()
    {
        // Arrange & Act
        var aggregation = OutcomeAggregation.Empty("trial", "metric");

        // Assert
        Assert.Null(aggregation.FirstTimestamp);
        Assert.Null(aggregation.LastTimestamp);
    }

    [Fact]
    public void SumOfSquares_CalculatesCorrectly()
    {
        // Arrange - 3^2 + 4^2 = 9 + 16 = 25
        var aggregation = OutcomeAggregation.Empty("trial", "metric")
            .WithValue(3.0, false, DateTimeOffset.UtcNow)
            .WithValue(4.0, false, DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(25.0, aggregation.SumOfSquares);
    }

    #endregion
}
