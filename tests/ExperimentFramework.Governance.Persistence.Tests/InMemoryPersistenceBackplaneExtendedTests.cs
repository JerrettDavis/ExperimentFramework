using ExperimentFramework.Governance.Persistence.Models;
using ExperimentFramework.Governance.Policy;
using FluentAssertions;

namespace ExperimentFramework.Governance.Persistence.Tests;

/// <summary>
/// Additional tests for InMemoryGovernancePersistenceBackplane covering policy evaluations,
/// approval by transition, specific version lookup, and environment scoping.
/// </summary>
public sealed class InMemoryPersistenceBackplaneExtendedTests
{
    private static InMemoryGovernancePersistenceBackplane CreateBackplane() =>
        new InMemoryGovernancePersistenceBackplane();

    // ===== Policy Evaluation Tests =====

    [Fact]
    public async Task AppendPolicyEvaluation_ThenGetAll_ReturnsInOrder()
    {
        var bp = CreateBackplane();

        await bp.AppendPolicyEvaluationAsync(new PersistedPolicyEvaluation
        {
            EvaluationId = "e1",
            ExperimentName = "exp",
            PolicyName = "SamplePolicy",
            IsCompliant = true,
            Severity = PolicyViolationSeverity.Info,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5),
        });

        await bp.AppendPolicyEvaluationAsync(new PersistedPolicyEvaluation
        {
            EvaluationId = "e2",
            ExperimentName = "exp",
            PolicyName = "OtherPolicy",
            IsCompliant = false,
            Severity = PolicyViolationSeverity.Error,
            Timestamp = DateTimeOffset.UtcNow,
        });

        var evaluations = await bp.GetPolicyEvaluationsAsync("exp");

