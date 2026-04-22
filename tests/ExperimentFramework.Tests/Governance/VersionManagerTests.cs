using ExperimentFramework.Audit;
using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Versioning;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ExperimentFramework.Tests.Governance;

public class VersionManagerTests
{
    private readonly VersionManager _manager = new(NullLogger<VersionManager>.Instance);

    // ───────────────────────── CreateVersionAsync ─────────────────────────

    [Fact]
    public async Task CreateVersionAsync_ThrowsForNullOrWhitespaceExperimentName()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.CreateVersionAsync("", new { }));
    }

    [Fact]
    public async Task CreateVersionAsync_ThrowsWhenConfigurationIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _manager.CreateVersionAsync("exp", null!));
    }

    [Fact]
    public async Task CreateVersionAsync_ReturnsVersionWithNumber1_ForFirstVersion()
    {
        var version = await _manager.CreateVersionAsync("exp1", new { setting = "value" });

        Assert.Equal(1, version.VersionNumber);
        Assert.Equal("exp1", version.ExperimentName);
    }

    [Fact]
    public async Task CreateVersionAsync_IncrementsVersionNumber()
    {
        await _manager.CreateVersionAsync("exp2", new { v = 1 });
        var v2 = await _manager.CreateVersionAsync("exp2", new { v = 2 });

        Assert.Equal(2, v2.VersionNumber);
    }

    [Fact]
    public async Task CreateVersionAsync_RecordsActorAndDescription()
    {
        var version = await _manager.CreateVersionAsync("exp3", new { x = 1 },
            actor: "alice", changeDescription: "initial config");

        Assert.Equal("alice", version.CreatedBy);
        Assert.Equal("initial config", version.ChangeDescription);
    }

    [Fact]
    public async Task CreateVersionAsync_StoresLifecycleState()
    {
        var version = await _manager.CreateVersionAsync("exp4", new { },
            lifecycleState: ExperimentLifecycleState.Running);

        Assert.Equal(ExperimentLifecycleState.Running, version.LifecycleState);
    }

    [Fact]
    public async Task CreateVersionAsync_EmitsAuditEvent()
    {
        var auditSinkMock = new Mock<IAuditSink>();
        auditSinkMock.Setup(s => s.RecordAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var manager = new VersionManager(NullLogger<VersionManager>.Instance, auditSinkMock.Object);
        await manager.CreateVersionAsync("audited", new { });

        auditSinkMock.Verify(
            s => s.RecordAsync(It.Is<AuditEvent>(e => e.ExperimentName == "audited"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ───────────────────────── GetVersion ─────────────────────────

    [Fact]
    public void GetVersion_ThrowsForNullOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => _manager.GetVersion("", 1));
    }

    [Fact]
    public void GetVersion_ReturnsNull_WhenExperimentNotFound()
    {
        var version = _manager.GetVersion("never-created", 1);
        Assert.Null(version);
    }

    [Fact]
    public async Task GetVersion_ReturnsNull_WhenVersionNumberNotFound()
    {
        await _manager.CreateVersionAsync("exp5", new { });
        var version = _manager.GetVersion("exp5", 99);
        Assert.Null(version);
    }

    [Fact]
    public async Task GetVersion_ReturnsCorrectVersion()
    {
        await _manager.CreateVersionAsync("exp6", new { value = "a" });
        await _manager.CreateVersionAsync("exp6", new { value = "b" });

        var v1 = _manager.GetVersion("exp6", 1);
        var v2 = _manager.GetVersion("exp6", 2);

        Assert.NotNull(v1);
        Assert.NotNull(v2);
        Assert.Equal(1, v1!.VersionNumber);
        Assert.Equal(2, v2!.VersionNumber);
    }

    // ───────────────────────── GetLatestVersion ─────────────────────────

    [Fact]
    public void GetLatestVersion_ThrowsForNullOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => _manager.GetLatestVersion(""));
    }

    [Fact]
    public void GetLatestVersion_ReturnsNull_WhenNoVersions()
    {
        var latest = _manager.GetLatestVersion("no-versions");
        Assert.Null(latest);
    }

    [Fact]
    public async Task GetLatestVersion_ReturnsLastCreatedVersion()
    {
        await _manager.CreateVersionAsync("exp7", new { v = 1 });
        await _manager.CreateVersionAsync("exp7", new { v = 2 });
        await _manager.CreateVersionAsync("exp7", new { v = 3 });

        var latest = _manager.GetLatestVersion("exp7");

        Assert.NotNull(latest);
        Assert.Equal(3, latest!.VersionNumber);
    }

    // ───────────────────────── GetAllVersions ─────────────────────────

    [Fact]
    public void GetAllVersions_ThrowsForNullOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => _manager.GetAllVersions(""));
    }

    [Fact]
    public void GetAllVersions_ReturnsEmpty_WhenNoVersions()
    {
        var versions = _manager.GetAllVersions("no-versions-yet");
        Assert.Empty(versions);
    }

    [Fact]
    public async Task GetAllVersions_ReturnsAllVersionsInOrder()
    {
        await _manager.CreateVersionAsync("exp8", new { n = 1 });
        await _manager.CreateVersionAsync("exp8", new { n = 2 });

        var versions = _manager.GetAllVersions("exp8");

        Assert.Equal(2, versions.Count);
        Assert.Equal(1, versions[0].VersionNumber);
        Assert.Equal(2, versions[1].VersionNumber);
    }

    // ───────────────────────── GetDiff ─────────────────────────

    [Fact]
    public async Task GetDiff_ReturnsNull_WhenFromVersionNotFound()
    {
        await _manager.CreateVersionAsync("exp9", new { x = 1 });

        var diff = _manager.GetDiff("exp9", 99, 1);

        Assert.Null(diff);
    }

    [Fact]
    public async Task GetDiff_ReturnsNull_WhenToVersionNotFound()
    {
        await _manager.CreateVersionAsync("exp10", new { x = 1 });

        var diff = _manager.GetDiff("exp10", 1, 99);

        Assert.Null(diff);
    }

    [Fact]
    public async Task GetDiff_ReturnsEmptyChanges_WhenConfigurationsAreIdentical()
    {
        var config = new { value = "same" };
        await _manager.CreateVersionAsync("exp11", config);
        await _manager.CreateVersionAsync("exp11", config);

        var diff = _manager.GetDiff("exp11", 1, 2);

        Assert.NotNull(diff);
        Assert.Equal(1, diff!.FromVersion);
        Assert.Equal(2, diff.ToVersion);
        Assert.Empty(diff.Changes);
    }

    [Fact]
    public async Task GetDiff_ReturnsChanges_WhenConfigurationsDiffer()
    {
        await _manager.CreateVersionAsync("exp12", new { value = "old" });
        await _manager.CreateVersionAsync("exp12", new { value = "new" });

        var diff = _manager.GetDiff("exp12", 1, 2);

        Assert.NotNull(diff);
        Assert.NotEmpty(diff!.Changes);
        Assert.Equal("configuration", diff.Changes[0].Path);
    }

    // ───────────────────────── RollbackToVersionAsync ─────────────────────────

    [Fact]
    public async Task RollbackToVersionAsync_ThrowsWhenVersionNotFound()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.RollbackToVersionAsync("exp13", 99));
    }

    [Fact]
    public async Task RollbackToVersionAsync_CreatesNewVersionWithOldConfiguration()
    {
        await _manager.CreateVersionAsync("exp14", new { value = "v1" }, actor: "alice");
        await _manager.CreateVersionAsync("exp14", new { value = "v2" }, actor: "bob");

        var rolled = await _manager.RollbackToVersionAsync("exp14", 1, actor: "charlie");

        Assert.Equal(3, rolled.VersionNumber);
        Assert.Equal("charlie", rolled.CreatedBy);
        Assert.Contains("Rolled back", rolled.ChangeDescription);
    }
}
