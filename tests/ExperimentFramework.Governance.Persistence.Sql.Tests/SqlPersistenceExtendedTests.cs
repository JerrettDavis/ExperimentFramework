using ExperimentFramework.Governance.Persistence.Models;
using ExperimentFramework.Governance.Persistence.Sql;
using ExperimentFramework.Governance.Policy;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Governance.Persistence.Sql.Tests;

[Feature("SQL persistence backplane – extended CRUD, concurrency, and policy evaluation")]
public sealed class SqlPersistenceExtendedTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private static SqlGovernancePersistenceBackplane CreateBackplane()
    {
        var options = new DbContextOptionsBuilder<GovernanceDbContext>()
            .UseInMemoryDatabase($"GovernanceExtTest_{Guid.NewGuid()}")
            .Options;
        var dbContext = new GovernanceDbContext(options);
        var logger = Substitute.For<ILogger<SqlGovernancePersistenceBackplane>>();
        return new SqlGovernancePersistenceBackplane(dbContext, logger);
    }

    private static PersistedExperimentState MakeState(string name, ExperimentLifecycleState state = ExperimentLifecycleState.Draft)
        => new()
        {
            ExperimentName = name,
            CurrentState = state,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "test-user",
            ETag = Guid.NewGuid().ToString()
        };

    // ── CRUD ─────────────────────────────────────────────────────────────────

    [Scenario("Get nonexistent experiment returns null")]
    [Fact]
    public async Task Get_nonexistent_returns_null()
    {
        var backplane = CreateBackplane();
        var result = await backplane.GetExperimentStateAsync("does-not-exist");
        result.Should().BeNull();
    }

    [Scenario("Save new then update with correct ETag succeeds")]
    [Fact]
    public async Task Save_update_with_correct_etag_succeeds()
    {
        var backplane = CreateBackplane();
        var state = MakeState("update-test");

        var saveResult = await backplane.SaveExperimentStateAsync(state);
        saveResult.Success.Should().BeTrue();

        var updatedState = new PersistedExperimentState
        {
            ExperimentName = "update-test",
            CurrentState = ExperimentLifecycleState.Running,
            ConfigurationVersion = 2,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "updater",
            ETag = saveResult.NewETag!
        };

        var updateResult = await backplane.SaveExperimentStateAsync(updatedState, saveResult.NewETag);
        updateResult.Success.Should().BeTrue();

        var retrieved = await backplane.GetExperimentStateAsync("update-test");
        retrieved!.CurrentState.Should().Be(ExperimentLifecycleState.Running);
    }

    [Scenario("Update with wrong ETag fails with conflict")]
    [Fact]
    public async Task Update_with_wrong_etag_fails()
    {
        var backplane = CreateBackplane();
        var state = MakeState("wrong-etag-test");

        var first = await backplane.SaveExperimentStateAsync(state);
        first.Success.Should().BeTrue();

        var updated = new PersistedExperimentState
        {
            ExperimentName = "wrong-etag-test",
            CurrentState = ExperimentLifecycleState.Running,
            ConfigurationVersion = 2,
            LastModified = DateTimeOffset.UtcNow,
            ETag = "does-not-matter"
        };

        var second = await backplane.SaveExperimentStateAsync(updated, expectedETag: "wrong-etag-value");
        second.Success.Should().BeFalse();
        second.ConflictDetected.Should().BeTrue();
    }

    // ── transition history ───────────────────────────────────────────────────

    [Scenario("Empty transition history returns empty list")]
    [Fact]
    public async Task Empty_transition_history_returns_empty()
    {
        var backplane = CreateBackplane();
        var history = await backplane.GetStateTransitionHistoryAsync("no-transitions-exp");
        history.Should().BeEmpty();
    }

    [Scenario("Appended transitions are returned in insertion order")]
    [Fact]
    public async Task Appended_transitions_returned_in_order()
    {
        var backplane = CreateBackplane();
        var states = new[]
        {
            ExperimentLifecycleState.Draft,
            ExperimentLifecycleState.PendingApproval,
            ExperimentLifecycleState.Approved,
            ExperimentLifecycleState.Running
        };

        for (var i = 0; i < states.Length - 1; i++)
        {
            await backplane.AppendStateTransitionAsync(new PersistedStateTransition
            {
                TransitionId = $"t{i}",
                ExperimentName = "ordered-transitions",
                FromState = states[i],
                ToState = states[i + 1],
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(i),
                Actor = $"user-{i}"
            });
        }

        var history = await backplane.GetStateTransitionHistoryAsync("ordered-transitions");
        history.Count.Should().Be(3);
        history[0].FromState.Should().Be(ExperimentLifecycleState.Draft);
        history[2].ToState.Should().Be(ExperimentLifecycleState.Running);
    }

    // ── approval records ─────────────────────────────────────────────────────

    [Scenario("Approval record can be retrieved by transition ID")]
    [Fact]
    public async Task Approval_retrievable_by_transition_id()
    {
        var backplane = CreateBackplane();
        var transitionId = Guid.NewGuid().ToString();

        await backplane.AppendApprovalRecordAsync(new PersistedApprovalRecord
        {
            ApprovalId = Guid.NewGuid().ToString(),
            ExperimentName = "approval-exp",
            TransitionId = transitionId,
            ToState = ExperimentLifecycleState.Approved,
            IsApproved = true,
            Approver = "reviewer@test.com",
            Timestamp = DateTimeOffset.UtcNow,
            GateName = "ManualReview"
        });

        var approvals = await backplane.GetApprovalRecordsByTransitionAsync(transitionId);
        approvals.Should().HaveCount(1);
        approvals[0].Approver.Should().Be("reviewer@test.com");
    }

    [Scenario("Multiple approval records for same experiment accumulate")]
    [Fact]
    public async Task Multiple_approvals_accumulate()
    {
        var backplane = CreateBackplane();

        for (var i = 0; i < 3; i++)
        {
            await backplane.AppendApprovalRecordAsync(new PersistedApprovalRecord
            {
                ApprovalId = $"ap-{i}",
                ExperimentName = "multi-approval-exp",
                TransitionId = $"t-{i}",
                ToState = ExperimentLifecycleState.Approved,
                IsApproved = i % 2 == 0,
                Timestamp = DateTimeOffset.UtcNow,
                GateName = "Gate"
            });
        }

        var approvals = await backplane.GetApprovalRecordsAsync("multi-approval-exp");
        approvals.Should().HaveCount(3);
    }

    // ── policy evaluations ────────────────────────────────────────────────────

    [Scenario("Policy evaluation can be appended and retrieved")]
    [Fact]
    public async Task Policy_evaluation_appended_and_retrieved()
    {
        var backplane = CreateBackplane();

        await backplane.AppendPolicyEvaluationAsync(new PersistedPolicyEvaluation
        {
            EvaluationId = Guid.NewGuid().ToString(),
            ExperimentName = "policy-exp",
            PolicyName = "TrafficPolicy",
            IsCompliant = true,
            Reason = "Traffic within limits",
            Severity = PolicyViolationSeverity.Critical,
            Timestamp = DateTimeOffset.UtcNow
        });

        var evaluations = await backplane.GetPolicyEvaluationsAsync("policy-exp");
        evaluations.Should().HaveCount(1);
        evaluations[0].PolicyName.Should().Be("TrafficPolicy");
        evaluations[0].IsCompliant.Should().BeTrue();
    }

    [Scenario("Latest policy evaluation for a policy name is retrieved")]
    [Fact]
    public async Task Latest_policy_evaluation_retrieved_correctly()
    {
        var backplane = CreateBackplane();

        for (var i = 1; i <= 3; i++)
        {
            await backplane.AppendPolicyEvaluationAsync(new PersistedPolicyEvaluation
            {
                EvaluationId = $"eval-{i}",
                ExperimentName = "latest-policy-exp",
                PolicyName = "SampleSizePolicy",
                IsCompliant = i == 3, // Only 3rd is compliant
                Reason = $"Evaluation {i}",
                Severity = PolicyViolationSeverity.Critical,
                Timestamp = DateTimeOffset.UtcNow.AddMinutes(i)
            });
        }

        var latest = await backplane.GetLatestPolicyEvaluationAsync("latest-policy-exp", "SampleSizePolicy");
        latest.Should().NotBeNull();
        latest!.EvaluationId.Should().Be("eval-3");
        latest.IsCompliant.Should().BeTrue();
    }

    // ── environment scoping ───────────────────────────────────────────────────

    [Scenario("Environment-scoped states are isolated from default scope")]
    [Fact]
    public async Task Environment_scoped_states_are_isolated()
    {
        var backplane = CreateBackplane();

        await backplane.SaveExperimentStateAsync(new PersistedExperimentState
        {
            ExperimentName = "env-exp",
            CurrentState = ExperimentLifecycleState.Draft,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            ETag = "etag-prod",
            Environment = "production"
        });

        await backplane.SaveExperimentStateAsync(new PersistedExperimentState
        {
            ExperimentName = "env-exp",
            CurrentState = ExperimentLifecycleState.Running,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            ETag = "etag-staging",
            Environment = "staging"
        });

        var prodState = await backplane.GetExperimentStateAsync("env-exp", environment: "production");
        var stagingState = await backplane.GetExperimentStateAsync("env-exp", environment: "staging");

        prodState!.CurrentState.Should().Be(ExperimentLifecycleState.Draft);
        stagingState!.CurrentState.Should().Be(ExperimentLifecycleState.Running);
    }
}
