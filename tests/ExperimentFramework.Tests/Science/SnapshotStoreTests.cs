using ExperimentFramework.Science.Models.Snapshots;
using ExperimentFramework.Science.Snapshots;

namespace ExperimentFramework.Tests.Science;

public class SnapshotStoreTests
{
    private readonly InMemorySnapshotStore _store = new();

    private static ExperimentSnapshot CreateSnapshot(
        string experimentName = "test-exp",
        SnapshotType type = SnapshotType.PreRegistration,
        DateTimeOffset? timestamp = null)
    {
        return new ExperimentSnapshot
        {
            Id = Guid.NewGuid().ToString("N"),
            ExperimentName = experimentName,
            Timestamp = timestamp ?? DateTimeOffset.UtcNow,
            Type = type,
            Description = "Test snapshot"
        };
    }

    [Fact]
    public async Task SaveAsync_StoresSnapshot()
    {
        // Arrange
        var snapshot = CreateSnapshot();

        // Act
        await _store.SaveAsync(snapshot);
        var retrieved = await _store.GetAsync(snapshot.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(snapshot.Id, retrieved.Id);
        Assert.Equal(snapshot.ExperimentName, retrieved.ExperimentName);
    }

    [Fact]
    public async Task GetAsync_ReturnsNullForNonexistent()
    {
        // Act
        var result = await _store.GetAsync("nonexistent-id");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ListAsync_ReturnsAllSnapshotsForExperiment()
    {
        // Arrange
        await _store.SaveAsync(CreateSnapshot("exp-1"));
        await _store.SaveAsync(CreateSnapshot("exp-1"));
        await _store.SaveAsync(CreateSnapshot("exp-2"));

        // Act
        var snapshots = await _store.ListAsync("exp-1");

        // Assert
        Assert.Equal(2, snapshots.Count);
        Assert.All(snapshots, s => Assert.Equal("exp-1", s.ExperimentName));
    }

    [Fact]
    public async Task ListAsync_ReturnsOrderedByTimestamp()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.SaveAsync(CreateSnapshot(timestamp: now.AddHours(-2)));
        await _store.SaveAsync(CreateSnapshot(timestamp: now));
        await _store.SaveAsync(CreateSnapshot(timestamp: now.AddHours(-1)));

        // Act
        var snapshots = await _store.ListAsync("test-exp");

        // Assert
        Assert.Equal(3, snapshots.Count);
        Assert.True(snapshots[0].Timestamp <= snapshots[1].Timestamp);
        Assert.True(snapshots[1].Timestamp <= snapshots[2].Timestamp);
    }

    [Fact]
    public async Task ListAsync_ReturnsEmptyForNonexistentExperiment()
    {
        // Act
        var snapshots = await _store.ListAsync("nonexistent");

        // Assert
        Assert.Empty(snapshots);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsLatestOfType()
    {
        // Arrange
        var now = DateTimeOffset.UtcNow;
        await _store.SaveAsync(CreateSnapshot(type: SnapshotType.PreRegistration, timestamp: now.AddHours(-2)));
        await _store.SaveAsync(CreateSnapshot(type: SnapshotType.InterimAnalysis, timestamp: now.AddHours(-1)));
        await _store.SaveAsync(CreateSnapshot(type: SnapshotType.PreRegistration, timestamp: now));

        // Act
        var latest = await _store.GetLatestAsync("test-exp", SnapshotType.PreRegistration);

        // Assert
        Assert.NotNull(latest);
        Assert.Equal(SnapshotType.PreRegistration, latest.Type);
        Assert.Equal(now, latest.Timestamp);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsNullIfNoMatchingType()
    {
        // Arrange
        await _store.SaveAsync(CreateSnapshot(type: SnapshotType.PreRegistration));

        // Act
        var latest = await _store.GetLatestAsync("test-exp", SnapshotType.FinalAnalysis);

        // Assert
        Assert.Null(latest);
    }

    [Fact]
    public async Task DeleteAsync_RemovesSnapshot()
    {
        // Arrange
        var snapshot = CreateSnapshot();
        await _store.SaveAsync(snapshot);

        // Act
        var deleted = await _store.DeleteAsync(snapshot.Id);
        var retrieved = await _store.GetAsync(snapshot.Id);

        // Assert
        Assert.True(deleted);
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalseForNonexistent()
    {
        // Act
        var deleted = await _store.DeleteAsync("nonexistent-id");

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAllAsync_RemovesAllForExperiment()
    {
        // Arrange
        await _store.SaveAsync(CreateSnapshot("exp-1"));
        await _store.SaveAsync(CreateSnapshot("exp-1"));
        await _store.SaveAsync(CreateSnapshot("exp-2"));

        // Act
        var deleted = await _store.DeleteAllAsync("exp-1");
        var remaining = await _store.ListAsync("exp-1");
        var otherRemaining = await _store.ListAsync("exp-2");

        // Assert
        Assert.Equal(2, deleted);
        Assert.Empty(remaining);
        Assert.Single(otherRemaining);
    }

    [Fact]
    public async Task DeleteAllAsync_ReturnsZeroForNonexistent()
    {
        // Act
        var deleted = await _store.DeleteAllAsync("nonexistent");

        // Assert
        Assert.Equal(0, deleted);
    }

    [Fact]
    public async Task SaveAsync_OverwritesExisting()
    {
        // Arrange
        var snapshotId = Guid.NewGuid().ToString("N");
        var timestamp = DateTimeOffset.UtcNow;
        var original = new ExperimentSnapshot
        {
            Id = snapshotId,
            ExperimentName = "test-exp",
            Timestamp = timestamp,
            Type = SnapshotType.PreRegistration,
            Description = "Original"
        };
        await _store.SaveAsync(original);

        var updated = new ExperimentSnapshot
        {
            Id = snapshotId,
            ExperimentName = "test-exp",
            Timestamp = timestamp,
            Type = SnapshotType.PreRegistration,
            Description = "Updated"
        };

        // Act
        await _store.SaveAsync(updated);
        var retrieved = await _store.GetAsync(snapshotId);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("Updated", retrieved.Description);
    }

    [Fact]
    public async Task SaveAsync_AcceptsCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var snapshot = CreateSnapshot();

        // Act - should not throw (implementation doesn't check token but accepts it)
        await _store.SaveAsync(snapshot, cts.Token);
        var retrieved = await _store.GetAsync(snapshot.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(snapshot.Id, retrieved.Id);
    }

    [Fact]
    public async Task Snapshot_PreservesAllProperties()
    {
        // Arrange
        var snapshot = new ExperimentSnapshot
        {
            Id = "test-id",
            ExperimentName = "exp",
            Timestamp = DateTimeOffset.UtcNow,
            Type = SnapshotType.FinalAnalysis,
            Description = "Final analysis",
            Notes = "Some notes",
            Tags = new List<string> { "tag1", "tag2" },
            FrameworkVersion = "1.0.0",
            DataHash = "abc123",
            Environment = new EnvironmentInfo
            {
                MachineName = "test-machine",
                OperatingSystem = "Windows",
                RuntimeVersion = ".NET 10.0"
            },
            Metadata = new Dictionary<string, object>
            {
                ["key"] = "value"
            }
        };

        // Act
        await _store.SaveAsync(snapshot);
        var retrieved = await _store.GetAsync("test-id");

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("exp", retrieved.ExperimentName);
        Assert.Equal(SnapshotType.FinalAnalysis, retrieved.Type);
        Assert.Equal("Final analysis", retrieved.Description);
        Assert.Equal("Some notes", retrieved.Notes);
        Assert.NotNull(retrieved.Tags);
        Assert.Equal(2, retrieved.Tags.Count);
        Assert.Equal("1.0.0", retrieved.FrameworkVersion);
        Assert.Equal("abc123", retrieved.DataHash);
        Assert.NotNull(retrieved.Environment);
        Assert.Equal("test-machine", retrieved.Environment.MachineName);
        Assert.NotNull(retrieved.Metadata);
        Assert.Equal("value", retrieved.Metadata["key"]);
    }
}
