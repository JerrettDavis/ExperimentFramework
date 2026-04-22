using ExperimentFramework.Governance;
using ExperimentFramework.Audit;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ExperimentFramework.Tests.Governance;

public class LifecycleManagerTests
{
    private readonly LifecycleManager _manager = new(NullLogger<LifecycleManager>.Instance);

    // ───────────────────────── GetState ─────────────────────────

    [Fact]
    public void GetState_ThrowsForNullOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => _manager.GetState(null!));
        Assert.Throws<ArgumentException>(() => _manager.GetState("   "));
    }

    [Fact]
    public void GetState_ReturnsNull_ForUnknownExperiment()
    {
        var state = _manager.GetState("unknown-exp");
        Assert.Null(state);
    }

    [Fact]
    public async Task GetState_ReturnsCurrentState_AfterTransition()
    {
        await _manager.TransitionAsync("exp1", ExperimentLifecycleState.PendingApproval);

        var state = _manager.GetState("exp1");

        Assert.Equal(ExperimentLifecycleState.PendingApproval, state);
    }

    // ───────────────────────── GetHistory ─────────────────────────

    [Fact]
    public void GetHistory_ThrowsForNullOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => _manager.GetHistory(null!));
        Assert.Throws<ArgumentException>(() => _manager.GetHistory(""));
    }

    [Fact]
    public void GetHistory_ReturnsEmpty_ForUnknownExperiment()
    {
        var history = _manager.GetHistory("never-seen");
        Assert.Empty(history);
    }

    [Fact]
    public async Task GetHistory_ReturnsAllTransitions()
    {
        await _manager.TransitionAsync("exp2", ExperimentLifecycleState.PendingApproval);
        await _manager.TransitionAsync("exp2", ExperimentLifecycleState.Approved);

        var history = _manager.GetHistory("exp2");

        Assert.Equal(2, history.Count);
        Assert.Equal(ExperimentLifecycleState.PendingApproval, history[0].ToState);
        Assert.Equal(ExperimentLifecycleState.Approved, history[1].ToState);
    }

    // ───────────────────────── CanTransition ─────────────────────────

    [Fact]
    public void CanTransition_ThrowsForNullOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => _manager.CanTransition(null!, ExperimentLifecycleState.PendingApproval));
    }

    [Fact]
    public void CanTransition_ReturnsFalse_ForInvalidTransition()
    {
        // Draft -> Running is not allowed directly
        var canTransition = _manager.CanTransition("new-exp", ExperimentLifecycleState.Running);
        Assert.False(canTransition);
    }

    [Fact]
    public void CanTransition_ReturnsTrue_ForValidTransition()
    {
        // Draft -> PendingApproval is allowed
        var canTransition = _manager.CanTransition("new-exp", ExperimentLifecycleState.PendingApproval);
        Assert.True(canTransition);
    }

    [Fact]
    public async Task CanTransition_ReturnsTrue_ForValidTransitionFromCurrentState()
    {
        await _manager.TransitionAsync("exp3", ExperimentLifecycleState.PendingApproval);

        // PendingApproval -> Approved is allowed
        var canTransition = _manager.CanTransition("exp3", ExperimentLifecycleState.Approved);
        Assert.True(canTransition);
    }

    // ───────────────────────── TransitionAsync ─────────────────────────

    [Fact]
    public async Task TransitionAsync_ThrowsForNullOrWhitespace()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _manager.TransitionAsync("", ExperimentLifecycleState.PendingApproval));
    }

    [Fact]
    public async Task TransitionAsync_ThrowsForInvalidTransition()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.TransitionAsync("exp4", ExperimentLifecycleState.Running)); // Draft -> Running is invalid
    }

    [Fact]
    public async Task TransitionAsync_RecordsActorAndReason()
    {
        await _manager.TransitionAsync("exp5", ExperimentLifecycleState.PendingApproval,
            actor: "alice", reason: "ready for review");

        var history = _manager.GetHistory("exp5");

        Assert.Single(history);
        Assert.Equal("alice", history[0].Actor);
        Assert.Equal("ready for review", history[0].Reason);
    }

    [Fact]
    public async Task TransitionAsync_EmitsAuditEvent()
    {
        var auditSinkMock = new Mock<IAuditSink>();
        auditSinkMock.Setup(s => s.RecordAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.CompletedTask);

        var manager = new LifecycleManager(NullLogger<LifecycleManager>.Instance, auditSinkMock.Object);

        await manager.TransitionAsync("audited-exp", ExperimentLifecycleState.PendingApproval, actor: "system");

        auditSinkMock.Verify(
            s => s.RecordAsync(It.Is<AuditEvent>(e => e.ExperimentName == "audited-exp"), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task TransitionAsync_WorksWithoutAuditSink()
    {
        // No audit sink — should not throw
        await _manager.TransitionAsync("exp6", ExperimentLifecycleState.PendingApproval);

        var state = _manager.GetState("exp6");
        Assert.Equal(ExperimentLifecycleState.PendingApproval, state);
    }

    // ───────────────────────── GetAllowedTransitions ─────────────────────────

    [Fact]
    public void GetAllowedTransitions_ThrowsForNullOrWhitespace()
    {
        Assert.Throws<ArgumentException>(() => _manager.GetAllowedTransitions(""));
    }

    [Fact]
    public void GetAllowedTransitions_ReturnsDraftTransitions_ForNewExperiment()
    {
        var allowed = _manager.GetAllowedTransitions("brand-new");

        // From Draft: can go to PendingApproval or Archived
        Assert.Contains(ExperimentLifecycleState.PendingApproval, allowed);
        Assert.Contains(ExperimentLifecycleState.Archived, allowed);
    }

    [Fact]
    public async Task GetAllowedTransitions_ReturnsCorrectTransitions_AfterStateChange()
    {
        await _manager.TransitionAsync("exp7", ExperimentLifecycleState.PendingApproval);
        await _manager.TransitionAsync("exp7", ExperimentLifecycleState.Approved);
        await _manager.TransitionAsync("exp7", ExperimentLifecycleState.Running);

        var allowed = _manager.GetAllowedTransitions("exp7");

        Assert.Contains(ExperimentLifecycleState.Paused, allowed);
        Assert.Contains(ExperimentLifecycleState.RolledBack, allowed);
    }

    [Fact]
    public async Task GetAllowedTransitions_ReturnsEmpty_ForArchivedExperiment()
    {
        await _manager.TransitionAsync("exp8", ExperimentLifecycleState.Archived);

        var allowed = _manager.GetAllowedTransitions("exp8");

        Assert.Empty(allowed);
    }

    // ───────────────────────── Full lifecycle flow ─────────────────────────

    [Fact]
    public async Task FullLifecycle_DraftThroughToArchived()
    {
        const string name = "full-lifecycle-exp";

        await _manager.TransitionAsync(name, ExperimentLifecycleState.PendingApproval);
        await _manager.TransitionAsync(name, ExperimentLifecycleState.Approved);
        await _manager.TransitionAsync(name, ExperimentLifecycleState.Running);
        await _manager.TransitionAsync(name, ExperimentLifecycleState.Paused);
        await _manager.TransitionAsync(name, ExperimentLifecycleState.RolledBack);
        await _manager.TransitionAsync(name, ExperimentLifecycleState.Archived);

        var state = _manager.GetState(name);
        Assert.Equal(ExperimentLifecycleState.Archived, state);

        var history = _manager.GetHistory(name);
        Assert.Equal(6, history.Count);
    }
}
