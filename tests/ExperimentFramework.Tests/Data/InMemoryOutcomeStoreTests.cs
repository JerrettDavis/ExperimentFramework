using ExperimentFramework.Data.Models;
using ExperimentFramework.Data.Storage;

namespace ExperimentFramework.Tests.Data;

public class InMemoryOutcomeStoreTests
{
    private readonly InMemoryOutcomeStore _store = new();

    private static ExperimentOutcome CreateOutcome(
        string experimentName = "test-exp",
        string trialKey = "control",
        string subjectId = "user-1",
        string metricName = "conversion",
        OutcomeType outcomeType = OutcomeType.Binary,
        double value = 1.0,
        DateTimeOffset? timestamp = null)
    {
        return new ExperimentOutcome
        {
            Id = Guid.NewGuid().ToString("N"),
            ExperimentName = experimentName,
            TrialKey = trialKey,
            SubjectId = subjectId,
            MetricName = metricName,
            OutcomeType = outcomeType,
            Value = value,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public async Task RecordAsync_StoresOutcome()
    {
        // Arrange
        var outcome = CreateOutcome();

        // Act
        await _store.RecordAsync(outcome);
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "test-exp" });

        // Assert
        Assert.Single(results);
        Assert.Equal(outcome.Id, results[0].Id);
    }

