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
}
