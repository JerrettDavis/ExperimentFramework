using ExperimentFramework.DataPlane.Abstractions;
using ExperimentFramework.DataPlane.Abstractions.Events;
using ExperimentFramework.DataPlane.Implementations;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ExperimentFramework.DataPlane.Tests;

public class OpenTelemetryDataBackplaneTests
{
    [Fact]
    public async Task PublishAsync_ExposureEvent_DoesNotThrow()
    {
        // Arrange
        var backplane = new OpenTelemetryDataBackplane(NullLogger<OpenTelemetryDataBackplane>.Instance);
        var exposureEvent = new ExposureEvent
        {
            ExperimentName = "TestExperiment",
            VariantKey = "control",
            SubjectId = "user-123",
            SubjectType = "user",
            Timestamp = DateTimeOffset.UtcNow,
            SelectionReason = "trial-selection",
            AssignmentPolicy = AssignmentPolicy.BestEffort,
            IsRepeatExposure = false
        };
        var envelope = new DataPlaneEnvelope
        {
            EventId = "test-1",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = DataPlaneEventType.Exposure,
            SchemaVersion = ExposureEvent.SchemaVersion,
            Payload = exposureEvent
        };

        // Act & Assert
        await backplane.PublishAsync(envelope);
    }

    [Fact]
    public async Task PublishAsync_AssignmentEvent_DoesNotThrow()
    {
        // Arrange
        var backplane = new OpenTelemetryDataBackplane(NullLogger<OpenTelemetryDataBackplane>.Instance);
        var assignmentEvent = new AssignmentEvent
        {
            ExperimentName = "TestExperiment",
            SubjectId = "user-123",
            PreviousVariantKey = "control",
            NewVariantKey = "variant-a",
            Timestamp = DateTimeOffset.UtcNow,
            ChangeReason = "config-change",
            AssignmentPolicy = AssignmentPolicy.SubjectSticky
        };
        var envelope = new DataPlaneEnvelope
        {
            EventId = "test-2",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = DataPlaneEventType.Assignment,
            SchemaVersion = AssignmentEvent.SchemaVersion,
            Payload = assignmentEvent
        };

        // Act & Assert
        await backplane.PublishAsync(envelope);
    }

    [Fact]
    public async Task PublishAsync_AnalysisSignalEvent_DoesNotThrow()
    {
        // Arrange
        var backplane = new OpenTelemetryDataBackplane(NullLogger<OpenTelemetryDataBackplane>.Instance);
        var signalEvent = new AnalysisSignalEvent
        {
            ExperimentName = "TestExperiment",
            SignalType = AnalysisSignalType.SampleRatioMismatch,
            Severity = SignalSeverity.Warning,
            Timestamp = DateTimeOffset.UtcNow,
            Message = "Sample ratio mismatch detected"
        };
        var envelope = new DataPlaneEnvelope
        {
            EventId = "test-3",
            Timestamp = DateTimeOffset.UtcNow,
            EventType = DataPlaneEventType.AnalysisSignal,
            SchemaVersion = AnalysisSignalEvent.SchemaVersion,
            Payload = signalEvent
        };

        // Act & Assert
        await backplane.PublishAsync(envelope);
    }

    [Fact]
    public async Task HealthAsync_ReturnsHealthy()
    {
        // Arrange
        var backplane = new OpenTelemetryDataBackplane(NullLogger<OpenTelemetryDataBackplane>.Instance);

        // Act
        var health = await backplane.HealthAsync();

        // Assert
        health.IsHealthy.Should().BeTrue();
        health.Description.Should().Contain("OpenTelemetry");
    }

    [Fact]
    public async Task FlushAsync_DoesNotThrow()
    {
        // Arrange
        var backplane = new OpenTelemetryDataBackplane(NullLogger<OpenTelemetryDataBackplane>.Instance);

        // Act & Assert
        await backplane.FlushAsync();
    }

    [Fact]
    public async Task PublishAsync_NullEnvelope_DoesNotThrow()
    {
        // Arrange
        var backplane = new OpenTelemetryDataBackplane(NullLogger<OpenTelemetryDataBackplane>.Instance);

        // Act & Assert
        await backplane.PublishAsync(null!);
    }
}
