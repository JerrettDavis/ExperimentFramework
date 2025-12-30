using ExperimentFramework.Governance.Persistence.Models;
using FluentAssertions;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Governance.Persistence.Tests;

[Feature("InMemory persistence backplane stores and retrieves experiment state with optimistic concurrency")]
public sealed class InMemoryPersistenceBackplaneTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestContext(
        InMemoryGovernancePersistenceBackplane Backplane,
        PersistedExperimentState? State = null,
        PersistenceResult<PersistedExperimentState>? Result = null,
        string? SavedETag = null);

    private static TestContext CreateBackplane()
        => new(new InMemoryGovernancePersistenceBackplane());

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
            return c with { Result = result, SavedETag = result.NewETag };
        });

    private static Task<TestContext> SaveStateWithETag(TestContext c)
        => Task.Run(async () =>
        {
            var result = await c.Backplane.SaveExperimentStateAsync(c.State!, expectedETag: c.SavedETag);
            return c with { Result = result };
        });

    private static Task<TestContext> RetrieveState(TestContext c, string name)
        => Task.Run(async () =>
        {
            var retrieved = await c.Backplane.GetExperimentStateAsync(name);
            return c with { State = retrieved };
        });

    [Scenario("Save new experiment state succeeds")]
    [Fact]
    public Task Save_new_experiment_state()
        => Given("a new backplane", CreateBackplane)
            .And("an experiment state", c => CreateExperimentState(c, "test-experiment"))
            .When("state is saved", SaveState)
            .Then("save succeeds", c => c.Result!.Success.Should().BeTrue())
            .And("new ETag is generated", c => c.Result!.NewETag.Should().NotBeNullOrEmpty())
            .AssertPassed();

    [Scenario("Retrieve saved experiment state")]
    [Fact]
    public Task Retrieve_saved_state()
        => Given("a backplane", CreateBackplane)
            .And("an experiment state", c => CreateExperimentState(c, "test-experiment"))
            .And("state is saved", SaveState)
            .When("state is retrieved", c => RetrieveState(c, "test-experiment"))
            .Then("state is found", c => c.State.Should().NotBeNull())
            .And("experiment name matches", c => c.State!.ExperimentName.Should().Be("test-experiment"))
            .And("ETag matches", c => c.State!.ETag.Should().Be(c.SavedETag))
            .AssertPassed();

    [Scenario("Update with correct ETag succeeds")]
    [Fact]
    public Task Update_with_correct_etag()
        => Given("a backplane with saved state", CreateBackplane)
            .And("initial state", c => CreateExperimentState(c, "test-experiment"))
            .And("state is saved", SaveState)
            .When("state is updated with correct ETag", c => Task.Run(async () =>
            {
                var updatedState = new PersistedExperimentState
                {
                    ExperimentName = "test-experiment",
                    CurrentState = ExperimentLifecycleState.Running,
                    ConfigurationVersion = 2,
                    LastModified = DateTimeOffset.UtcNow,
                    LastModifiedBy = "test-user",
                    ETag = Guid.NewGuid().ToString()
                };
                var result = await c.Backplane.SaveExperimentStateAsync(updatedState, expectedETag: c.SavedETag);
                return c with { Result = result };
            }))
            .Then("update succeeds", c => c.Result!.Success.Should().BeTrue())
            .And("new ETag is generated", c => c.Result!.NewETag.Should().NotBeNullOrEmpty())
            .AssertPassed();

    [Scenario("Update with incorrect ETag fails with conflict")]
    [Fact]
    public Task Update_with_incorrect_etag_fails()
        => Given("a backplane with saved state", CreateBackplane)
            .And("initial state", c => CreateExperimentState(c, "test-experiment"))
            .And("state is saved", c => Task.Run(async () =>
            {
                await c.Backplane.SaveExperimentStateAsync(c.State!, expectedETag: null);
                return c;
            }))
            .When("state is updated with wrong ETag", c => Task.Run(async () =>
            {
                var updatedState = new PersistedExperimentState
                {
                    ExperimentName = "test-experiment",
                    CurrentState = ExperimentLifecycleState.Running,
                    ConfigurationVersion = 2,
                    LastModified = DateTimeOffset.UtcNow,
                    LastModifiedBy = "test-user",
                    ETag = Guid.NewGuid().ToString()
                };
                var result = await c.Backplane.SaveExperimentStateAsync(updatedState, expectedETag: "wrong-etag");
                return c with { Result = result };
            }))
            .Then("update fails", c => c.Result!.Success.Should().BeFalse())
            .And("conflict is detected", c => c.Result!.ConflictDetected.Should().BeTrue())
            .AssertPassed();

    [Scenario("Retrieve non-existent state returns null")]
    [Fact]
    public Task Retrieve_nonexistent_state()
        => Given("a backplane", CreateBackplane)
            .When("non-existent state is retrieved", c => RetrieveState(c, "non-existent"))
            .Then("state is null", c => c.State.Should().BeNull())
            .AssertPassed();

    [Scenario("Append and retrieve state transition history")]
    [Fact]
    public Task Append_and_retrieve_transitions()
        => Given("a backplane", CreateBackplane)
            .And("transitions are appended", c => Task.Run(async () =>
            {
                await c.Backplane.AppendStateTransitionAsync(new PersistedStateTransition
                {
                    TransitionId = "t1",
                    ExperimentName = "test-exp",
                    FromState = ExperimentLifecycleState.Draft,
                    ToState = ExperimentLifecycleState.PendingApproval,
                    Timestamp = DateTimeOffset.UtcNow,
                    Actor = "user1"
                });
                await c.Backplane.AppendStateTransitionAsync(new PersistedStateTransition
                {
                    TransitionId = "t2",
                    ExperimentName = "test-exp",
                    FromState = ExperimentLifecycleState.PendingApproval,
                    ToState = ExperimentLifecycleState.Approved,
                    Timestamp = DateTimeOffset.UtcNow.AddMinutes(1),
                    Actor = "user2"
                });
                return c;
            }))
            .When("history is retrieved", c => Task.Run(async () =>
            {
                var history = await c.Backplane.GetStateTransitionHistoryAsync("test-exp");
                return (c, history);
            }))
            .Then("two transitions are returned", r => r.history.Count.Should().Be(2))
            .And("transitions are in order", r =>
            {
                r.history[0].TransitionId.Should().Be("t1");
                r.history[1].TransitionId.Should().Be("t2");
            })
            .AssertPassed();

    [Scenario("Append and retrieve approval records")]
    [Fact]
    public Task Append_and_retrieve_approvals()
        => Given("a backplane", CreateBackplane)
            .And("approval is appended", c => Task.Run(async () =>
            {
                await c.Backplane.AppendApprovalRecordAsync(new PersistedApprovalRecord
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
                return c;
            }))
            .When("approvals are retrieved", c => Task.Run(async () =>
            {
                var approvals = await c.Backplane.GetApprovalRecordsAsync("test-exp");
                return (c, approvals);
            }))
            .Then("approval is returned", r => r.approvals.Count.Should().Be(1))
            .And("approval details are correct", r =>
            {
                r.approvals[0].ApprovalId.Should().Be("a1");
                r.approvals[0].IsApproved.Should().BeTrue();
                r.approvals[0].Approver.Should().Be("manager");
            })
            .AssertPassed();

    [Scenario("Append and retrieve configuration versions")]
    [Fact]
    public Task Append_and_retrieve_versions()
        => Given("a backplane", CreateBackplane)
            .And("versions are appended", c => Task.Run(async () =>
            {
                await c.Backplane.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
                {
                    ExperimentName = "test-exp",
                    VersionNumber = 1,
                    ConfigurationJson = "{\"traffic\": 5}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    ConfigurationHash = "hash1"
                });
                await c.Backplane.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
                {
                    ExperimentName = "test-exp",
                    VersionNumber = 2,
                    ConfigurationJson = "{\"traffic\": 10}",
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1),
                    ConfigurationHash = "hash2"
                });
                return c;
            }))
            .When("all versions are retrieved", c => Task.Run(async () =>
            {
                var versions = await c.Backplane.GetAllConfigurationVersionsAsync("test-exp");
                return (c, versions);
            }))
            .Then("two versions are returned", r => r.versions.Count.Should().Be(2))
            .And("versions are in order", r =>
            {
                r.versions[0].VersionNumber.Should().Be(1);
                r.versions[1].VersionNumber.Should().Be(2);
            })
            .AssertPassed();

    [Scenario("Retrieve latest configuration version")]
    [Fact]
    public Task Retrieve_latest_version()
        => Given("a backplane", CreateBackplane)
            .And("multiple versions appended", c => Task.Run(async () =>
            {
                await c.Backplane.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
                {
                    ExperimentName = "test-exp",
                    VersionNumber = 1,
                    ConfigurationJson = "{\"traffic\": 5}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    ConfigurationHash = "hash1"
                });
                await c.Backplane.AppendConfigurationVersionAsync(new PersistedConfigurationVersion
                {
                    ExperimentName = "test-exp",
                    VersionNumber = 2,
                    ConfigurationJson = "{\"traffic\": 10}",
                    CreatedAt = DateTimeOffset.UtcNow.AddMinutes(1),
                    ConfigurationHash = "hash2"
                });
                return c;
            }))
            .When("latest version is retrieved", c => Task.Run(async () =>
            {
                var latest = await c.Backplane.GetLatestConfigurationVersionAsync("test-exp");
                return (c, latest);
            }))
            .Then("latest version is returned", r => r.latest.Should().NotBeNull())
            .And("version number is 2", r => r.latest!.VersionNumber.Should().Be(2))
            .AssertPassed();

    [Scenario("Support multi-tenancy with tenant scoping")]
    [Fact]
    public Task Support_multitenancy()
        => Given("a backplane", CreateBackplane)
            .And("states for different tenants saved", c => Task.Run(async () =>
            {
                await c.Backplane.SaveExperimentStateAsync(new PersistedExperimentState
                {
                    ExperimentName = "exp1",
                    CurrentState = ExperimentLifecycleState.Draft,
                    ConfigurationVersion = 1,
                    LastModified = DateTimeOffset.UtcNow,
                    ETag = "etag1",
                    TenantId = "tenant-a"
                });
                await c.Backplane.SaveExperimentStateAsync(new PersistedExperimentState
                {
                    ExperimentName = "exp1",
                    CurrentState = ExperimentLifecycleState.Running,
                    ConfigurationVersion = 1,
                    LastModified = DateTimeOffset.UtcNow,
                    ETag = "etag2",
                    TenantId = "tenant-b"
                });
                return c;
            }))
            .When("state for tenant-a is retrieved", c => Task.Run(async () =>
            {
                var state = await c.Backplane.GetExperimentStateAsync("exp1", tenantId: "tenant-a");
                return c with { State = state };
            }))
            .Then("correct tenant state is returned", c => c.State.Should().NotBeNull())
            .And("state is for tenant-a", c => c.State!.TenantId.Should().Be("tenant-a"))
            .And("state is Draft", c => c.State!.CurrentState.Should().Be(ExperimentLifecycleState.Draft))
            .AssertPassed();
}
