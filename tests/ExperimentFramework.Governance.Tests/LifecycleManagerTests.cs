using ExperimentFramework.Audit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ExperimentFramework.Governance.Tests;

public class LifecycleManagerTests
{
    private readonly ILogger<LifecycleManager> _logger;
    private readonly IAuditSink _auditSink;
    private readonly LifecycleManager _sut;

    public LifecycleManagerTests()
    {
        _logger = Substitute.For<ILogger<LifecycleManager>>();
        _auditSink = Substitute.For<IAuditSink>();
        _sut = new LifecycleManager(_logger, _auditSink);
    }

    [Fact]
    public void GetState_ReturnsNull_WhenExperimentNotTracked()
    {
        // Act
        var state = _sut.GetState("non-existent");

        // Assert
        state.Should().BeNull();
    }

    [Fact]
    public async Task TransitionAsync_TransitionsFromDraftToPendingApproval_Successfully()
    {
        // Arrange
        var experimentName = "test-experiment";

        // Act
        await _sut.TransitionAsync(experimentName, ExperimentLifecycleState.PendingApproval, "user1", "Ready for review");

        // Assert
        var state = _sut.GetState(experimentName);
        state.Should().Be(ExperimentLifecycleState.PendingApproval);
    }

    [Fact]
    public async Task TransitionAsync_RecordsAuditEvent()
    {
        // Arrange
        var experimentName = "test-experiment";

        // Act
        await _sut.TransitionAsync(experimentName, ExperimentLifecycleState.PendingApproval, "user1", "Ready for review");

        // Assert
        await _auditSink.Received(1).RecordAsync(
            Arg.Is<AuditEvent>(e =>
                e.ExperimentName == experimentName &&
                e.EventType == AuditEventType.ExperimentModified &&
                e.Actor == "user1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TransitionAsync_ThrowsException_WhenTransitionNotAllowed()
    {
        // Arrange
        var experimentName = "test-experiment";
        await _sut.TransitionAsync(experimentName, ExperimentLifecycleState.Archived);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _sut.TransitionAsync(experimentName, ExperimentLifecycleState.Running));
    }

    [Fact]
    public void CanTransition_ReturnsTrue_ForValidTransition()
    {
        // Arrange
        var experimentName = "test-experiment";

        // Act
        var canTransition = _sut.CanTransition(experimentName, ExperimentLifecycleState.PendingApproval);

        // Assert
        canTransition.Should().BeTrue();
    }

    [Fact]
    public async Task CanTransition_ReturnsFalse_ForInvalidTransition()
    {
        // Arrange
        var experimentName = "test-experiment";
        await _sut.TransitionAsync(experimentName, ExperimentLifecycleState.Archived);

        // Act
        var canTransition = _sut.CanTransition(experimentName, ExperimentLifecycleState.Running);

        // Assert
        canTransition.Should().BeFalse();
    }

    [Fact]
    public async Task GetHistory_ReturnsAllTransitions_InChronologicalOrder()
    {
        // Arrange
        var experimentName = "test-experiment";
        await _sut.TransitionAsync(experimentName, ExperimentLifecycleState.PendingApproval, "user1");
        await _sut.TransitionAsync(experimentName, ExperimentLifecycleState.Approved, "user2");
        await _sut.TransitionAsync(experimentName, ExperimentLifecycleState.Running, "user3");

        // Act
        var history = _sut.GetHistory(experimentName);

        // Assert
        history.Should().HaveCount(3);
        history[0].ToState.Should().Be(ExperimentLifecycleState.PendingApproval);
        history[1].ToState.Should().Be(ExperimentLifecycleState.Approved);
        history[2].ToState.Should().Be(ExperimentLifecycleState.Running);
    }

    [Fact]
    public async Task GetAllowedTransitions_ReturnsCorrectStates()
    {
        // Arrange
        var experimentName = "test-experiment";
        await _sut.TransitionAsync(experimentName, ExperimentLifecycleState.PendingApproval);
        await _sut.TransitionAsync(experimentName, ExperimentLifecycleState.Approved);

        // Act
        var allowed = _sut.GetAllowedTransitions(experimentName);

        // Assert
        allowed.Should().Contain(ExperimentLifecycleState.Running);
        allowed.Should().Contain(ExperimentLifecycleState.Ramping);
        allowed.Should().Contain(ExperimentLifecycleState.Archived);
    }

    [Theory]
    [InlineData(ExperimentLifecycleState.Draft, ExperimentLifecycleState.PendingApproval)]
    [InlineData(ExperimentLifecycleState.PendingApproval, ExperimentLifecycleState.Approved)]
    [InlineData(ExperimentLifecycleState.Approved, ExperimentLifecycleState.Running)]
    [InlineData(ExperimentLifecycleState.Running, ExperimentLifecycleState.Paused)]
    [InlineData(ExperimentLifecycleState.Paused, ExperimentLifecycleState.Running)]
    public async Task TransitionAsync_SupportsCommonWorkflows(
        ExperimentLifecycleState fromState,
        ExperimentLifecycleState toState)
    {
        // Arrange
        var experimentName = "test-experiment";
        if (fromState != ExperimentLifecycleState.Draft)
        {
            // Get to the starting state first
            var path = GetPathToState(fromState);
            foreach (var state in path)
            {
                await _sut.TransitionAsync(experimentName, state);
            }
        }

        // Act
        await _sut.TransitionAsync(experimentName, toState);

        // Assert
        var finalState = _sut.GetState(experimentName);
        finalState.Should().Be(toState);
    }

    private static List<ExperimentLifecycleState> GetPathToState(ExperimentLifecycleState targetState)
    {
        return targetState switch
        {
            ExperimentLifecycleState.Draft => new List<ExperimentLifecycleState>(),
            ExperimentLifecycleState.PendingApproval => new() { ExperimentLifecycleState.PendingApproval },
            ExperimentLifecycleState.Approved => new() { ExperimentLifecycleState.PendingApproval, ExperimentLifecycleState.Approved },
            ExperimentLifecycleState.Running => new() { ExperimentLifecycleState.PendingApproval, ExperimentLifecycleState.Approved, ExperimentLifecycleState.Running },
            ExperimentLifecycleState.Paused => new() { ExperimentLifecycleState.PendingApproval, ExperimentLifecycleState.Approved, ExperimentLifecycleState.Running, ExperimentLifecycleState.Paused },
            _ => new List<ExperimentLifecycleState>()
        };
    }
}
