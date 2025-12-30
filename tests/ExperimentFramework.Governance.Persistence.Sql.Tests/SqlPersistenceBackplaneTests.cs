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
    private static (GovernanceDbContext, SqlGovernancePersistenceBackplane) CreateBackplane()
    {
        var options = new DbContextOptionsBuilder<GovernanceDbContext>()
            .UseInMemoryDatabase($"GovernanceTest_{Guid.NewGuid()}")
            .Options;

        var dbContext = new GovernanceDbContext(options);
        var logger = Substitute.For<ILogger<SqlGovernancePersistenceBackplane>>();
        var backplane = new SqlGovernancePersistenceBackplane(dbContext, logger);

        return (dbContext, backplane);
    }

    [Scenario("Save new experiment state to SQL database")]
    [Fact]
    public async Task Save_new_experiment_state_sql()
    {
        // Given
        var (dbContext, backplane) = CreateBackplane();
        var state = new PersistedExperimentState
        {
            ExperimentName = "sql-test-exp",
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
        var entity = await dbContext.ExperimentStates
            .FirstOrDefaultAsync(e => e.ExperimentName == "sql-test-exp");
        entity.Should().NotBeNull();
    }

    [Scenario("SQL persistence enforces optimistic concurrency with ETag")]
    [Fact]
    public async Task Sql_optimistic_concurrency()
    {
        // Given
        var (_, backplane) = CreateBackplane();
        var state = new PersistedExperimentState
        {
            ExperimentName = "concurrency-test",
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
            ExperimentName = "concurrency-test",
            CurrentState = ExperimentLifecycleState.Running,
            ConfigurationVersion = 2,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "test-user",
            ETag = Guid.NewGuid().ToString()
        };
        var result = await backplane.SaveExperimentStateAsync(updatedState, expectedETag: "invalid-etag");

        // Then
        result.ConflictDetected.Should().BeTrue();
        var unchangedState = await backplane.GetExperimentStateAsync("concurrency-test");
        unchangedState!.CurrentState.Should().Be(ExperimentLifecycleState.Draft);
    }

    [Scenario("SQL persistence stores immutable state transition history")]
    [Fact]
    public async Task Sql_immutable_transition_history()
    {
        // Given
        var (dbContext, backplane) = CreateBackplane();
        await backplane.AppendStateTransitionAsync(new PersistedStateTransition
        {
            TransitionId = "t1",
            ExperimentName = "history-test",
            FromState = ExperimentLifecycleState.Draft,
            ToState = ExperimentLifecycleState.PendingApproval,
            Timestamp = DateTimeOffset.UtcNow,
            Actor = "user1"
        });
        await backplane.AppendStateTransitionAsync(new PersistedStateTransition
        {
            TransitionId = "t2",
            ExperimentName = "history-test",
            FromState = ExperimentLifecycleState.PendingApproval,
            ToState = ExperimentLifecycleState.Approved,
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(1),
            Actor = "user2"
        });

        // When
        var history = await backplane.GetStateTransitionHistoryAsync("history-test");

        // Then
        history.Count.Should().Be(2);
        var count = await dbContext.StateTransitions
            .CountAsync(t => t.ExperimentName == "history-test");
        count.Should().Be(2);
    }
}