    [Fact]
    public async Task RecordBatchAsync_StoresMultipleOutcomes()
    {
        // Arrange
        var outcomes = new[]
        {
            CreateOutcome(subjectId: "user-1"),
            CreateOutcome(subjectId: "user-2"),
            CreateOutcome(subjectId: "user-3")
        };

        // Act
        await _store.RecordBatchAsync(outcomes);
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "test-exp" });

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task QueryAsync_FiltersByExperimentName()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(experimentName: "exp-1"));
        await _store.RecordAsync(CreateOutcome(experimentName: "exp-2"));
        await _store.RecordAsync(CreateOutcome(experimentName: "exp-1"));

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp-1" });

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.Equal("exp-1", r.ExperimentName));
    }

    [Fact]
    public async Task QueryAsync_FiltersByTrialKey()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(trialKey: "control"));
        await _store.RecordAsync(CreateOutcome(trialKey: "treatment"));
        await _store.RecordAsync(CreateOutcome(trialKey: "control"));

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            TrialKey = "treatment"
        });

        // Assert
        Assert.Single(results);
        Assert.Equal("treatment", results[0].TrialKey);
    }

    [Fact]
    public async Task QueryAsync_FiltersByMetricName()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(metricName: "conversion"));
        await _store.RecordAsync(CreateOutcome(metricName: "revenue"));

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            MetricName = "revenue"
        });

        // Assert
        Assert.Single(results);
        Assert.Equal("revenue", results[0].MetricName);
    }

    [Fact]
    public async Task QueryAsync_FiltersBySubjectId()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(subjectId: "user-1"));
        await _store.RecordAsync(CreateOutcome(subjectId: "user-2"));

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            SubjectId = "user-1"
        });

        // Assert
        Assert.Single(results);
        Assert.Equal("user-1", results[0].SubjectId);
    }

    [Fact]
    public async Task QueryAsync_FiltersByOutcomeType()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(outcomeType: OutcomeType.Binary));
        await _store.RecordAsync(CreateOutcome(outcomeType: OutcomeType.Continuous));

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            OutcomeType = OutcomeType.Continuous
        });

        // Assert
        Assert.Single(results);
        Assert.Equal(OutcomeType.Continuous, results[0].OutcomeType);
    }

    [Fact]
    public async Task QueryAsync_FiltersByTimestampRange()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.RecordAsync(CreateOutcome(timestamp: now.AddDays(-2)));
        await _store.RecordAsync(CreateOutcome(timestamp: now.AddDays(-1)));
        await _store.RecordAsync(CreateOutcome(timestamp: now));

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            FromTimestamp = now.AddDays(-1.5),
            ToTimestamp = now.AddMinutes(-1)
        });

        // Assert
        Assert.Single(results);
    }

    [Fact]
    public async Task QueryAsync_OrdersByTimestamp()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.RecordAsync(CreateOutcome(subjectId: "user-2", timestamp: now.AddHours(-1)));
        await _store.RecordAsync(CreateOutcome(subjectId: "user-1", timestamp: now.AddHours(-2)));
        await _store.RecordAsync(CreateOutcome(subjectId: "user-3", timestamp: now));

        // Act - ascending
        var ascending = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            OrderByTimestampDescending = false
        });

        // Assert
        Assert.Equal("user-1", ascending[0].SubjectId);
        Assert.Equal("user-2", ascending[1].SubjectId);
        Assert.Equal("user-3", ascending[2].SubjectId);

        // Act - descending
        var descending = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            OrderByTimestampDescending = true
        });

        // Assert
        Assert.Equal("user-3", descending[0].SubjectId);
        Assert.Equal("user-2", descending[1].SubjectId);
        Assert.Equal("user-1", descending[2].SubjectId);
    }

    [Fact]
    public async Task QueryAsync_AppliesPagination()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await _store.RecordAsync(CreateOutcome(subjectId: $"user-{i}"));
        }

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            Offset = 3,
            Limit = 4
        });

        // Assert
        Assert.Equal(4, results.Count);
    }

    [Fact]
    public async Task GetAggregationsAsync_ReturnsAggregatedData()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(trialKey: "control", value: 1.0));
        await _store.RecordAsync(CreateOutcome(trialKey: "control", value: 0.0));
        await _store.RecordAsync(CreateOutcome(trialKey: "treatment", value: 1.0));
        await _store.RecordAsync(CreateOutcome(trialKey: "treatment", value: 1.0));
        await _store.RecordAsync(CreateOutcome(trialKey: "treatment", value: 1.0));

        // Act
        var aggregations = await _store.GetAggregationsAsync("test-exp", "conversion");

        // Assert
        Assert.Equal(2, aggregations.Count);
        Assert.True(aggregations.ContainsKey("control"));
        Assert.True(aggregations.ContainsKey("treatment"));
        Assert.Equal(2, aggregations["control"].Count);
        Assert.Equal(3, aggregations["treatment"].Count);
    }

    [Fact]
    public async Task GetTrialKeysAsync_ReturnsDistinctTrialKeys()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(trialKey: "control"));
        await _store.RecordAsync(CreateOutcome(trialKey: "treatment-a"));
        await _store.RecordAsync(CreateOutcome(trialKey: "control"));
        await _store.RecordAsync(CreateOutcome(trialKey: "treatment-b"));

        // Act
        var trialKeys = await _store.GetTrialKeysAsync("test-exp");

        // Assert
        Assert.Equal(3, trialKeys.Count);
        Assert.Contains("control", trialKeys);
        Assert.Contains("treatment-a", trialKeys);
        Assert.Contains("treatment-b", trialKeys);
    }

    [Fact]
    public async Task GetMetricNamesAsync_ReturnsDistinctMetricNames()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(metricName: "conversion"));
        await _store.RecordAsync(CreateOutcome(metricName: "revenue"));
        await _store.RecordAsync(CreateOutcome(metricName: "conversion"));

        // Act
        var metricNames = await _store.GetMetricNamesAsync("test-exp");

        // Assert
        Assert.Equal(2, metricNames.Count);
        Assert.Contains("conversion", metricNames);
        Assert.Contains("revenue", metricNames);
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(trialKey: "control"));
        await _store.RecordAsync(CreateOutcome(trialKey: "control"));
        await _store.RecordAsync(CreateOutcome(trialKey: "treatment"));

        // Act
        var totalCount = await _store.CountAsync(new OutcomeQuery { ExperimentName = "test-exp" });
        var controlCount = await _store.CountAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            TrialKey = "control"
        });

        // Assert
        Assert.Equal(3, totalCount);
        Assert.Equal(2, controlCount);
    }

    [Fact]
    public async Task DeleteAsync_RemovesMatchingOutcomes()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(trialKey: "control"));
        await _store.RecordAsync(CreateOutcome(trialKey: "control"));
        await _store.RecordAsync(CreateOutcome(trialKey: "treatment"));

        // Act
        var deleted = await _store.DeleteAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            TrialKey = "control"
        });

        var remaining = await _store.CountAsync(new OutcomeQuery { ExperimentName = "test-exp" });

        // Assert
        Assert.Equal(2, deleted);
        Assert.Equal(1, remaining);
    }

    [Fact]
    public async Task Clear_RemovesAllData()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome());
        await _store.RecordAsync(CreateOutcome());

        // Act
        _store.Clear();
        var count = await _store.CountAsync(new OutcomeQuery());

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task RecordAsync_ThrowsOnCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _store.RecordAsync(CreateOutcome(), cts.Token).AsTask());
    }

    [Fact]
    public async Task GetAggregationsAsync_ReturnsEmptyForNonexistentExperiment()
    {
        // Act
        var aggregations = await _store.GetAggregationsAsync("nonexistent", "metric");

        // Assert
        Assert.Empty(aggregations);
    }

    #region Additional Edge Case Tests

    [Fact]
    public async Task RecordBatchAsync_WithEmptyEnumerable_DoesNotThrow()
    {
        // Act & Assert - should not throw
        await _store.RecordBatchAsync(Array.Empty<ExperimentOutcome>());

        var count = await _store.CountAsync(new OutcomeQuery());
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task RecordBatchAsync_ThrowsOnCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var outcomes = new[] { CreateOutcome() };

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _store.RecordBatchAsync(outcomes, cts.Token).AsTask());
    }

    [Fact]
    public async Task QueryAsync_ThrowsOnCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _store.QueryAsync(new OutcomeQuery(), cts.Token).AsTask());
    }

    [Fact]
    public async Task CountAsync_ThrowsOnCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _store.CountAsync(new OutcomeQuery(), cts.Token).AsTask());
    }

    [Fact]
    public async Task DeleteAsync_ThrowsOnCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _store.DeleteAsync(new OutcomeQuery(), cts.Token).AsTask());
    }

    [Fact]
    public async Task GetTrialKeysAsync_ThrowsOnCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _store.GetTrialKeysAsync("exp", cts.Token).AsTask());
    }

    [Fact]
    public async Task GetMetricNamesAsync_ThrowsOnCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _store.GetMetricNamesAsync("exp", cts.Token).AsTask());
    }

    [Fact]
    public async Task GetAggregationsAsync_ThrowsOnCancellation()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _store.GetAggregationsAsync("exp", "metric", cts.Token).AsTask());
    }

    [Fact]
    public async Task QueryAsync_WithEmptyStore_ReturnsEmptyList()
    {
        // Act
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "any" });

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task QueryAsync_WithOnlyLimit_ReturnsLimitedResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await _store.RecordAsync(CreateOutcome(subjectId: $"user-{i}"));
        }

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            Limit = 5
        });

        // Assert
        Assert.Equal(5, results.Count);
    }

    [Fact]
    public async Task QueryAsync_WithOnlyOffset_SkipsResults()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await _store.RecordAsync(CreateOutcome(subjectId: $"user-{i}"));
        }

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            Offset = 7
        });

        // Assert
        Assert.Equal(3, results.Count);
    }

    [Fact]
    public async Task QueryAsync_WithOffsetBeyondCount_ReturnsEmpty()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome());
        await _store.RecordAsync(CreateOutcome());

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            Offset = 100
        });

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task QueryAsync_WithMultipleFilters_CombinesWithAnd()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(trialKey: "control", metricName: "conversion"));
        await _store.RecordAsync(CreateOutcome(trialKey: "control", metricName: "revenue"));
        await _store.RecordAsync(CreateOutcome(trialKey: "treatment", metricName: "conversion"));
        await _store.RecordAsync(CreateOutcome(trialKey: "treatment", metricName: "revenue"));

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            TrialKey = "control",
            MetricName = "conversion"
        });

        // Assert
        Assert.Single(results);
        Assert.Equal("control", results[0].TrialKey);
        Assert.Equal("conversion", results[0].MetricName);
    }

    [Fact]
    public async Task DeleteAsync_WithNoMatches_ReturnsZero()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(experimentName: "exp-1"));

        // Act
        var deleted = await _store.DeleteAsync(new OutcomeQuery { ExperimentName = "nonexistent" });

        // Assert
        Assert.Equal(0, deleted);
    }

    [Fact]
    public async Task DeleteAsync_WithAllMatching_DeletesAll()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome());
        await _store.RecordAsync(CreateOutcome());
        await _store.RecordAsync(CreateOutcome());

        // Act
        var deleted = await _store.DeleteAsync(new OutcomeQuery { ExperimentName = "test-exp" });

        // Assert
        Assert.Equal(3, deleted);
        var remaining = await _store.CountAsync(new OutcomeQuery());
        Assert.Equal(0, remaining);
    }

    [Fact]
    public async Task GetTrialKeysAsync_ReturnsEmptyForNonexistentExperiment()
    {
        // Act
        var trialKeys = await _store.GetTrialKeysAsync("nonexistent");

        // Assert
        Assert.Empty(trialKeys);
    }

    [Fact]
    public async Task GetMetricNamesAsync_ReturnsEmptyForNonexistentExperiment()
    {
        // Act
        var metricNames = await _store.GetMetricNamesAsync("nonexistent");

        // Assert
        Assert.Empty(metricNames);
    }

    [Fact]
    public async Task GetTrialKeysAsync_ReturnsSortedKeys()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(trialKey: "z-trial"));
        await _store.RecordAsync(CreateOutcome(trialKey: "a-trial"));
        await _store.RecordAsync(CreateOutcome(trialKey: "m-trial"));

        // Act
        var trialKeys = await _store.GetTrialKeysAsync("test-exp");

        // Assert
        Assert.Equal(3, trialKeys.Count);
        Assert.Equal("a-trial", trialKeys[0]);
        Assert.Equal("m-trial", trialKeys[1]);
        Assert.Equal("z-trial", trialKeys[2]);
    }

    [Fact]
    public async Task GetMetricNamesAsync_ReturnsSortedNames()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome(metricName: "z-metric"));
        await _store.RecordAsync(CreateOutcome(metricName: "a-metric"));
        await _store.RecordAsync(CreateOutcome(metricName: "m-metric"));

        // Act
        var metricNames = await _store.GetMetricNamesAsync("test-exp");

        // Assert
        Assert.Equal(3, metricNames.Count);
        Assert.Equal("a-metric", metricNames[0]);
        Assert.Equal("m-metric", metricNames[1]);
        Assert.Equal("z-metric", metricNames[2]);
    }

    [Fact]
    public async Task GetAggregationsAsync_TracksSuccessForBinaryOutcomes()
    {
        // Arrange - Binary outcomes with value >= 0.5 are considered success
        await _store.RecordAsync(CreateOutcome(trialKey: "control", value: 1.0, outcomeType: OutcomeType.Binary));
        await _store.RecordAsync(CreateOutcome(trialKey: "control", value: 0.5, outcomeType: OutcomeType.Binary));
        await _store.RecordAsync(CreateOutcome(trialKey: "control", value: 0.0, outcomeType: OutcomeType.Binary));
        await _store.RecordAsync(CreateOutcome(trialKey: "control", value: 0.49, outcomeType: OutcomeType.Binary));

        // Act
        var aggregations = await _store.GetAggregationsAsync("test-exp", "conversion");

        // Assert
        Assert.Single(aggregations);
        Assert.Equal(4, aggregations["control"].Count);
        Assert.Equal(2, aggregations["control"].SuccessCount); // 1.0 and 0.5 are successes
    }

    [Fact]
    public async Task CountAsync_WithAllFilters()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.RecordAsync(CreateOutcome(trialKey: "control", metricName: "conv", timestamp: now.AddDays(-1)));
        await _store.RecordAsync(CreateOutcome(trialKey: "control", metricName: "conv", timestamp: now.AddDays(-2)));
        await _store.RecordAsync(CreateOutcome(trialKey: "treatment", metricName: "conv", timestamp: now.AddDays(-1)));

        // Act
        var count = await _store.CountAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            TrialKey = "control",
            MetricName = "conv",
            FromTimestamp = now.AddDays(-1.5),
            ToTimestamp = now
        });

        // Assert
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task RecordAsync_DuplicateId_DoesNotOverwrite()
    {
        // Arrange
        var id = Guid.NewGuid().ToString("N");
        var outcome1 = new ExperimentOutcome
        {
            Id = id,
            ExperimentName = "exp",
            TrialKey = "trial",
            SubjectId = "user-1",
            MetricName = "metric",
            OutcomeType = OutcomeType.Binary,
            Value = 1.0,
            Timestamp = DateTimeOffset.UtcNow
        };
        var outcome2 = new ExperimentOutcome
        {
            Id = id, // Same ID
            ExperimentName = "exp",
            TrialKey = "trial",
            SubjectId = "user-2",
            MetricName = "metric",
            OutcomeType = OutcomeType.Binary,
            Value = 0.0,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _store.RecordAsync(outcome1);
        await _store.RecordAsync(outcome2); // Should not overwrite

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Single(results);
        Assert.Equal("user-1", results[0].SubjectId); // First one should be retained
        Assert.Equal(1.0, results[0].Value);
    }

    [Fact]
    public async Task Clear_AlsoClearsAggregations()
    {
        // Arrange
        await _store.RecordAsync(CreateOutcome());
        var aggregationsBefore = await _store.GetAggregationsAsync("test-exp", "conversion");
        Assert.NotEmpty(aggregationsBefore);

        // Act
        _store.Clear();

        // Assert
        var aggregationsAfter = await _store.GetAggregationsAsync("test-exp", "conversion");
        Assert.Empty(aggregationsAfter);
    }

    [Fact]
    public async Task QueryAsync_FiltersByFromTimestampInclusive()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.RecordAsync(CreateOutcome(subjectId: "user-1", timestamp: now));
        await _store.RecordAsync(CreateOutcome(subjectId: "user-2", timestamp: now.AddSeconds(1)));

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            FromTimestamp = now
        });

        // Assert
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public async Task QueryAsync_FiltersByToTimestampExclusive()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.RecordAsync(CreateOutcome(subjectId: "user-1", timestamp: now.AddSeconds(-1)));
        await _store.RecordAsync(CreateOutcome(subjectId: "user-2", timestamp: now));

        // Act
        var results = await _store.QueryAsync(new OutcomeQuery
        {
            ExperimentName = "test-exp",
            ToTimestamp = now
        });

        // Assert
        Assert.Single(results);
        Assert.Equal("user-1", results[0].SubjectId);
    }

    #endregion
}
