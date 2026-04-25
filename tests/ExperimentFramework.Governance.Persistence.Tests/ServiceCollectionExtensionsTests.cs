using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Governance.Persistence.Tests;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddInMemoryGovernancePersistence_RegistersBackplane()
    {
        var services = new ServiceCollection();
        services.AddInMemoryGovernancePersistence();

        using var sp = services.BuildServiceProvider();
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();

        backplane.Should().NotBeNull();
        backplane.Should().BeOfType<InMemoryGovernancePersistenceBackplane>();
    }

    [Fact]
    public void AddInMemoryGovernancePersistence_CalledTwice_RegistersOnce()
    {
        var services = new ServiceCollection();
        services.AddInMemoryGovernancePersistence();
        services.AddInMemoryGovernancePersistence();

        using var sp = services.BuildServiceProvider();
        var backplanes = sp.GetServices<IGovernancePersistenceBackplane>().ToList();

        backplanes.Should().HaveCount(1);
    }

    [Fact]
    public void AddGovernancePersistence_Generic_RegistersCustomImplementation()
    {
        var services = new ServiceCollection();
        services.AddGovernancePersistence<InMemoryGovernancePersistenceBackplane>();

        using var sp = services.BuildServiceProvider();
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();

        backplane.Should().NotBeNull();
        backplane.Should().BeOfType<InMemoryGovernancePersistenceBackplane>();
    }

    [Fact]
    public void AddGovernancePersistence_Generic_CalledTwice_RegistersOnce()
    {
        var services = new ServiceCollection();
        services.AddGovernancePersistence<InMemoryGovernancePersistenceBackplane>();
        services.AddGovernancePersistence<InMemoryGovernancePersistenceBackplane>();

        using var sp = services.BuildServiceProvider();
        var backplanes = sp.GetServices<IGovernancePersistenceBackplane>().ToList();

        backplanes.Should().HaveCount(1);
    }

    [Fact]
    public void AddGovernancePersistence_WithFactory_UsesFactory()
    {
        var services = new ServiceCollection();
        var expectedInstance = new InMemoryGovernancePersistenceBackplane();
        services.AddGovernancePersistence(_ => expectedInstance);

        using var sp = services.BuildServiceProvider();
        var backplane = sp.GetService<IGovernancePersistenceBackplane>();

        backplane.Should().BeSameAs(expectedInstance);
    }

    [Fact]
    public void AddGovernancePersistence_WithFactory_ReturnsSingleton()
    {
        var services = new ServiceCollection();
        services.AddGovernancePersistence(_ => new InMemoryGovernancePersistenceBackplane());

        using var sp = services.BuildServiceProvider();
        var b1 = sp.GetService<IGovernancePersistenceBackplane>();
        var b2 = sp.GetService<IGovernancePersistenceBackplane>();

        b1.Should().BeSameAs(b2);
    }

    [Fact]
    public void AddInMemoryGovernancePersistence_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddInMemoryGovernancePersistence();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddGovernancePersistence_Generic_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddGovernancePersistence<InMemoryGovernancePersistenceBackplane>();

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void AddGovernancePersistence_WithFactory_ReturnsServiceCollection_ForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddGovernancePersistence(_ => new InMemoryGovernancePersistenceBackplane());

        result.Should().BeSameAs(services);
    }
}
