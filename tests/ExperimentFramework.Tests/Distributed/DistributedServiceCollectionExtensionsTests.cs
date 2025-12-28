using ExperimentFramework.Distributed;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Distributed;

[Feature("ServiceCollectionExtensions registers distributed services")]
public sealed class DistributedServiceCollectionExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AddExperimentDistributedInMemory registers state and lock provider")]
    [Fact]
    public async Task AddExperimentDistributedInMemory_registers_services()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedInMemory();
        var sp = services.BuildServiceProvider();

        var state = sp.GetService<IDistributedExperimentState>();
        var lockProvider = sp.GetService<IDistributedLockProvider>();

        Assert.NotNull(state);
        Assert.NotNull(lockProvider);
        Assert.IsType<InMemoryDistributedState>(state);
        Assert.IsType<InMemoryDistributedLockProvider>(lockProvider);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentDistributedInMemory returns service collection for chaining")]
    [Fact]
    public Task AddExperimentDistributedInMemory_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding in-memory distributed", services => services.AddExperimentDistributedInMemory())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentDistributedInMemory is idempotent")]
    [Fact]
    public async Task AddExperimentDistributedInMemory_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedInMemory();
        services.AddExperimentDistributedInMemory();
        var sp = services.BuildServiceProvider();

        var states = sp.GetServices<IDistributedExperimentState>().ToList();
        var lockProviders = sp.GetServices<IDistributedLockProvider>().ToList();

        Assert.Single(states);
        Assert.Single(lockProviders);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentDistributedState registers custom state implementation")]
    [Fact]
    public async Task AddExperimentDistributedState_registers_custom_state()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedState<CustomDistributedState>();
        var sp = services.BuildServiceProvider();

        var state = sp.GetService<IDistributedExperimentState>();

        Assert.NotNull(state);
        Assert.IsType<CustomDistributedState>(state);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentDistributedState returns service collection for chaining")]
    [Fact]
    public Task AddExperimentDistributedState_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding custom state", services => services.AddExperimentDistributedState<CustomDistributedState>())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentDistributedState is idempotent")]
    [Fact]
    public async Task AddExperimentDistributedState_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedState<CustomDistributedState>();
        services.AddExperimentDistributedState<InMemoryDistributedState>();
        var sp = services.BuildServiceProvider();

        var state = sp.GetService<IDistributedExperimentState>();

        // TryAddSingleton keeps first registration
        Assert.IsType<CustomDistributedState>(state);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentDistributedLocking registers custom lock provider")]
    [Fact]
    public async Task AddExperimentDistributedLocking_registers_custom_provider()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedLocking<CustomLockProvider>();
        var sp = services.BuildServiceProvider();

        var lockProvider = sp.GetService<IDistributedLockProvider>();

        Assert.NotNull(lockProvider);
        Assert.IsType<CustomLockProvider>(lockProvider);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentDistributedLocking returns service collection for chaining")]
    [Fact]
    public Task AddExperimentDistributedLocking_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding custom locking", services => services.AddExperimentDistributedLocking<CustomLockProvider>())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentDistributedLocking is idempotent")]
    [Fact]
    public async Task AddExperimentDistributedLocking_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedLocking<CustomLockProvider>();
        services.AddExperimentDistributedLocking<InMemoryDistributedLockProvider>();
        var sp = services.BuildServiceProvider();

        var lockProvider = sp.GetService<IDistributedLockProvider>();

        // TryAddSingleton keeps first registration
        Assert.IsType<CustomLockProvider>(lockProvider);

        await Task.CompletedTask;
    }

    [Scenario("Services are registered as singletons")]
    [Fact]
    public async Task Services_are_singletons()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedInMemory();
        var sp = services.BuildServiceProvider();

        var state1 = sp.GetService<IDistributedExperimentState>();
        var state2 = sp.GetService<IDistributedExperimentState>();
        var lock1 = sp.GetService<IDistributedLockProvider>();
        var lock2 = sp.GetService<IDistributedLockProvider>();

        Assert.Same(state1, state2);
        Assert.Same(lock1, lock2);

        await Task.CompletedTask;
    }

    [Scenario("Custom implementations can be combined")]
    [Fact]
    public async Task Custom_implementations_can_be_combined()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedState<CustomDistributedState>();
        services.AddExperimentDistributedLocking<CustomLockProvider>();
        var sp = services.BuildServiceProvider();

        var state = sp.GetService<IDistributedExperimentState>();
        var lockProvider = sp.GetService<IDistributedLockProvider>();

        Assert.IsType<CustomDistributedState>(state);
        Assert.IsType<CustomLockProvider>(lockProvider);

        await Task.CompletedTask;
    }

    private sealed class CustomDistributedState : IDistributedExperimentState
    {
        public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<T?>(default);
        public ValueTask SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
        public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
        public ValueTask<long> IncrementAsync(string key, long delta = 1, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(delta);
    }

    private sealed class CustomLockProvider : IDistributedLockProvider
    {
        public ValueTask<IDistributedLockHandle?> TryAcquireAsync(
            string lockName,
            TimeSpan expiration,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IDistributedLockHandle?>(null);

        public ValueTask<IDistributedLockHandle?> AcquireAsync(
            string lockName,
            TimeSpan expiration,
            TimeSpan timeout,
            CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IDistributedLockHandle?>(null);
    }
}
