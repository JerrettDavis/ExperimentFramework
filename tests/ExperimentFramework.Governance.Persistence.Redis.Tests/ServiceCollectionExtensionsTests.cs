using ExperimentFramework.Governance.Persistence;
using ExperimentFramework.Governance.Persistence.Redis;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;

namespace ExperimentFramework.Governance.Persistence.Redis.Tests;

/// <summary>
/// Unit tests for Redis ServiceCollectionExtensions.
/// These tests use a mock IConnectionMultiplexer so they do not require a real Redis instance.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    private static IConnectionMultiplexer CreateMockMultiplexer()
        => new Mock<IConnectionMultiplexer>().Object;

    [Fact]
    public void AddRedisGovernancePersistence_WithExistingMultiplexer_RegistersBackplane()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(CreateMockMultiplexer());
        services.AddRedisGovernancePersistence(keyPrefix: "test:");

        using var sp = services.BuildServiceProvider();
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();

        Assert.NotNull(backplane);
        Assert.IsType<RedisGovernancePersistenceBackplane>(backplane);
    }

    [Fact]
    public void AddRedisGovernancePersistence_CalledTwice_RegistersOnce()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(CreateMockMultiplexer());
        services.AddRedisGovernancePersistence(keyPrefix: "test:");
        services.AddRedisGovernancePersistence(keyPrefix: "test:");

        using var sp = services.BuildServiceProvider();
        var backplanes = sp.GetServices<IGovernancePersistenceBackplane>().ToList();

        Assert.Single(backplanes);
    }

    [Fact]
    public void AddRedisGovernancePersistence_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(CreateMockMultiplexer());

        var result = services.AddRedisGovernancePersistence(keyPrefix: "test:");

        Assert.Same(services, result);
    }

    [Fact]
    public void AddRedisGovernancePersistence_WithConnectionString_RegistersBackplaneDescriptor()
    {
        // Verify the DI registration is present without actually connecting to Redis.
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRedisGovernancePersistence("localhost:6379", "test:");

        var descriptor = services.FirstOrDefault(x => x.ServiceType == typeof(IGovernancePersistenceBackplane));
        Assert.NotNull(descriptor);
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void AddRedisGovernancePersistence_WithConnectionString_AlreadyHasMultiplexer_DoesNotRegisterSecond()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(CreateMockMultiplexer());

        // Call with connection string overload — should NOT add a second multiplexer
        services.AddRedisGovernancePersistence("localhost:6379", "test:");

        var multiplexerDescriptors = services
            .Where(x => x.ServiceType == typeof(IConnectionMultiplexer))
            .ToList();

        // Only 1 — our pre-registered mock, not a second one from the factory
        Assert.Single(multiplexerDescriptors);
    }

    [Fact]
    public void AddRedisGovernancePersistence_WithConnectionString_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var result = services.AddRedisGovernancePersistence("localhost:6379", "test:");

        Assert.Same(services, result);
    }

    [Fact]
    public void AddRedisGovernancePersistence_IsSingleton()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(CreateMockMultiplexer());
        services.AddRedisGovernancePersistence(keyPrefix: "test:");

        using var sp = services.BuildServiceProvider();
        var b1 = sp.GetService<IGovernancePersistenceBackplane>();
        var b2 = sp.GetService<IGovernancePersistenceBackplane>();

        Assert.Same(b1, b2);
    }
}
