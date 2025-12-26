using ExperimentFramework.Data.Models;
using ExperimentFramework.Data.Storage;

namespace ExperimentFramework.Tests.Data;

public class NoopOutcomeStoreTests
{
    private readonly NoopOutcomeStore _store = NoopOutcomeStore.Instance;

    [Fact]
    public async Task RecordAsync_DoesNotThrow()
    {
        // Arrange
        var outcome = new ExperimentOutcome
        {
            Id = "test",
            ExperimentName = "exp",
            TrialKey = "trial",
            SubjectId = "user",
            MetricName = "metric",
            OutcomeType = OutcomeType.Binary,
            Value = 1.0,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act & Assert - should not throw
        await _store.RecordAsync(outcome);
    }

    [Fact]
    public async Task RecordBatchAsync_DoesNotThrow()
    {
        // Arrange
        var outcomes = new[]
        {
            new ExperimentOutcome
            {
                Id = "1",
                ExperimentName = "exp",
                TrialKey = "trial",
                SubjectId = "user",
                MetricName = "metric",
                OutcomeType = OutcomeType.Binary,
                Value = 1.0,
                Timestamp = DateTimeOffset.UtcNow
            }
        };

        // Act & Assert - should not throw
        await _store.RecordBatchAsync(outcomes);
    }

    [Fact]
    public async Task QueryAsync_ReturnsEmptyList()
    {
        // Act
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAggregationsAsync_ReturnsEmptyDictionary()
    {
        // Act
        var aggregations = await _store.GetAggregationsAsync("exp", "metric");

        // Assert
        Assert.Empty(aggregations);
    }

    [Fact]
    public async Task GetTrialKeysAsync_ReturnsEmptyList()
    {
        // Act
        var trialKeys = await _store.GetTrialKeysAsync("exp");

        // Assert
        Assert.Empty(trialKeys);
    }

    [Fact]
    public async Task GetMetricNamesAsync_ReturnsEmptyList()
    {
        // Act
        var metricNames = await _store.GetMetricNamesAsync("exp");

        // Assert
        Assert.Empty(metricNames);
    }

    [Fact]
    public async Task CountAsync_ReturnsZero()
    {
        // Act
        var count = await _store.CountAsync(new OutcomeQuery { ExperimentName = "exp" });

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsZero()
    {
        // Act
        var deleted = await _store.DeleteAsync(new OutcomeQuery { ExperimentName = "exp" });

        // Assert
        Assert.Equal(0, deleted);
    }

    [Fact]
    public void Instance_ReturnsSingleton()
    {
        // Act
        var instance1 = NoopOutcomeStore.Instance;
        var instance2 = NoopOutcomeStore.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }
}
