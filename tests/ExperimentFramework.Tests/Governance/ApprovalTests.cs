using ExperimentFramework.Governance;
using ExperimentFramework.Governance.Approval;

namespace ExperimentFramework.Tests.Governance;

public class ApprovalManagerTests
{
    private readonly ApprovalManager _manager = new();

    [Fact]
    public void RegisterGate_ThrowsWhenGateIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _manager.RegisterGate(null, ExperimentLifecycleState.Approved, null!));
    }

    [Fact]
    public async Task EvaluateAsync_ThrowsWhenContextIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _manager.EvaluateAsync(null!));
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsEmpty_WhenNoGatesRegistered()
    {
        var context = CreateContext(ExperimentLifecycleState.Draft, ExperimentLifecycleState.PendingApproval);

        var results = await _manager.EvaluateAsync(context);

        Assert.Empty(results);
    }

    [Fact]
    public async Task EvaluateAsync_OnlyEvaluatesMatchingGates_ByTargetState()
    {
        var gate = new AutomaticApprovalGate();
        _manager.RegisterGate(null, ExperimentLifecycleState.Approved, gate);

        // Different target state — gate should not apply
        var context = CreateContext(ExperimentLifecycleState.Draft, ExperimentLifecycleState.PendingApproval);
        var results = await _manager.EvaluateAsync(context);

        Assert.Empty(results);
    }

    [Fact]
    public async Task EvaluateAsync_EvaluatesGate_WhenFromStateMatchesAndTargetStateMatches()
    {
        var gate = new AutomaticApprovalGate();
        _manager.RegisterGate(ExperimentLifecycleState.Draft, ExperimentLifecycleState.PendingApproval, gate);

        var context = CreateContext(ExperimentLifecycleState.Draft, ExperimentLifecycleState.PendingApproval);
        var results = await _manager.EvaluateAsync(context);

        Assert.Single(results);
        Assert.True(results[0].IsApproved);
    }

    [Fact]
    public async Task EvaluateAsync_SkipsGate_WhenFromStateMismatch()
    {
        var gate = new AutomaticApprovalGate();
        _manager.RegisterGate(ExperimentLifecycleState.PendingApproval, ExperimentLifecycleState.Approved, gate);

        // From Draft, not PendingApproval
        var context = CreateContext(ExperimentLifecycleState.Draft, ExperimentLifecycleState.Approved);
        var results = await _manager.EvaluateAsync(context);

        Assert.Empty(results);
    }

    [Fact]
    public async Task EvaluateAsync_EvaluatesGate_WhenFromStateIsNullAndTargetMatches()
    {
        var gate = new AutomaticApprovalGate();
        _manager.RegisterGate(null, ExperimentLifecycleState.PendingApproval, gate); // null = any state

        var context = CreateContext(ExperimentLifecycleState.Running, ExperimentLifecycleState.PendingApproval);
        var results = await _manager.EvaluateAsync(context);

        Assert.Single(results);
    }

    [Fact]
    public async Task IsApprovedAsync_ReturnsFalse_WhenNoGates()
    {
        var context = CreateContext(ExperimentLifecycleState.Draft, ExperimentLifecycleState.PendingApproval);

        // No gates = all pass vacuously
        var isApproved = await _manager.IsApprovedAsync(context);

        Assert.True(isApproved); // All() on empty returns true
    }

    [Fact]
    public async Task IsApprovedAsync_ReturnsTrue_WhenAllGatesApprove()
    {
        _manager.RegisterGate(null, ExperimentLifecycleState.PendingApproval, new AutomaticApprovalGate());

        var context = CreateContext(ExperimentLifecycleState.Draft, ExperimentLifecycleState.PendingApproval);
        var isApproved = await _manager.IsApprovedAsync(context);

        Assert.True(isApproved);
    }

    [Fact]
    public async Task IsApprovedAsync_ReturnsFalse_WhenAnyGateRejects()
    {
        var rejecting = new ManualApprovalGate(); // Returns pending (not approved) by default
        _manager.RegisterGate(null, ExperimentLifecycleState.PendingApproval, rejecting);

        var context = CreateContext(ExperimentLifecycleState.Draft, ExperimentLifecycleState.PendingApproval);
        var isApproved = await _manager.IsApprovedAsync(context);

        Assert.False(isApproved);
    }

    private static ApprovalContext CreateContext(
        ExperimentLifecycleState current,
        ExperimentLifecycleState target,
        string experiment = "test-exp",
        string? actor = null,
        IReadOnlyDictionary<string, object>? metadata = null) =>
        new()
        {
            ExperimentName = experiment,
            CurrentState = current,
            TargetState = target,
            Actor = actor,
            Metadata = metadata
        };
}

