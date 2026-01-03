using ExperimentFramework.ServiceRegistration;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.ServiceRegistration;

[Feature("Service graph snapshot captures service collection state")]
public class ServiceGraphSnapshotTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Capturing a snapshot from an empty service collection")]
    [Fact]
    public Task Snapshot_captures_empty_collection()
        => Given("an empty service collection", () => new ServiceCollection())
            .When("capturing a snapshot", services => ServiceGraphSnapshot.Capture(services))
            .Then("snapshot should have a valid ID", snapshot => !string.IsNullOrEmpty(snapshot.SnapshotId))
            .And("snapshot should have a timestamp", snapshot => snapshot.Timestamp != default)
            .And("descriptors should be empty", snapshot => snapshot.Descriptors.Count == 0)
            .And("fingerprint should be generated", snapshot => !string.IsNullOrEmpty(snapshot.Fingerprint))
            .AssertPassed();

    [Scenario("Capturing a snapshot from a populated service collection")]
    [Fact]
    public Task Snapshot_captures_populated_collection()
        => Given("a service collection with registrations", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, TestServiceImpl>();
                services.AddScoped<IAnotherService, AnotherServiceImpl>();
                services.AddTransient<IThirdService, ThirdServiceImpl>();
                return services;
            })
            .When("capturing a snapshot", services => ServiceGraphSnapshot.Capture(services))
            .Then("snapshot should contain all descriptors", snapshot => snapshot.Descriptors.Count == 3)
            .And("first descriptor should be singleton", snapshot => snapshot.Descriptors[0].Lifetime == ServiceLifetime.Singleton)
            .And("second descriptor should be scoped", snapshot => snapshot.Descriptors[1].Lifetime == ServiceLifetime.Scoped)
            .And("third descriptor should be transient", snapshot => snapshot.Descriptors[2].Lifetime == ServiceLifetime.Transient)
            .And("snapshot should have a fingerprint", snapshot => !string.IsNullOrEmpty(snapshot.Fingerprint))
            .AssertPassed();

    [Scenario("Each snapshot has a unique ID")]
    [Fact]
    public Task Snapshot_IDs_are_unique()
        => Given("a service collection", () => new ServiceCollection())
            .When("capturing two snapshots", services =>
            {
                var snapshot1 = ServiceGraphSnapshot.Capture(services);
                var snapshot2 = ServiceGraphSnapshot.Capture(services);
                return (snapshot1, snapshot2);
            })
            .Then("snapshot IDs should be different", 
                snapshots => snapshots.snapshot1.SnapshotId != snapshots.snapshot2.SnapshotId)
            .AssertPassed();

    // Test interfaces
    private interface ITestService { }
    private class TestServiceImpl : ITestService { }
    private interface IAnotherService { }
    private class AnotherServiceImpl : IAnotherService { }
    private interface IThirdService { }
    private class ThirdServiceImpl : IThirdService { }
}
