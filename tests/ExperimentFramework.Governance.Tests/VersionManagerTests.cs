using ExperimentFramework.Audit;
using ExperimentFramework.Governance.Versioning;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ExperimentFramework.Governance.Tests;

public class VersionManagerTests
{
    private readonly ILogger<VersionManager> _logger;
    private readonly IAuditSink _auditSink;
    private readonly VersionManager _sut;

    public VersionManagerTests()
    {
        _logger = Substitute.For<ILogger<VersionManager>>();
        _auditSink = Substitute.For<IAuditSink>();
        _sut = new VersionManager(_logger, _auditSink);
    }

    [Fact]
    public async Task CreateVersionAsync_CreatesFirstVersion()
    {
        // Arrange
        var experimentName = "test-experiment";
        var config = new { Setting = "value" };

        // Act
        var version = await _sut.CreateVersionAsync(experimentName, config, "user1", "Initial version");

        // Assert
        version.VersionNumber.Should().Be(1);
        version.ExperimentName.Should().Be(experimentName);
        version.CreatedBy.Should().Be("user1");
        version.ChangeDescription.Should().Be("Initial version");
    }

    [Fact]
    public async Task CreateVersionAsync_IncrementsVersionNumber()
    {
        // Arrange
        var experimentName = "test-experiment";
        await _sut.CreateVersionAsync(experimentName, new { V = 1 });
        await _sut.CreateVersionAsync(experimentName, new { V = 2 });

        // Act
        var version = await _sut.CreateVersionAsync(experimentName, new { V = 3 });

        // Assert
        version.VersionNumber.Should().Be(3);
    }

    [Fact]
    public async Task CreateVersionAsync_RecordsAuditEvent()
    {
        // Arrange
        var experimentName = "test-experiment";

        // Act
        await _sut.CreateVersionAsync(experimentName, new { V = 1 }, "user1");

        // Assert
        await _auditSink.Received(1).RecordAsync(
            Arg.Is<AuditEvent>(e =>
                e.ExperimentName == experimentName &&
                e.EventType == AuditEventType.ExperimentModified),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVersion_ReturnsCorrectVersion()
    {
        // Arrange
        var experimentName = "test-experiment";
        await _sut.CreateVersionAsync(experimentName, new { V = 1 });
        await _sut.CreateVersionAsync(experimentName, new { V = 2 });
        await _sut.CreateVersionAsync(experimentName, new { V = 3 });

        // Act
        var version = _sut.GetVersion(experimentName, 2);

        // Assert
        version.Should().NotBeNull();
        version!.VersionNumber.Should().Be(2);
    }

    [Fact]
    public void GetVersion_ReturnsNull_WhenVersionNotFound()
    {
        // Act
        var version = _sut.GetVersion("non-existent", 1);

        // Assert
        version.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestVersion_ReturnsNewestVersion()
    {
        // Arrange
        var experimentName = "test-experiment";
        await _sut.CreateVersionAsync(experimentName, new { V = 1 });
        await _sut.CreateVersionAsync(experimentName, new { V = 2 });
        await _sut.CreateVersionAsync(experimentName, new { V = 3 });

        // Act
        var latest = _sut.GetLatestVersion(experimentName);

        // Assert
        latest.Should().NotBeNull();
        latest!.VersionNumber.Should().Be(3);
    }

    [Fact]
    public void GetLatestVersion_ReturnsNull_WhenNoVersions()
    {
        // Act
        var latest = _sut.GetLatestVersion("non-existent");

        // Assert
        latest.Should().BeNull();
    }

    [Fact]
    public async Task GetAllVersions_ReturnsAllInOrder()
    {
        // Arrange
        var experimentName = "test-experiment";
        await _sut.CreateVersionAsync(experimentName, new { V = 1 });
        await _sut.CreateVersionAsync(experimentName, new { V = 2 });
        await _sut.CreateVersionAsync(experimentName, new { V = 3 });

        // Act
        var versions = _sut.GetAllVersions(experimentName);

        // Assert
        versions.Should().HaveCount(3);
        versions[0].VersionNumber.Should().Be(1);
        versions[1].VersionNumber.Should().Be(2);
        versions[2].VersionNumber.Should().Be(3);
    }

    [Fact]
    public async Task GetDiff_ReturnsNull_WhenVersionNotFound()
    {
        // Arrange
        await _sut.CreateVersionAsync("test", new { V = 1 });

        // Act
        var diff = _sut.GetDiff("test", 1, 999);

        // Assert
        diff.Should().BeNull();
    }

    [Fact]
    public async Task GetDiff_ReturnsChanges_BetweenVersions()
    {
        // Arrange
        var experimentName = "test-experiment";
        await _sut.CreateVersionAsync(experimentName, new { Setting = "old" });
        await _sut.CreateVersionAsync(experimentName, new { Setting = "new" });

        // Act
        var diff = _sut.GetDiff(experimentName, 1, 2);

        // Assert
        diff.Should().NotBeNull();
        diff!.FromVersion.Should().Be(1);
        diff.ToVersion.Should().Be(2);
        diff.Changes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RollbackToVersionAsync_CreatesNewVersionWithOldConfig()
    {
        // Arrange
        var experimentName = "test-experiment";
        var config1 = new { Setting = "v1" };
        var config2 = new { Setting = "v2" };
        var config3 = new { Setting = "v3" };

        await _sut.CreateVersionAsync(experimentName, config1);
        await _sut.CreateVersionAsync(experimentName, config2);
        await _sut.CreateVersionAsync(experimentName, config3);

        // Act
        var rolledBack = await _sut.RollbackToVersionAsync(experimentName, 1, "user1");

        // Assert
        rolledBack.VersionNumber.Should().Be(4); // New version
        rolledBack.ChangeDescription.Should().Contain("Rolled back");
        rolledBack.CreatedBy.Should().Be("user1");
    }

    [Fact]
    public async Task RollbackToVersionAsync_ThrowsException_WhenVersionNotFound()
    {
        // Arrange
        await _sut.CreateVersionAsync("test", new { V = 1 });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.RollbackToVersionAsync("test", 999));
    }

    [Fact]
    public async Task CreateVersionAsync_StoresLifecycleState()
    {
        // Arrange
        var experimentName = "test-experiment";

        // Act
        var version = await _sut.CreateVersionAsync(
            experimentName,
            new { V = 1 },
            lifecycleState: ExperimentLifecycleState.Running);

        // Assert
        version.LifecycleState.Should().Be(ExperimentLifecycleState.Running);
    }
}