        evaluations.Should().HaveCount(2);
        evaluations[0].EvaluationId.Should().Be("e1");
        evaluations[1].EvaluationId.Should().Be("e2");
    }

    [Fact]
    public async Task GetPolicyEvaluations_NonExistentExperiment_ReturnsEmpty()
    {
        var bp = CreateBackplane();
        var evaluations = await bp.GetPolicyEvaluationsAsync("no-such-experiment");

        evaluations.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLatestPolicyEvaluation_ReturnsNewestForPolicy()
    {
        var bp = CreateBackplane();

        await bp.AppendPolicyEvaluationAsync(new PersistedPolicyEvaluation
        {
            EvaluationId = "e1",
            ExperimentName = "exp",
            PolicyName = "MyPolicy",
            IsCompliant = false,
            Severity = PolicyViolationSeverity.Warning,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-10),
        });

        await bp.AppendPolicyEvaluationAsync(new PersistedPolicyEvaluation
        {
            EvaluationId = "e2",
            ExperimentName = "exp",
            PolicyName = "MyPolicy",
            IsCompliant = true,
            Severity = PolicyViolationSeverity.Info,
            Timestamp = DateTimeOffset.UtcNow,
        });

        var latest = await bp.GetLatestPolicyEvaluationAsync("exp", "MyPolicy");

        latest.Should().NotBeNull();
        latest!.EvaluationId.Should().Be("e2");
        latest.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public async Task GetLatestPolicyEvaluation_NonExistentExperiment_ReturnsNull()
    {
        var bp = CreateBackplane();
        var latest = await bp.GetLatestPolicyEvaluationAsync("no-such", "AnyPolicy");

        latest.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestPolicyEvaluation_NonExistentPolicy_ReturnsNull()
    {
        var bp = CreateBackplane();

        await bp.AppendPolicyEvaluationAsync(new PersistedPolicyEvaluation
        {
            EvaluationId = "e1",
            ExperimentName = "exp",
            PolicyName = "OtherPolicy",
            IsCompliant = true,
            Severity = PolicyViolationSeverity.Info,
            Timestamp = DateTimeOffset.UtcNow,
        });

        var latest = await bp.GetLatestPolicyEvaluationAsync("exp", "NonExistentPolicy");

        latest.Should().BeNull();
    }

    // ===== Approval by Transition Tests =====

    [Fact]
    public async Task GetApprovalRecordsByTransition_ReturnsMatchingApprovals()
    {
        var bp = CreateBackplane();

        await bp.AppendApprovalRecordAsync(new PersistedApprovalRecord
        {
            ApprovalId = "a1",
            ExperimentName = "exp",
            TransitionId = "t1",
            ToState = ExperimentLifecycleState.Approved,
            IsApproved = true,
            Approver = "alice",
            Timestamp = DateTimeOffset.UtcNow,
            GateName = "Manual"
        });

        await bp.AppendApprovalRecordAsync(new PersistedApprovalRecord
        {
            ApprovalId = "a2",
            ExperimentName = "exp",
            TransitionId = "t1",
            ToState = ExperimentLifecycleState.Approved,
            IsApproved = true,
            Approver = "bob",
            Timestamp = DateTimeOffset.UtcNow.AddSeconds(1),
            GateName = "Manual"
        });

        var records = await bp.GetApprovalRecordsByTransitionAsync("t1");

        records.Should().HaveCount(2);
        records.Select(r => r.ApprovalId).Should().Contain(new[] { "a1", "a2" });
    }

    [Fact]
    public async Task GetApprovalRecordsByTransition_NonExistentTransition_ReturnsEmpty()
    {
        var bp = CreateBackplane();
        var records = await bp.GetApprovalRecordsByTransitionAsync("no-such-transition");

        records.Should().BeEmpty();
    }

    // ===== Configuration Version Tests =====

    [Fact]
    public async Task GetConfigurationVersion_ByVersionNumber_ReturnsCorrectVersion()
    {
        var bp = CreateBackplane();

        await bp.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
        {
            ExperimentName = "exp",
            VersionNumber = 1,
            ConfigurationJson = "{\"v\":1}",
            CreatedAt = DateTimeOffset.UtcNow,
            ConfigurationHash = "hash1"
        });

        await bp.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
        {
            ExperimentName = "exp",
            VersionNumber = 2,
            ConfigurationJson = "{\"v\":2}",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1),
            ConfigurationHash = "hash2"
        });

        var v1 = await bp.GetConfigurationVersionAsync("exp", 1);
        var v2 = await bp.GetConfigurationVersionAsync("exp", 2);

        v1.Should().NotBeNull();
        v1!.ConfigurationHash.Should().Be("hash1");

        v2.Should().NotBeNull();
        v2!.ConfigurationHash.Should().Be("hash2");
    }

    [Fact]
    public async Task GetConfigurationVersion_NonExistentVersion_ReturnsNull()
    {
        var bp = CreateBackplane();

        await bp.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
        {
            ExperimentName = "exp",
            VersionNumber = 1,
            ConfigurationJson = "{}",
            CreatedAt = DateTimeOffset.UtcNow,
            ConfigurationHash = "hash1"
        });

        var result = await bp.GetConfigurationVersionAsync("exp", 99);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetConfigurationVersion_NonExistentExperiment_ReturnsNull()
    {
        var bp = CreateBackplane();
        var result = await bp.GetConfigurationVersionAsync("no-such", 1);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLatestConfigurationVersion_NonExistentExperiment_ReturnsNull()
    {
        var bp = CreateBackplane();
        var result = await bp.GetLatestConfigurationVersionAsync("no-such");

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllConfigurationVersions_NonExistentExperiment_ReturnsEmpty()
    {
        var bp = CreateBackplane();
        var result = await bp.GetAllConfigurationVersionsAsync("no-such");

        result.Should().BeEmpty();
    }

    // ===== Environment Scoping Tests =====

    [Fact]
    public async Task SaveAndRetrieve_WithEnvironment_ScopedCorrectly()
    {
        var bp = CreateBackplane();

        await bp.SaveExperimentStateAsync(new PersistedExperimentState
        {
            ExperimentName = "exp",
            CurrentState = ExperimentLifecycleState.Draft,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            ETag = "etag1",
            Environment = "staging"
        });

        await bp.SaveExperimentStateAsync(new PersistedExperimentState
        {
            ExperimentName = "exp",
            CurrentState = ExperimentLifecycleState.Running,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            ETag = "etag2",
            Environment = "production"
        });

        var staging = await bp.GetExperimentStateAsync("exp", environment: "staging");
        var prod = await bp.GetExperimentStateAsync("exp", environment: "production");

        staging.Should().NotBeNull();
        staging!.CurrentState.Should().Be(ExperimentLifecycleState.Draft);

        prod.Should().NotBeNull();
        prod!.CurrentState.Should().Be(ExperimentLifecycleState.Running);
    }

    // ===== State Transition History edge cases =====

    [Fact]
    public async Task GetStateTransitionHistory_NonExistentExperiment_ReturnsEmpty()
    {
        var bp = CreateBackplane();
        var history = await bp.GetStateTransitionHistoryAsync("no-such");

        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApprovalRecords_NonExistentExperiment_ReturnsEmpty()
    {
        var bp = CreateBackplane();
        var records = await bp.GetApprovalRecordsAsync("no-such");

        records.Should().BeEmpty();
    }

    // ===== Duplicate save (conflict on null expectedETag) =====

    [Fact]
    public async Task SaveState_DuplicateWithNullETag_ReturnsConflict()
    {
        var bp = CreateBackplane();

        var state = new PersistedExperimentState
        {
            ExperimentName = "dup-exp",
            CurrentState = ExperimentLifecycleState.Draft,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            ETag = "initial"
        };

        var first = await bp.SaveExperimentStateAsync(state, expectedETag: null);
        first.Success.Should().BeTrue();

        var second = await bp.SaveExperimentStateAsync(state, expectedETag: null);
        second.Success.Should().BeFalse();
        second.ConflictDetected.Should().BeTrue();
    }
}
