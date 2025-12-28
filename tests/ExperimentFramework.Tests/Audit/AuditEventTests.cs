using ExperimentFramework.Audit;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Audit;

[Feature("AuditEvent represents experiment audit trail entries")]
public sealed class AuditEventTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AuditEvent is created with required properties")]
    [Fact]
    public Task AuditEvent_created_with_required_properties()
        => Given("an audit event", () => new AuditEvent
            {
                EventId = "evt-001",
                Timestamp = DateTimeOffset.UtcNow,
                EventType = AuditEventType.VariantSelected
            })
            .Then("has event ID", evt => evt.EventId == "evt-001")
            .And("has timestamp", evt => evt.Timestamp != default)
            .And("has event type", evt => evt.EventType == AuditEventType.VariantSelected)
            .AssertPassed();

    [Scenario("AuditEvent with full properties")]
    [Fact]
    public Task AuditEvent_with_all_properties()
        => Given("a fully populated audit event", () => new AuditEvent
            {
                EventId = "evt-002",
                Timestamp = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero),
                EventType = AuditEventType.ExperimentCreated,
                ExperimentName = "test-experiment",
                ServiceType = "IPaymentProcessor",
                Actor = "admin@example.com",
                SelectedTrialKey = "variant-a",
                CorrelationId = "corr-123",
                Details = new Dictionary<string, object>
                {
                    ["reason"] = "AB test",
                    ["priority"] = 1
                }
            })
            .Then("has experiment name", evt => evt.ExperimentName == "test-experiment")
            .And("has service type", evt => evt.ServiceType == "IPaymentProcessor")
            .And("has actor", evt => evt.Actor == "admin@example.com")
            .And("has selected trial key", evt => evt.SelectedTrialKey == "variant-a")
            .And("has correlation ID", evt => evt.CorrelationId == "corr-123")
            .And("has details", evt => evt.Details?.Count == 2)
            .AssertPassed();

    [Scenario("All event types are defined")]
    [Fact]
    public Task All_event_types_defined()
        => Given("the event type enum", () => Enum.GetValues<AuditEventType>())
            .Then("contains VariantSelected", types => types.Contains(AuditEventType.VariantSelected))
            .And("contains ExperimentCreated", types => types.Contains(AuditEventType.ExperimentCreated))
            .And("contains ExperimentStarted", types => types.Contains(AuditEventType.ExperimentStarted))
            .And("contains ExperimentStopped", types => types.Contains(AuditEventType.ExperimentStopped))
            .And("contains ExperimentModified", types => types.Contains(AuditEventType.ExperimentModified))
            .And("contains ExperimentDeleted", types => types.Contains(AuditEventType.ExperimentDeleted))
            .And("contains RolloutChanged", types => types.Contains(AuditEventType.RolloutChanged))
            .And("contains Error", types => types.Contains(AuditEventType.Error))
            .And("contains FallbackTriggered", types => types.Contains(AuditEventType.FallbackTriggered))
            .AssertPassed();

    [Scenario("Optional properties default to null")]
    [Fact]
    public Task Optional_properties_default_to_null()
        => Given("a minimal audit event", () => new AuditEvent
            {
                EventId = "evt-003",
                Timestamp = DateTimeOffset.UtcNow,
                EventType = AuditEventType.Error
            })
            .Then("experiment name is null", evt => evt.ExperimentName == null)
            .And("service type is null", evt => evt.ServiceType == null)
            .And("actor is null", evt => evt.Actor == null)
            .And("selected trial key is null", evt => evt.SelectedTrialKey == null)
            .And("correlation ID is null", evt => evt.CorrelationId == null)
            .And("details is null", evt => evt.Details == null)
            .AssertPassed();
}
