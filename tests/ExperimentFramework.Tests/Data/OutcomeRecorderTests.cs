using ExperimentFramework.Data.Models;
using ExperimentFramework.Data.Recording;
using ExperimentFramework.Data.Storage;

namespace ExperimentFramework.Tests.Data;

public class OutcomeRecorderTests
{
    private readonly InMemoryOutcomeStore _store = new();
    private readonly OutcomeRecorder _recorder;

    public OutcomeRecorderTests()
    {
        _recorder = new OutcomeRecorder(_store);
    }

    [Fact]
    public async Task RecordBinaryAsync_RecordsSuccessAsOne()
    {
        // Act
        await _recorder.RecordBinaryAsync("exp", "trial", "user-1", "conversion", success: true);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Single(results);
        Assert.Equal(OutcomeType.Binary, results[0].OutcomeType);
        Assert.Equal(1.0, results[0].Value);
    }

    [Fact]
    public async Task RecordBinaryAsync_RecordsFailureAsZero()
    {
        // Act
        await _recorder.RecordBinaryAsync("exp", "trial", "user-1", "conversion", success: false);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Single(results);
        Assert.Equal(0.0, results[0].Value);
    }

    [Fact]
    public async Task RecordContinuousAsync_RecordsValue()
    {
        // Act
        await _recorder.RecordContinuousAsync("exp", "trial", "user-1", "revenue", 149.99);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Single(results);
        Assert.Equal(OutcomeType.Continuous, results[0].OutcomeType);
        Assert.Equal(149.99, results[0].Value);
    }

    [Fact]
    public async Task RecordCountAsync_RecordsCount()
    {
        // Act
        await _recorder.RecordCountAsync("exp", "trial", "user-1", "items_added", 5);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Single(results);
        Assert.Equal(OutcomeType.Count, results[0].OutcomeType);
        Assert.Equal(5.0, results[0].Value);
    }

    [Fact]
    public async Task RecordDurationAsync_RecordsDurationInSeconds()
    {
        // Act
        await _recorder.RecordDurationAsync("exp", "trial", "user-1", "load_time", TimeSpan.FromMilliseconds(250));

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Single(results);
        Assert.Equal(OutcomeType.Duration, results[0].OutcomeType);
        Assert.Equal(0.25, results[0].Value);
    }

    [Fact]
    public async Task RecordAsync_RecordsDirectOutcome()
    {
        // Arrange
        var outcome = new ExperimentOutcome
        {
            Id = "custom-id",
            ExperimentName = "exp",
            TrialKey = "trial",
            SubjectId = "user-1",
            MetricName = "metric",
            OutcomeType = OutcomeType.Binary,
            Value = 1.0,
            Timestamp = DateTimeOffset.UtcNow
        };

        // Act
        await _recorder.RecordAsync(outcome);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Single(results);
        Assert.Equal("custom-id", results[0].Id);
    }

    [Fact]
    public async Task RecordBinaryAsync_IncludesMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object>
        {
            ["device"] = "mobile",
            ["region"] = "us-west"
        };

        // Act
        await _recorder.RecordBinaryAsync("exp", "trial", "user-1", "conversion", true, metadata);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Single(results);
        Assert.NotNull(results[0].Metadata);
        Assert.Equal("mobile", results[0].Metadata!["device"]);
        Assert.Equal("us-west", results[0].Metadata!["region"]);
    }

    [Fact]
    public async Task RecordContinuousAsync_IncludesMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["currency"] = "USD" };

        // Act
        await _recorder.RecordContinuousAsync("exp", "trial", "user-1", "revenue", 100.0, metadata);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Equal("USD", results[0].Metadata!["currency"]);
    }

    [Fact]
    public async Task RecordCountAsync_IncludesMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["category"] = "electronics" };

        // Act
        await _recorder.RecordCountAsync("exp", "trial", "user-1", "items", 3, metadata);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Equal("electronics", results[0].Metadata!["category"]);
    }

    [Fact]
    public async Task RecordDurationAsync_IncludesMetadata()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["endpoint"] = "/api/search" };

        // Act
        await _recorder.RecordDurationAsync("exp", "trial", "user-1", "response_time", TimeSpan.FromSeconds(1), metadata);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Equal("/api/search", results[0].Metadata!["endpoint"]);
    }

    [Fact]
    public void Constructor_ThrowsOnNullStore()
    {
        Assert.Throws<ArgumentNullException>(() => new OutcomeRecorder(null!));
    }

    [Fact]
    public async Task Recorder_WithAutoGenerateIdsFalse_UsesEmptyId()
    {
        // Arrange
        var options = new OutcomeRecorderOptions { AutoGenerateIds = false };
        var recorder = new OutcomeRecorder(_store, options);

        // Act
        await recorder.RecordBinaryAsync("exp", "trial", "user-1", "metric", true);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Single(results);
        Assert.Equal(string.Empty, results[0].Id);
    }

    [Fact]
    public async Task Recorder_WithAutoSetTimestampsFalse_UsesDefaultTimestamp()
    {
        // Arrange
        var options = new OutcomeRecorderOptions { AutoSetTimestamps = false };
        var recorder = new OutcomeRecorder(_store, options);

        // Act
        await recorder.RecordBinaryAsync("exp", "trial", "user-1", "metric", true);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Single(results);
        Assert.Equal(default, results[0].Timestamp);
    }

    [Fact]
    public async Task Recorder_GeneratesUniqueIds()
    {
        // Act
        await _recorder.RecordBinaryAsync("exp", "trial", "user-1", "metric", true);
        await _recorder.RecordBinaryAsync("exp", "trial", "user-2", "metric", true);

        // Assert
        var results = await _store.QueryAsync(new OutcomeQuery { ExperimentName = "exp" });
        Assert.Equal(2, results.Count);
        Assert.NotEqual(results[0].Id, results[1].Id);
    }
}
