using ExperimentFramework.Governance.Persistence.Models;
using ExperimentFramework.Governance.Persistence.Redis;
using ExperimentFramework.Governance.Policy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using StackExchange.Redis;
using Testcontainers.Redis;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Governance.Persistence.Redis.Tests;

[Feature("Redis governance persistence backplane – serialization, key naming, TTL, concurrent writers, failure recovery")]
[Trait("Category", "integration")]
public sealed class RedisGovernancePersistenceBackplaneTests : TinyBddXunitBase, IAsyncLifetime
{
    private readonly RedisContainer _redis;
    private IConnectionMultiplexer? _connection;
    private readonly ILogger<RedisGovernancePersistenceBackplane> _logger = NullLogger<RedisGovernancePersistenceBackplane>.Instance;

    public RedisGovernancePersistenceBackplaneTests(ITestOutputHelper output) : base(output)
    {
        _redis = new RedisBuilder("redis:7-alpine").Build();
    }

    public override async Task InitializeAsync()
    {
        await _redis.StartAsync();
        _connection = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());
    }

    public override async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
        await _redis.DisposeAsync();
    }

    private RedisGovernancePersistenceBackplane CreateBackplane(string? keyPrefix = null)
        => new(_connection!, _logger, keyPrefix ?? "test-governance:");

    private static PersistedExperimentState MakeState(string name,
        ExperimentLifecycleState state = ExperimentLifecycleState.Draft,
        string? tenantId = null)
        => new()
        {
            ExperimentName = name,
            CurrentState = state,
            ConfigurationVersion = 1,
            LastModified = DateTimeOffset.UtcNow,
            LastModifiedBy = "test-user",
            ETag = Guid.NewGuid().ToString(),
            TenantId = tenantId
        };

    // ── serialization round-trip ─────────────────────────────────────────────

    [Scenario("State is serialized and deserialized correctly via Redis")]
    [Fact]
    public async Task State_serializes_and_deserializes()
    {
        var backplane = CreateBackplane();
        var state = MakeState("serialize-test");

        var saveResult = await backplane.SaveExperimentStateAsync(state);
        Assert.True(saveResult.Success);

        var retrieved = await backplane.GetExperimentStateAsync("serialize-test");
        Assert.NotNull(retrieved);
        Assert.Equal("serialize-test", retrieved!.ExperimentName);
        Assert.Equal(ExperimentLifecycleState.Draft, retrieved.CurrentState);
        Assert.Equal(1, retrieved.ConfigurationVersion);
    }

    [Scenario("Transition history serializes event types correctly")]
    [Fact]
    public async Task Transition_history_serializes_event_types()
    {
        var backplane = CreateBackplane();
        await backplane.AppendStateTransitionAsync(new PersistedStateTransition
        {
            TransitionId = "t-ser-1",
            ExperimentName = "transition-ser-exp",
            FromState = ExperimentLifecycleState.Draft,
            ToState = ExperimentLifecycleState.Running,
            Timestamp = DateTimeOffset.UtcNow,
            Actor = "user@test.com"
        });

        var history = await backplane.GetStateTransitionHistoryAsync("transition-ser-exp");
        Assert.Single(history);
        Assert.Equal(ExperimentLifecycleState.Draft, history[0].FromState);
        Assert.Equal(ExperimentLifecycleState.Running, history[0].ToState);
        Assert.Equal("user@test.com", history[0].Actor);
    }

    // ── key naming / isolation ────────────────────────────────────────────────

    [Scenario("Different key prefixes isolate experiment state")]
    [Fact]
    public async Task Different_prefixes_isolate_state()
    {
        var backplane1 = CreateBackplane("prefix-a:");
        var backplane2 = CreateBackplane("prefix-b:");

        var state = MakeState("isolated-exp");
        await backplane1.SaveExperimentStateAsync(state);

        var inA = await backplane1.GetExperimentStateAsync("isolated-exp");
        var inB = await backplane2.GetExperimentStateAsync("isolated-exp");

        Assert.NotNull(inA);
        Assert.Null(inB); // Different prefix, different key
    }

    [Scenario("Transition lists are keyed separately from state")]
    [Fact]
    public async Task Transition_lists_separate_from_state()
    {
        var backplane = CreateBackplane();
        // Save state
        await backplane.SaveExperimentStateAsync(MakeState("key-separation-exp"));

        // Append transition
        await backplane.AppendStateTransitionAsync(new PersistedStateTransition
        {
            TransitionId = "t-ks-1",
            ExperimentName = "key-separation-exp",
            FromState = ExperimentLifecycleState.Draft,
            ToState = ExperimentLifecycleState.PendingApproval,
            Timestamp = DateTimeOffset.UtcNow
        });

        var state = await backplane.GetExperimentStateAsync("key-separation-exp");
        var transitions = await backplane.GetStateTransitionHistoryAsync("key-separation-exp");

        Assert.NotNull(state);
        Assert.Single(transitions);
    }

    // ── optimistic concurrency ────────────────────────────────────────────────

    [Scenario("Save new experiment state succeeds with null ETag")]
    [Fact]
    public async Task Save_new_succeeds_with_null_etag()
    {
        var backplane = CreateBackplane();
        var state = MakeState("new-exp-redis");

        var result = await backplane.SaveExperimentStateAsync(state, expectedETag: null);

        Assert.True(result.Success);
        Assert.False(result.ConflictDetected);
        Assert.NotNull(result.NewETag);
    }

    [Scenario("Update with correct ETag succeeds")]
    [Fact]
    public async Task Update_with_correct_etag_succeeds()
    {
        var backplane = CreateBackplane();
        var state = MakeState("etag-update-exp");

        var first = await backplane.SaveExperimentStateAsync(state, expectedETag: null);
        Assert.True(first.Success);

        var updatedState = new PersistedExperimentState
        {
            ExperimentName = "etag-update-exp",
            CurrentState = ExperimentLifecycleState.Running,
            ConfigurationVersion = 2,
            LastModified = DateTimeOffset.UtcNow,
            ETag = first.NewETag!
        };

        var second = await backplane.SaveExperimentStateAsync(updatedState, expectedETag: first.NewETag);
        Assert.True(second.Success);

        var retrieved = await backplane.GetExperimentStateAsync("etag-update-exp");
        Assert.Equal(ExperimentLifecycleState.Running, retrieved!.CurrentState);
    }

    [Scenario("Update with wrong ETag returns conflict")]
    [Fact]
    public async Task Update_with_wrong_etag_returns_conflict()
    {
        var backplane = CreateBackplane();
        var state = MakeState("conflict-exp-redis");

        var first = await backplane.SaveExperimentStateAsync(state, expectedETag: null);
        Assert.True(first.Success);

        var updatedState = new PersistedExperimentState
        {
            ExperimentName = "conflict-exp-redis",
            CurrentState = ExperimentLifecycleState.Running,
            ConfigurationVersion = 2,
            LastModified = DateTimeOffset.UtcNow,
            ETag = "stale-etag"
        };

        var second = await backplane.SaveExperimentStateAsync(updatedState, expectedETag: "wrong-etag");
        Assert.False(second.Success);
        Assert.True(second.ConflictDetected);
    }

    // ── concurrent writers ────────────────────────────────────────────────────

    [Scenario("Concurrent writers to transition list all entries persist")]
    [Fact]
    public async Task Concurrent_writers_to_transition_list_all_persist()
    {
        var backplane = CreateBackplane();

        var tasks = Enumerable.Range(0, 10).Select(i =>
            backplane.AppendStateTransitionAsync(new PersistedStateTransition
            {
                TransitionId = $"concurrent-t-{i}",
                ExperimentName = "concurrent-transitions-exp",
                FromState = ExperimentLifecycleState.Draft,
                ToState = ExperimentLifecycleState.Running,
                Timestamp = DateTimeOffset.UtcNow,
                Actor = $"user-{i}"
            }));

        await Task.WhenAll(tasks);

        var history = await backplane.GetStateTransitionHistoryAsync("concurrent-transitions-exp");
        Assert.Equal(10, history.Count);
    }

    // ── approval records ─────────────────────────────────────────────────────

    [Scenario("Approval record stored and retrieved by experiment and transition")]
    [Fact]
    public async Task Approval_record_stored_and_retrieved()
    {
        var backplane = CreateBackplane();
        var transitionId = Guid.NewGuid().ToString();

        await backplane.AppendApprovalRecordAsync(new PersistedApprovalRecord
        {
            ApprovalId = Guid.NewGuid().ToString(),
            ExperimentName = "approval-redis-exp",
            TransitionId = transitionId,
            ToState = ExperimentLifecycleState.Approved,
            IsApproved = true,
            Approver = "approver@redis.com",
            Timestamp = DateTimeOffset.UtcNow,
            GateName = "ManualGate"
        });

        var byExperiment = await backplane.GetApprovalRecordsAsync("approval-redis-exp");
        var byTransition = await backplane.GetApprovalRecordsByTransitionAsync(transitionId);

        Assert.Single(byExperiment);
        Assert.Single(byTransition);
        Assert.Equal("approver@redis.com", byExperiment[0].Approver);
    }

    // ── non-existent returns null/empty ───────────────────────────────────────

    [Scenario("Get nonexistent state returns null")]
    [Fact]
    public async Task Get_nonexistent_state_returns_null()
    {
        var backplane = CreateBackplane();
        var result = await backplane.GetExperimentStateAsync("does-not-exist-redis");
        Assert.Null(result);
    }

    [Scenario("Get nonexistent transition history returns empty list")]
    [Fact]
    public async Task Get_nonexistent_transition_history_returns_empty()
    {
        var backplane = CreateBackplane();
        var history = await backplane.GetStateTransitionHistoryAsync("no-history-exp");
        Assert.Empty(history);
    }

    // ── tenant isolation ─────────────────────────────────────────────────────

    [Scenario("Tenant-scoped state is isolated between tenants")]
    [Fact]
    public async Task Tenant_scoped_state_isolated()
    {
        var backplane = CreateBackplane();

        await backplane.SaveExperimentStateAsync(MakeState("tenant-exp", ExperimentLifecycleState.Draft, "tenant-1"));
        await backplane.SaveExperimentStateAsync(MakeState("tenant-exp", ExperimentLifecycleState.Running, "tenant-2"));

        var t1State = await backplane.GetExperimentStateAsync("tenant-exp", tenantId: "tenant-1");
        var t2State = await backplane.GetExperimentStateAsync("tenant-exp", tenantId: "tenant-2");

        Assert.Equal(ExperimentLifecycleState.Draft, t1State!.CurrentState);
        Assert.Equal(ExperimentLifecycleState.Running, t2State!.CurrentState);
    }
}
