using ExperimentFramework.Governance.Persistence.Models;
using FluentAssertions;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Governance.Persistence.Tests;

[Feature("InMemory persistence backplane stores and retrieves experiment state with optimistic concurrency")]
public sealed class InMemoryPersistenceBackplaneTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Save new experiment state succeeds")]
    [Fact]
    public async Task Save_new_experiment_state()
    {
        // Given
        var backplane = new InMemoryGovernancePersistenceBackplane();
        var state = new PersistedExperimentState
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Draft,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "test-user",
            ETag = Guid.NewGuid().ToString()
        };

        // When
        var result = await backplane.SaveExperimentStateAsync(state, expectedETag: null);

        // Then
        result.Success.Should().BeTrue();
        result.NewETag.Should().NotBeNullOrEmpty();
    }

    [Scenario("Retrieve saved experiment state")]
    [Fact]
    public async Task Retrieve_saved_state()
    {
        // Given
        var backplane = new InMemoryGovernancePersistenceBackplane();
        var state = new PersistedExperimentState
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Draft,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "test-user",
            ETag = Guid.NewGuid().ToString()
        };
        var saveResult = await backplane.SaveExperimentStateAsync(state, expectedETag: null);

        // When
        var retrieved = await backplane.GetExperimentStateAsync("test-experiment");

        // Then
        retrieved.Should().NotBeNull();
        retrieved!.ExperimentName.Should().Be("test-experiment");
        retrieved.ETag.Should().Be(saveResult.NewETag);
    }

    [Scenario("Update with correct ETag succeeds")]
    [Fact]
    public async Task Update_with_correct_etag()
    {
        // Given
        var backplane = new InMemoryGovernancePersistenceBackplane();
        var state = new PersistedExperimentState
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Draft,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "test-user",
            ETag = Guid.NewGuid().ToString()
        };
        var saveResult = await backplane.SaveExperimentStateAsync(state, expectedETag: null);

        // When
        var updatedState = new PersistedExperimentState
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Running,
            ConfigurationVersion = 2,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "test-user",
            ETag = Guid.NewGuid().ToString()
        };
        var updateResult = await backplane.SaveExperimentStateAsync(updatedState, expectedETag: saveResult.NewETag);

        // Then
        updateResult.Success.Should().BeTrue();
        updateResult.NewETag.Should().NotBeNullOrEmpty();
    }

    [Scenario("Update with incorrect ETag fails with conflict")]
    [Fact]
    public async Task Update_with_incorrect_etag_fails()
    {
        // Given
        var backplane = new InMemoryGovernancePersistenceBackplane();
        var state = new PersistedExperimentState
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Draft,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "test-user",
            ETag = Guid.NewGuid().ToString()
        };
        await backplane.SaveExperimentStateAsync(state, expectedETag: null);

        // When
        var updatedState = new PersistedExperimentState
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Running,
            ConfigurationVersion = 2,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "test-user",
            ETag = Guid.NewGuid().ToString()
        };
        var updateResult = await backplane.SaveExperimentStateAsync(updatedState, expectedETag: "wrong-etag");

        // Then
        updateResult.Success.Should().BeFalse();
        updateResult.ConflictDetected.Should().BeTrue();
    }

    [Scenario("Retrieve non-existent state returns null")]
    [Fact]
    public async Task Retrieve_nonexistent_state()
    {
        // Given
        var backplane = new InMemoryGovernancePersistenceBackplane();

        // When
        var state = await backplane.GetExperimentStateAsync("non-existent");

        // Then
        state.Should().BeNull();
    }

    [Scenario("Append and retrieve state transition history")]
    [Fact]
    public async Task Append_and_retrieve_transitions()
    {
        // Given
        var backplane = new InMemoryGovernancePersistenceBackplane();
        await backplane.AppendStateTransitionAsync(new PersistedStateTransition
        {
            TransitionId = "t1",
            ExperimentName = "test-exp",
            FromState = ExperimentLifecycleState.Draft,
            ToState = ExperimentLifecycleState.PendingApproval,
            Timestamp = DateTimeOffset.UtcNow,
            Actor = "user1"
        });
        await backplane.AppendStateTransitionAsync(new PersistedStateTransition
        {
            TransitionId = "t2",
            ExperimentName = "test-exp",
            FromState = ExperimentLifecycleState.PendingApproval,
            ToState = ExperimentLifecycleState.Approved,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(1),
            Actor = "user2"
        });

        // When
        var history = await backplane.GetStateTransitionHistoryAsync("test-exp");

        // Then
        history.Count.Should().Be(2);
        history[0].TransitionId.Should().Be("t1");
        history[1].TransitionId.Should().Be("t2");
    }

    [Scenario("Append and retrieve approval records")]
    [Fact]
    public async Task Append_and_retrieve_approvals()
    {
        // Given
        var backplane = new InMemoryGovernancePersistenceBackplane();
        await backplane.AppendApprovalRecordAsync(new PersistedApprovalRecord
        {
            ApprovalId = "a1",
            ExperimentName = "test-exp",
            TransitionId = "t1",
            ToState = ExperimentLifecycleState.Approved,
            IsApproved = true,
            Approver = "manager",
            Timestamp = DateTimeOffset.UtcNow,
            GateName = "ManualApproval"
        });

        // When
        var approvals = await backplane.GetApprovalRecordsAsync("test-exp");

        // Then
        approvals.Count.Should().Be(1);
        approvals[0].ApprovalId.Should().Be("a1");
        approvals[0].IsApproved.Should().BeTrue();
        approvals[0].Approver.Should().Be("manager");
    }

    [Scenario("Append and retrieve configuration versions")]
    [Fact]
    public async Task Append_and_retrieve_versions()
    {
        // Given
        var backplane = new InMemoryGovernancePersistenceBackplane();
        await backplane.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
        {
            ExperimentName = "test-exp",
            VersionNumber = 1,
            ConfigurationJson = "{\"traffic\": 5}",
            CreatedAt = DateTimeOffset.UtcNow,
            ConfigurationHash = "hash1"
        });
        await backplane.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
        {
            ExperimentName = "test-exp",
            VersionNumber = 2,
            ConfigurationJson = "{\"traffic\": 10}",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1),
            ConfigurationHash = "hash2"
        });

        // When
        var versions = await backplane.GetAllConfigurationVersionsAsync("test-exp");

        // Then
        versions.Count.Should().Be(2);
        versions[0].VersionNumber.Should().Be(1);
        versions[1].VersionNumber.Should().Be(2);
    }

    [Scenario("Retrieve latest configuration version")]
    [Fact]
    public async Task Retrieve_latest_version()
    {
        // Given
        var backplane = new InMemoryGovernancePersistenceBackplane();
        await backplane.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
        {
            ExperimentName = "test-exp",
            VersionNumber = 1,
            ConfigurationJson = "{\"traffic\": 5}",
            CreatedAt = DateTimeOffset.UtcNow,
            ConfigurationHash = "hash1"
        });
        await backplane.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
        {
            ExperimentName = "test-exp",
            VersionNumber = 2,
            ConfigurationJson = "{\"traffic\": 10}",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1),
            ConfigurationHash = "hash2"
        });

        // When
        var latest = await backplane.GetLatestConfigurationVersionAsync("test-exp");

        // Then
        latest.Should().NotBeNull();
        latest!.VersionNumber.Should().Be(2);
    }

    [Scenario("Support multi-tenancy with tenant scoping")]
    [Fact]
    public async Task Support_multitenancy()
    {
        // Given
        var backplane = new InMemoryGovernancePersistenceBackplane();
        await backplane.SaveExperimentStateAsync(new PersistedExperimentState
        {
            ExperimentName = "exp1",
            CurrentState = ExperimentLifecycleState.Draft,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            ETag = "etag1",
            TenantId = "tenant-a"
        });
        await backplane.SaveExperimentStateAsync(new PersistedExperimentState
        {
            ExperimentName = "exp1",
            CurrentState = ExperimentLifecycleState.Running,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            ETag = "etag2",
            TenantId = "tenant-b"
        });

        // When
        var state = await backplane.GetExperimentStateAsync("exp1", tenantId: "tenant-a");

        // Then
        state.Should().NotBeNull();
        state!.TenantId.Should().Be("tenant-a");
        state.CurrentState.Should().Be(ExperimentLifecycleState.Draft);
    }
}
