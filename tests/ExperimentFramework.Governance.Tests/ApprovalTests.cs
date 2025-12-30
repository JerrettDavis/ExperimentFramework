using ExperimentFramework.Governance.Approval;
using FluentAssertions;
using Xunit;

namespace ExperimentFramework.Governance.Tests;

public class ApprovalManagerTests
{
    private readonly ApprovalManager _sut;

    public ApprovalManagerTests()
    {
        _sut = new ApprovalManager();
    }

    [Fact]
    public async Task EvaluateAsync_ReturnsEmpty_WhenNoGatesRegistered()
    {
        // Arrange
        var context = new ApprovalContext
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.PendingApproval
        };

        // Act
        var results = await _sut.EvaluateAsync(context);

        // Assert
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateAsync_EvaluatesMatchingGate()
    {
        // Arrange
        var gate = new AutomaticApprovalGate();
        _sut.RegisterGate(null, ExperimentLifecycleState.PendingApproval, gate);

        var context = new ApprovalContext
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.PendingApproval
        };

        // Act
        var results = await _sut.EvaluateAsync(context);

        // Assert
        results.Should().HaveCount(1);
        results[0].IsApproved.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluateAsync_EvaluatesMultipleGates()
    {
        // Arrange
        _sut.RegisterGate(null, ExperimentLifecycleState.Running, new AutomaticApprovalGate());
        _sut.RegisterGate(null, ExperimentLifecycleState.Running, new RoleBasedApprovalGate("admin"));

        var context = new ApprovalContext
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Approved,
            TargetState = ExperimentLifecycleState.Running
        };

        // Act
        var results = await _sut.EvaluateAsync(context);

        // Assert
        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task IsApprovedAsync_ReturnsTrue_WhenAllGatesApprove()
    {
        // Arrange
        _sut.RegisterGate(null, ExperimentLifecycleState.Running, new AutomaticApprovalGate());

        var context = new ApprovalContext
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Approved,
            TargetState = ExperimentLifecycleState.Running
        };

        // Act
        var isApproved = await _sut.IsApprovedAsync(context);

        // Assert
        isApproved.Should().BeTrue();
    }

    [Fact]
    public async Task IsApprovedAsync_ReturnsFalse_WhenAnyGateRejects()
    {
        // Arrange
        _sut.RegisterGate(null, ExperimentLifecycleState.Running, new AutomaticApprovalGate());
        _sut.RegisterGate(null, ExperimentLifecycleState.Running, new RoleBasedApprovalGate("admin"));

        var context = new ApprovalContext
        {
            ExperimentName = "test-experiment",
            CurrentState = ExperimentLifecycleState.Approved,
            TargetState = ExperimentLifecycleState.Running,
            Actor = "user1"
        };

        // Act
        var isApproved = await _sut.IsApprovedAsync(context);

        // Assert
        isApproved.Should().BeFalse(); // RoleBasedGate will reject
    }
}

public class ApprovalGateTests
{
    [Fact]
    public async Task AutomaticApprovalGate_AlwaysApproves()
    {
        // Arrange
        var gate = new AutomaticApprovalGate();
        var context = new ApprovalContext
        {
            ExperimentName = "test",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.Approved
        };

        // Act
        var result = await gate.EvaluateAsync(context);

        // Assert
        result.IsApproved.Should().BeTrue();
        result.Approver.Should().Be("system");
    }

    [Fact]
    public async Task ManualApprovalGate_ReturnsPending_WhenNoApprovalRecorded()
    {
        // Arrange
        var gate = new ManualApprovalGate();
        var context = new ApprovalContext
        {
            ExperimentName = "test",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.Approved
        };

        // Act
        var result = await gate.EvaluateAsync(context);

        // Assert
        result.IsApproved.Should().BeFalse();
        result.Reason.Should().Contain("Manual approval required");
    }

    [Fact]
    public async Task ManualApprovalGate_ReturnsRecorded_WhenApprovalRecorded()
    {
        // Arrange
        var gate = new ManualApprovalGate();
        gate.RecordApproval("test", ExperimentLifecycleState.Approved, ApprovalResult.Approved("approver1", "Looks good"));

        var context = new ApprovalContext
        {
            ExperimentName = "test",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.Approved
        };

        // Act
        var result = await gate.EvaluateAsync(context);

        // Assert
        result.IsApproved.Should().BeTrue();
        result.Approver.Should().Be("approver1");
        result.Reason.Should().Be("Looks good");
    }

    [Fact]
    public async Task RoleBasedApprovalGate_Approves_WhenActorHasRole()
    {
        // Arrange
        var gate = new RoleBasedApprovalGate("admin", "operator");
        var context = new ApprovalContext
        {
            ExperimentName = "test",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.Approved,
            Actor = "user1",
            Metadata = new Dictionary<string, object> { ["actorRole"] = "admin" }
        };

        // Act
        var result = await gate.EvaluateAsync(context);

        // Assert
        result.IsApproved.Should().BeTrue();
        result.Reason.Should().Contain("admin");
    }

    [Fact]
    public async Task RoleBasedApprovalGate_Rejects_WhenActorLacksRole()
    {
        // Arrange
        var gate = new RoleBasedApprovalGate("admin");
        var context = new ApprovalContext
        {
            ExperimentName = "test",
            CurrentState = ExperimentLifecycleState.Draft,
            TargetState = ExperimentLifecycleState.Approved,
            Actor = "user1",
            Metadata = new Dictionary<string, object> { ["actorRole"] = "developer" }
        };

        // Act
        var result = await gate.EvaluateAsync(context);

        // Assert
        result.IsApproved.Should().BeFalse();
        result.Reason.Should().Contain("Insufficient role privileges");
    }
}