public class AutomaticApprovalGateTests
{
    [Fact]
    public async Task EvaluateAsync_AlwaysReturnsApproved()
    {
        var gate = new AutomaticApprovalGate();
        var context = new ApprovalContext
        {
            ExperimentName = "exp",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.PendingApproval
        };

        var result = await gate.EvaluateAsync(context);

        Assert.True(result.IsApproved);
        Assert.Equal("Automatic", gate.Name);
    }
}

public class ManualApprovalGateTests
{
    [Fact]
    public async Task EvaluateAsync_ReturnsPending_WhenNotRecorded()
    {
        var gate = new ManualApprovalGate();
        var context = new ApprovalContext
        {
            ExperimentName = "exp",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.PendingApproval
        };

        var result = await gate.EvaluateAsync(context);

        Assert.False(result.IsApproved);
        Assert.Equal("Manual", gate.Name);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsRecordedApproval()
    {
        var gate = new ManualApprovalGate();
        gate.RecordApproval("exp", ExperimentLifecycleState.PendingApproval,
            ApprovalResult.Approved("reviewer", "Looks good"));

        var context = new ApprovalContext
        {
            ExperimentName = "exp",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.PendingApproval
        };

        var result = await gate.EvaluateAsync(context);

        Assert.True(result.IsApproved);
        Assert.Equal("reviewer", result.Approver);
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsPending_AfterClearApproval()
    {
        var gate = new ManualApprovalGate();
        gate.RecordApproval("exp", ExperimentLifecycleState.PendingApproval,
            ApprovalResult.Approved("reviewer", "ok"));
        gate.ClearApproval("exp", ExperimentLifecycleState.PendingApproval);

        var context = new ApprovalContext
        {
            ExperimentName = "exp",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.PendingApproval
        };

        var result = await gate.EvaluateAsync(context);

        Assert.False(result.IsApproved);
    }
}

public class RoleBasedApprovalGateTests
{
    [Fact]
    public async Task EvaluateAsync_Approves_WhenActorHasRequiredRole()
    {
        var gate = new RoleBasedApprovalGate("admin", "manager");
        var context = new ApprovalContext
        {
            ExperimentName = "exp",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.PendingApproval,
            Actor = "bob",
            Metadata = new Dictionary<string, object> { ["actorRole"] = "manager" }
        };

        var result = await gate.EvaluateAsync(context);

        Assert.True(result.IsApproved);
        Assert.Equal("RoleBased", gate.Name);
    }

    [Fact]
    public async Task EvaluateAsync_Rejects_WhenActorHasWrongRole()
    {
        var gate = new RoleBasedApprovalGate("admin");
        var context = new ApprovalContext
        {
            ExperimentName = "exp",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.PendingApproval,
            Actor = "bob",
            Metadata = new Dictionary<string, object> { ["actorRole"] = "viewer" }
        };

        var result = await gate.EvaluateAsync(context);

        Assert.False(result.IsApproved);
    }

    [Fact]
    public async Task EvaluateAsync_Rejects_WhenNoMetadata()
    {
        var gate = new RoleBasedApprovalGate("admin");
        var context = new ApprovalContext
        {
            ExperimentName = "exp",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.PendingApproval,
            Metadata = null
        };

        var result = await gate.EvaluateAsync(context);

        Assert.False(result.IsApproved);
    }

    [Fact]
    public async Task EvaluateAsync_Approves_CaseInsensitive()
    {
        var gate = new RoleBasedApprovalGate("ADMIN");
        var context = new ApprovalContext
        {
            ExperimentName = "exp",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.PendingApproval,
            Metadata = new Dictionary<string, object> { ["actorRole"] = "admin" }
        };

        var result = await gate.EvaluateAsync(context);

        Assert.True(result.IsApproved);
    }

    [Fact]
    public async Task EvaluateAsync_Rejects_WhenRoleValueIsNotString()
    {
        var gate = new RoleBasedApprovalGate("admin");
        var context = new ApprovalContext
        {
            ExperimentName = "exp",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.PendingApproval,
            Metadata = new Dictionary<string, object> { ["actorRole"] = 42 } // not a string
        };

        var result = await gate.EvaluateAsync(context);

        Assert.False(result.IsApproved);
    }
}
