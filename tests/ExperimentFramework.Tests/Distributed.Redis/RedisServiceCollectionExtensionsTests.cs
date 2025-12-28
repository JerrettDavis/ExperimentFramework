using ExperimentFramework.Distributed;
using ExperimentFramework.Distributed.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Testcontainers.Redis;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Distributed.Redis;

[Feature("ServiceCollectionExtensions register Redis distributed services")]
public sealed class RedisServiceCollectionExtensionsTests : TinyBddXunitBase, IAsyncLifetime
{
    private readonly RedisContainer _redis;

    public RedisServiceCollectionExtensionsTests(ITestOutputHelper output) : base(output)
    {
        _redis = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _redis.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _redis.DisposeAsync();
    }

    [Scenario("AddExperimentDistributedRedis registers all services with connection string")]
    [Fact]
    public async Task Registers_all_services_with_connection_string()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedRedis(_redis.GetConnectionString());
        var sp = services.BuildServiceProvider();

        var connection = sp.GetService<IConnectionMultiplexer>();
        var state = sp.GetService<IDistributedExperimentState>();
        var lockProvider = sp.GetService<IDistributedLockProvider>();

        Assert.NotNull(connection);
        Assert.NotNull(state);
        Assert.NotNull(lockProvider);
        Assert.IsType<RedisDistributedState>(state);
        Assert.IsType<RedisDistributedLockProvider>(lockProvider);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentDistributedRedis applies configuration")]
    [Fact]
    public async Task Applies_configuration()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedRedis(_redis.GetConnectionString(), opts =>
        {
            opts.KeyPrefix = "custom:prefix:";
        });
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<RedisDistributedStateOptions>();

        Assert.NotNull(options);
        Assert.Equal("custom:prefix:", options.KeyPrefix);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentDistributedRedis works with existing connection")]
    [Fact]
    public async Task Works_with_existing_connection()
    {
        var connection = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());

        var services = new ServiceCollection();
        services.AddSingleton<IConnectionMultiplexer>(connection);
        services.AddExperimentDistributedRedis(opts =>
        {
            opts.KeyPrefix = "existing:";
        });
        var sp = services.BuildServiceProvider();

        var state = sp.GetService<IDistributedExperimentState>();
        var lockProvider = sp.GetService<IDistributedLockProvider>();

        Assert.NotNull(state);
        Assert.NotNull(lockProvider);

        await connection.CloseAsync();
        connection.Dispose();
    }

    [Scenario("AddExperimentDistributedRedis returns service collection for chaining")]
    [Fact]
    public Task Returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding redis distributed", services =>
                services.AddExperimentDistributedRedis(_redis.GetConnectionString()))
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("Services are registered as singletons")]
    [Fact]
    public async Task Services_are_singletons()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedRedis(_redis.GetConnectionString());
        var sp = services.BuildServiceProvider();

        var state1 = sp.GetService<IDistributedExperimentState>();
        var state2 = sp.GetService<IDistributedExperimentState>();
        var lock1 = sp.GetService<IDistributedLockProvider>();
        var lock2 = sp.GetService<IDistributedLockProvider>();

        Assert.Same(state1, state2);
        Assert.Same(lock1, lock2);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentDistributedRedis without configure uses defaults")]
    [Fact]
    public async Task Without_configure_uses_defaults()
    {
        var services = new ServiceCollection();
        services.AddExperimentDistributedRedis(_redis.GetConnectionString());
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<RedisDistributedStateOptions>();

        Assert.NotNull(options);
        Assert.Equal("experiment:state:", options.KeyPrefix);

        await Task.CompletedTask;
    }

    [Scenario("Existing connection overload without configure uses defaults")]
    [Fact]
    public async Task Existing_connection_without_configure_uses_defaults()
    {
        var connection = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());

        var services = new ServiceCollection();
        services.AddSingleton<IConnectionMultiplexer>(connection);
        services.AddExperimentDistributedRedis();
        var sp = services.BuildServiceProvider();

        var options = sp.GetService<RedisDistributedStateOptions>();

        Assert.NotNull(options);
        Assert.Equal("experiment:state:", options.KeyPrefix);

        await connection.CloseAsync();
        connection.Dispose();
    }

    [Scenario("TryAdd semantics prevent duplicate registrations")]
    [Fact]
    public async Task Prevents_duplicate_registrations()
    {
        var connection = await ConnectionMultiplexer.ConnectAsync(_redis.GetConnectionString());

        var services = new ServiceCollection();
        services.AddSingleton<IConnectionMultiplexer>(connection);
        services.AddExperimentDistributedRedis(opts => opts.KeyPrefix = "first:");
        services.AddExperimentDistributedRedis(opts => opts.KeyPrefix = "second:"); // Should not override

        var sp = services.BuildServiceProvider();
        var options = sp.GetService<RedisDistributedStateOptions>();

        Assert.Equal("first:", options?.KeyPrefix);

        await connection.CloseAsync();
        connection.Dispose();
    }
}
