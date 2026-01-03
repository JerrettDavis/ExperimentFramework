using ExperimentFramework.Governance.Persistence.Models;
using ExperimentFramework.Governance.Persistence.Sql;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Governance.Persistence.Sql.Tests;

[Feature("SQL persistence backplane provides durable storage with optimistic concurrency")]
public sealed class SqlPersistenceBackplaneTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestContext(
        GovernanceDbContext DbContext,
        SqlGovernancePersistenceBackplane Backplane,
        PersistedExperimentState? State = null,
        PersistenceResult<PersistedExperimentState>? Result = null,
        string? SavedETag = null);

    private static TestContext CreateBackplane()
    {
        var options = new DbContextOptionsBuilder<GovernanceDbContext>()
            .UseInMemoryDatabase($"GovernanceTest_{Guid.NewGuid()}")
            .Options;

        var dbContext = new GovernanceDbContext(options);
        var logger = Substitute.For<ILogger<SqlGovernancePersistenceBackplane>>();
        var backplane = new SqlGovernancePersistenceBackplane(dbContext, logger);

        return new TestContext(dbContext, backplane);
    }

    private static TestContext CreateExperimentState(TestContext context, string name)
        => context with
        {
            State = new PersistedExperimentState
            {
                ExperimentName = name,
                CurrentState = ExperimentLifecycleState.Draft,
                ConfigurationVersion = 1,
                LastModified = DateTimeOffset.UtcNow,
                LastModifiedBy = "test-user",
                ETag = Guid.NewGuid().ToString()
            }
        };

    private static Task<TestContext> SaveState(TestContext c)
        => Task.Run(async () =>
        {
            var result = await c.Backplane.SaveExperimentStateAsync(c.State!, expectedETag: null);
            return c with { Result = result };
        });

    private static Task<TestContext> RetrieveState(TestContext c, string name)
        => Task.Run(async () =>
        {
            var retrieved = await c.Backplane.GetExperimentStateAsync(name);
            return c with { State = retrieved };
        });

    [Scenario("Save new experiment state to SQL database")]
    [Fact]
    public Task Save_new_experiment_state_sql()
        => Given("a SQL backplane", CreateBackplane)
            .And("an experiment state", c => CreateExperimentState(c, "sql-test-exp"))
            .When("state is saved", SaveState)
            .Then("save succeeds", c => c.Result!.Success.Should().BeTrue())
            .And("new ETag is generated", c => c.Result!.NewETag.Should().NotBeNullOrEmpty())
            .And("state is persisted in database", c => Task.Run(async () =>
            {
                var entity = await c.DbContext.ExperimentStates
                    .FirstOrDefaultAsync(e => e.ExperimentName == "sql-test-exp");
                entity.Should().NotBeNull();
            }))
            .AssertPassed();

    [Scenario("Retrieve experiment state from SQL database")]
    [Fact]
    public Task Retrieve_state_from_sql()
        => Given("a SQL backplane", CreateBackplane)
            .And("an experiment state", c => CreateExperimentState(c, "sql-test-exp"))
            .And("state is saved", SaveState)
            .When("state is retrieved", c => RetrieveState(c, "sql-test-exp"))
            .Then("state is found", c => c.State.Should().NotBeNull())
            .And("experiment name matches", c => c.State!.ExperimentName.Should().Be("sql-test-exp"))
            .And("lifecycle state is correct", c => c.State!.CurrentState.Should().Be(ExperimentLifecycleState.Draft))
            .AssertPassed();

    [Scenario("SQL persistence enforces optimistic concurrency with ETag")]
    [Fact]
    public Task Sql_optimistic_concurrency()
        => Given("a SQL backplane", CreateBackplane)
            .And("an experiment state", c => CreateExperimentState(c, "concurrency-test"))
            .And("state is saved", c => Task.Run(async () =>
            {
                await c.Backplane.SaveExperimentStateAsync(c.State!, expectedETag: null);
                return c;
            }))
            .When("state is updated with wrong ETag", c => Task.Run(async () =>
            {
                var updatedState = new PersistedExperimentState
                {
                    ExperimentName = "concurrency-test",
                    CurrentState = ExperimentLifecycleState.Running,
                    ConfigurationVersion = 2,
                    LastModified = DateTimeOffset.UtcNow,
                    LastModifiedBy = "test-user",
                    ETag = Guid.NewGuid().ToString()
                };
                var result = await c.Backplane.SaveExperimentStateAsync(updatedState, expectedETag: "invalid-etag");
                return c with { Result = result };
            }))
            .Then("update fails with conflict", c => c.Result!.ConflictDetected.Should().BeTrue())
            .And("original state unchanged in database", c => Task.Run(async () =>
            {
                var state = await c.Backplane.GetExperimentStateAsync("concurrency-test");
                state!.CurrentState.Should().Be(ExperimentLifecycleState.Draft);
            }))
            .AssertPassed();

    [Scenario("SQL persistence stores immutable state transition history")]
    [Fact]
    public Task Sql_immutable_transition_history()
        => Given("a SQL backplane", CreateBackplane)
            .And("multiple transitions are appended", c => Task.Run(async () =>
            {
                await c.Backplane.AppendStateTransitionAsync(new PersistedStateTransition
                {
                    TransitionId = "t1",
                    ExperimentName = "history-test",
                    FromState = ExperimentLifecycleState.Draft,
                    ToState = ExperimentLifecycleState.PendingApproval,
                    Timestamp = DateTimeOffset.UtcNow,
                    Actor = "user1"
                });
                await c.Backplane.AppendStateTransitionAsync(new PersistedStateTransition
                {
                    TransitionId = "t2",
                    ExperimentName = "history-test",
                    FromState = ExperimentLifecycleState.PendingApproval,
                    ToState = ExperimentLifecycleState.Approved,
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(1),
                    Actor = "user2"
                });
                return c;
            }))
            .When("history is retrieved", c => Task.Run(async () =>
            {
                var history = await c.Backplane.GetStateTransitionHistoryAsync("history-test");
                return (c, history);
            }))
            .Then("all transitions are returned in order", r => r.history.Count.Should().Be(2))
            .And("transitions are persisted in database", r => Task.Run(async () =>
            {
                var count = await r.c.DbContext.StateTransitions
                    .CountAsync(t => t.ExperimentName == "history-test");
                count.Should().Be(2);
            }))
            .AssertPassed();

    [Scenario("SQL persistence stores approval records")]
    [Fact]
    public Task Sql_approval_records()
        => Given("a SQL backplane", CreateBackplane)
            .And("approval record is appended", c => Task.Run(async () =>
            {
                await c.Backplane.AppendApprovalRecordAsync(new PersistedApprovalRecord
                {
                    ApprovalId = "a1",
                    ExperimentName = "approval-test",
                    TransitionId = "t1",
                    ToState = ExperimentLifecycleState.Approved,
                    IsApproved = true,
                    Approver = "manager@test.com",
                    Timestamp = DateTimeOffset.UtcNow,
                    GateName = "ManualApproval",
                    Reason = "Approved for production"
                });
                return c;
            }))
            .When("approvals are retrieved", c => Task.Run(async () =>
            {
                var approvals = await c.Backplane.GetApprovalRecordsAsync("approval-test");
                return (c, approvals);
            }))
            .Then("approval is returned with correct details", r =>
            {
                r.approvals.Count.Should().Be(1);
                r.approvals[0].IsApproved.Should().BeTrue();
                r.approvals[0].Approver.Should().Be("manager@test.com");
                r.approvals[0].Reason.Should().Be("Approved for production");
            })
            .AssertPassed();

    [Scenario("SQL persistence stores configuration versions")]
    [Fact]
    public Task Sql_configuration_versions()
        => Given("a SQL backplane", CreateBackplane)
            .And("multiple versions are appended", c => Task.Run(async () =>
            {
                await c.Backplane.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
                {
                    ExperimentName = "version-test",
                    VersionNumber = 1,
                    ConfigurationJson = "{\"traffic\": 5}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedBy = "user1",
                    ConfigurationHash = "hash1",
                    ChangeDescription = "Initial version"
                });
                await c.Backplane.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
                {
                    ExperimentName = "version-test",
                    VersionNumber = 2,
                    ConfigurationJson = "{\"traffic\": 10}",
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(5),
                    CreatedBy = "user2",
                    ConfigurationHash = "hash2",
                    ChangeDescription = "Increased traffic"
                });
                return c;
            }))
            .When("latest version is retrieved", c => Task.Run(async () =>
            {
                var latest = await c.Backplane.GetLatestConfigurationVersionAsync("version-test");
                return (c, latest);
            }))
            .Then("latest version is returned", r => r.latest.Should().NotBeNull())
            .And("version number is 2", r => r.latest!.VersionNumber.Should().Be(2))
            .And("change description is correct", r => r.latest!.ChangeDescription.Should().Be("Increased traffic"))
            .AssertPassed();

    [Scenario("SQL persistence supports tenant isolation")]
    [Fact]
    public Task Sql_tenant_isolation()
        => Given("a SQL backplane", CreateBackplane)
            .And("states for different tenants are saved", c => Task.Run(async () =>
            {
                await c.Backplane.SaveExperimentStateAsync(new PersistedExperimentState
                {
                    ExperimentName = "multi-tenant-exp",
                    CurrentState = ExperimentLifecycleState.Draft,
                    ConfigurationVersion = 1,
                    LastModified = DateTimeOffset.UtcNow,
                    ETag = "etag1",
                    TenantId = "tenant-alpha"
                });
                await c.Backplane.SaveExperimentStateAsync(new PersistedExperimentState
                {
                    ExperimentName = "multi-tenant-exp",
                    CurrentState = ExperimentLifecycleState.Running,
                    ConfigurationVersion = 1,
                    LastModified = DateTimeOffset.UtcNow,
                    ETag = "etag2",
                    TenantId = "tenant-beta"
                });
                return c;
            }))
            .When("state for tenant-alpha is retrieved", c => Task.Run(async () =>
            {
                var state = await c.Backplane.GetExperimentStateAsync("multi-tenant-exp", tenantId: "tenant-alpha");
                return (c, state);
            }))
            .Then("correct tenant state is returned", r => r.state.Should().NotBeNull())
            .And("state belongs to tenant-alpha", r => r.state!.TenantId.Should().Be("tenant-alpha"))
            .And("state is Draft", r => r.state!.CurrentState.Should().Be(ExperimentLifecycleState.Draft))
            .When("state for tenant-beta is retrieved", r => Task.Run(async () =>
            {
                var state = await r.c.Backplane.GetExperimentStateAsync("multi-tenant-exp", tenantId: "tenant-beta");
                return (r.c, state);
            }))
            .Then("tenant-beta state is Running", r => r.state!.CurrentState.Should().Be(ExperimentLifecycleState.Running))
            .AssertPassed();
}
