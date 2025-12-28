using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration;

[Feature("ConfigurationExtensionServiceCollectionExtensions registers configuration handlers")]
public sealed class ConfigurationExtensionServiceCollectionExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("AddExperimentConfigurationExtensions registers registry")]
    [Fact]
    public async Task AddExperimentConfigurationExtensions_registers_registry()
    {
        var services = new ServiceCollection();
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registry = sp.GetService<ConfigurationExtensionRegistry>();

        Assert.NotNull(registry);

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentConfigurationExtensions returns service collection for chaining")]
    [Fact]
    public Task AddExperimentConfigurationExtensions_returns_service_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding configuration extensions", services => services.AddExperimentConfigurationExtensions())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddExperimentConfigurationExtensions registers built-in decorator handlers")]
    [Fact]
    public async Task AddExperimentConfigurationExtensions_registers_builtin_decorators()
    {
        var services = new ServiceCollection();
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<ConfigurationExtensionRegistry>();

        Assert.True(registry.HasDecoratorHandler("logging"));
        Assert.True(registry.HasDecoratorHandler("timeout"));

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentConfigurationExtensions registers built-in selection mode handlers")]
    [Fact]
    public async Task AddExperimentConfigurationExtensions_registers_builtin_selection_modes()
    {
        var services = new ServiceCollection();
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<ConfigurationExtensionRegistry>();

        Assert.True(registry.HasSelectionModeHandler("featureFlag"));
        Assert.True(registry.HasSelectionModeHandler("configurationKey"));
        Assert.True(registry.HasSelectionModeHandler("custom"));

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentConfigurationExtensions registers custom decorator handler with type resolver")]
    [Fact]
    public async Task AddExperimentConfigurationExtensions_registers_custom_with_resolver()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITypeResolver>(new TestTypeResolver());
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<ConfigurationExtensionRegistry>();

        Assert.True(registry.HasDecoratorHandler("custom"));

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentConfigurationExtensions discovers DI-registered decorator handlers")]
    [Fact]
    public async Task AddExperimentConfigurationExtensions_discovers_di_decorator_handlers()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfigurationDecoratorHandler>(new TestDecoratorHandler("myCustomDecorator"));
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<ConfigurationExtensionRegistry>();

        Assert.True(registry.HasDecoratorHandler("myCustomDecorator"));

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentConfigurationExtensions discovers DI-registered selection mode handlers")]
    [Fact]
    public async Task AddExperimentConfigurationExtensions_discovers_di_selection_mode_handlers()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConfigurationSelectionModeHandler>(new TestSelectionModeHandler("myCustomMode"));
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<ConfigurationExtensionRegistry>();

        Assert.True(registry.HasSelectionModeHandler("myCustomMode"));

        await Task.CompletedTask;
    }

    [Scenario("AddExperimentConfigurationExtensions is idempotent")]
    [Fact]
    public async Task AddExperimentConfigurationExtensions_is_idempotent()
    {
        var services = new ServiceCollection();
        services.AddExperimentConfigurationExtensions();
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registries = sp.GetServices<ConfigurationExtensionRegistry>().ToList();

        // TryAddSingleton ensures only one registration
        Assert.Single(registries);

        await Task.CompletedTask;
    }

    [Scenario("AddConfigurationDecoratorHandler generic registers handler")]
    [Fact]
    public async Task AddConfigurationDecoratorHandler_generic_registers_handler()
    {
        var services = new ServiceCollection();
        services.AddConfigurationDecoratorHandler<TestDecoratorHandler>();
        var sp = services.BuildServiceProvider();

        var handlers = sp.GetServices<IConfigurationDecoratorHandler>().ToList();

        Assert.Single(handlers);
        Assert.IsType<TestDecoratorHandler>(handlers[0]);

        await Task.CompletedTask;
    }

    [Scenario("AddConfigurationDecoratorHandler generic returns service collection")]
    [Fact]
    public Task AddConfigurationDecoratorHandler_generic_returns_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding decorator handler", services => services.AddConfigurationDecoratorHandler<TestDecoratorHandler>())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddConfigurationDecoratorHandler instance registers handler")]
    [Fact]
    public async Task AddConfigurationDecoratorHandler_instance_registers_handler()
    {
        var services = new ServiceCollection();
        var handler = new TestDecoratorHandler("myHandler");
        services.AddConfigurationDecoratorHandler(handler);
        var sp = services.BuildServiceProvider();

        var registered = sp.GetService<IConfigurationDecoratorHandler>();

        Assert.Same(handler, registered);

        await Task.CompletedTask;
    }

    [Scenario("AddConfigurationDecoratorHandler instance returns service collection")]
    [Fact]
    public Task AddConfigurationDecoratorHandler_instance_returns_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding decorator handler instance", services =>
                services.AddConfigurationDecoratorHandler(new TestDecoratorHandler("test")))
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddConfigurationSelectionModeHandler generic registers handler")]
    [Fact]
    public async Task AddConfigurationSelectionModeHandler_generic_registers_handler()
    {
        var services = new ServiceCollection();
        services.AddConfigurationSelectionModeHandler<TestSelectionModeHandler>();
        var sp = services.BuildServiceProvider();

        var handlers = sp.GetServices<IConfigurationSelectionModeHandler>().ToList();

        Assert.Single(handlers);
        Assert.IsType<TestSelectionModeHandler>(handlers[0]);

        await Task.CompletedTask;
    }

    [Scenario("AddConfigurationSelectionModeHandler generic returns service collection")]
    [Fact]
    public Task AddConfigurationSelectionModeHandler_generic_returns_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding selection mode handler", services => services.AddConfigurationSelectionModeHandler<TestSelectionModeHandler>())
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("AddConfigurationSelectionModeHandler instance registers handler")]
    [Fact]
    public async Task AddConfigurationSelectionModeHandler_instance_registers_handler()
    {
        var services = new ServiceCollection();
        var handler = new TestSelectionModeHandler("myMode");
        services.AddConfigurationSelectionModeHandler(handler);
        var sp = services.BuildServiceProvider();

        var registered = sp.GetService<IConfigurationSelectionModeHandler>();

        Assert.Same(handler, registered);

        await Task.CompletedTask;
    }

    [Scenario("AddConfigurationSelectionModeHandler instance returns service collection")]
    [Fact]
    public Task AddConfigurationSelectionModeHandler_instance_returns_collection()
        => Given("a service collection", () => new ServiceCollection())
            .When("adding selection mode handler instance", services =>
                services.AddConfigurationSelectionModeHandler(new TestSelectionModeHandler("test")))
            .Then("returns the service collection", result => result != null)
            .AssertPassed();

    [Scenario("Multiple handlers can be registered via AddSingleton")]
    [Fact]
    public async Task Multiple_handlers_can_be_registered()
    {
        var services = new ServiceCollection();
        services.AddConfigurationDecoratorHandler<TestDecoratorHandler>();
        services.AddSingleton<IConfigurationDecoratorHandler>(new TestDecoratorHandler("second"));
        var sp = services.BuildServiceProvider();

        var handlers = sp.GetServices<IConfigurationDecoratorHandler>().ToList();

        Assert.Equal(2, handlers.Count);

        await Task.CompletedTask;
    }

    private sealed class TestTypeResolver : ITypeResolver
    {
        public Type Resolve(string typeName) => throw new NotImplementedException();

        public bool TryResolve(string typeName, out Type? type)
        {
            type = null;
            return false;
        }

        public void RegisterAlias(string alias, Type type) { }
    }

    public sealed class TestDecoratorHandler : IConfigurationDecoratorHandler
    {
        public TestDecoratorHandler() : this("test") { }

        public TestDecoratorHandler(string decoratorType)
        {
            DecoratorType = decoratorType;
        }

        public string DecoratorType { get; }

        public void Apply(ExperimentFrameworkBuilder builder, DecoratorConfig config, ILogger? logger) { }

        public IEnumerable<ConfigurationValidationError> Validate(DecoratorConfig config, string path)
        {
            yield break;
        }
    }

    public sealed class TestSelectionModeHandler : IConfigurationSelectionModeHandler
    {
        public TestSelectionModeHandler() : this("test") { }

        public TestSelectionModeHandler(string modeType)
        {
            ModeType = modeType;
        }

        public string ModeType { get; }

        public void Apply<TService>(ServiceExperimentBuilder<TService> builder, SelectionModeConfig config, ILogger? logger)
            where TService : class
        { }

        public IEnumerable<ConfigurationValidationError> Validate(SelectionModeConfig config, string path)
        {
            yield break;
        }
    }
}
