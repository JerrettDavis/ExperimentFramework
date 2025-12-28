using ExperimentFramework.Data;
using ExperimentFramework.Data.Decorators;
using ExperimentFramework.Data.Recording;
using ExperimentFramework.Data.Storage;
using ExperimentFramework.Decorators;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Data;

[Feature("ExperimentBuilderExtensions configure outcome collection")]
public sealed class ExperimentBuilderExtensionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("WithOutcomeCollection adds decorator factory")]
    [Fact]
    public Task WithOutcomeCollection_adds_decorator_factory()
        => Given("an experiment framework builder", () => ExperimentFrameworkBuilder.Create())
            .When("adding outcome collection", builder => builder.WithOutcomeCollection())
            .Then("builder is returned for chaining", builder => builder != null)
            .AssertPassed();

    [Scenario("WithOutcomeCollection with default options")]
    [Fact]
    public async Task WithOutcomeCollection_with_default_options()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IOutcomeStore, InMemoryOutcomeStore>();

        var builder = ExperimentFrameworkBuilder.Create()
            .WithOutcomeCollection();

        var sp = services.BuildServiceProvider();

        // Act - Get the decorator factory and create the decorator
        var factories = builder.GetDecoratorFactories();

        // Assert
        Assert.NotEmpty(factories);
        Assert.Contains(factories, f => f is OutcomeCollectionDecoratorFactory);
    }

    [Scenario("WithOutcomeCollection with custom configuration")]
    [Fact]
    public async Task WithOutcomeCollection_with_custom_configuration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<IOutcomeStore, InMemoryOutcomeStore>();

        var builder = ExperimentFrameworkBuilder.Create()
            .WithOutcomeCollection(opts =>
            {
                opts.CollectDuration = false;
                opts.CollectErrors = false;
                opts.DurationMetricName = "custom_duration";
            });

        // Act
        var factories = builder.GetDecoratorFactories();

        // Assert
        Assert.NotEmpty(factories);
        Assert.Contains(factories, f => f is OutcomeCollectionDecoratorFactory);
    }

    [Scenario("WithOutcomeCollection with experiment name resolver")]
    [Fact]
    public async Task WithOutcomeCollection_with_name_resolver()
    {
        // Arrange
        var services = new ServiceCollection();
        var store = new InMemoryOutcomeStore();
        services.AddSingleton<IOutcomeStore>(store);

        var builder = ExperimentFrameworkBuilder.Create()
            .WithOutcomeCollection(name => $"Prefix_{name}");

        var sp = services.BuildServiceProvider();

        // Act
        var factories = builder.GetDecoratorFactories();
        var factory = factories.OfType<OutcomeCollectionDecoratorFactory>().FirstOrDefault();

        // Assert
        Assert.NotNull(factory);

        // Create decorator and test
        var decorator = factory.Create(sp);
        Assert.NotNull(decorator);

        var ctx = new InvocationContext(typeof(ITestService), "Method", "trial", []);
        await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("result"));

        var outcomes = await store.QueryAsync(new ExperimentFramework.Data.Models.OutcomeQuery());
        Assert.NotEmpty(outcomes);
        Assert.All(outcomes, o => Assert.StartsWith("Prefix_", o.ExperimentName));
    }

    [Scenario("WithOutcomeCollection with name resolver and configuration")]
    [Fact]
    public async Task WithOutcomeCollection_with_name_resolver_and_config()
    {
        // Arrange
        var services = new ServiceCollection();
        var store = new InMemoryOutcomeStore();
        services.AddSingleton<IOutcomeStore>(store);

        var builder = ExperimentFrameworkBuilder.Create()
            .WithOutcomeCollection(
                experimentNameResolver: name => name.ToLowerInvariant(),
                configure: opts =>
                {
                    opts.CollectDuration = false;
                    opts.SuccessMetricName = "completed";
                });

        var sp = services.BuildServiceProvider();

        // Act
        var factories = builder.GetDecoratorFactories();
        var factory = factories.OfType<OutcomeCollectionDecoratorFactory>().FirstOrDefault();

        // Assert
        Assert.NotNull(factory);

        var decorator = factory.Create(sp);
        var ctx = new InvocationContext(typeof(ITestService), "Method", "trial", []);
        await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("result"));

        var outcomes = await store.QueryAsync(new ExperimentFramework.Data.Models.OutcomeQuery());

        // Experiment name should be lowercase
        Assert.Contains(outcomes, o => o.ExperimentName == "testservice");

        // Should have success metric but not duration (since we disabled it)
        Assert.Contains(outcomes, o => o.MetricName == "completed");
    }

    [Scenario("Multiple WithOutcomeCollection calls add multiple factories")]
    [Fact]
    public Task Multiple_WithOutcomeCollection_adds_multiple_factories()
        => Given("an experiment framework builder", () => ExperimentFrameworkBuilder.Create())
            .When("adding outcome collection twice", builder =>
                builder
                    .WithOutcomeCollection()
                    .WithOutcomeCollection(opts => opts.CollectDuration = false))
            .Then("both factories are added", builder =>
            {
                var factories = builder.GetDecoratorFactories();
                return factories.Count(f => f is OutcomeCollectionDecoratorFactory) == 2;
            })
            .AssertPassed();

    [Scenario("WithOutcomeCollection allows null configuration action")]
    [Fact]
    public Task WithOutcomeCollection_allows_null_configuration()
        => Given("an experiment framework builder", () => ExperimentFrameworkBuilder.Create())
            .When("adding outcome collection with null config", builder =>
                builder.WithOutcomeCollection(configure: null))
            .Then("builder is returned", builder => builder != null)
            .AssertPassed();

    [Scenario("Decorator uses NoopOutcomeStore when store not registered")]
    [Fact]
    public async Task Decorator_uses_noop_store_when_not_registered()
    {
        // Arrange
        var services = new ServiceCollection();
        var sp = services.BuildServiceProvider();

        var builder = ExperimentFrameworkBuilder.Create()
            .WithOutcomeCollection();

        // Act
        var factories = builder.GetDecoratorFactories();
        var factory = factories.OfType<OutcomeCollectionDecoratorFactory>().First();
        var decorator = factory.Create(sp);

        // Assert - Should not throw even without store
        var ctx = new InvocationContext(typeof(ITestService), "Method", "trial", []);
        var result = await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("test"));

        Assert.Equal("test", result);
    }

    [Scenario("Builder extensions are chainable")]
    [Fact]
    public Task Builder_extensions_are_chainable()
        => Given("an experiment framework builder", () => ExperimentFrameworkBuilder.Create())
            .When("chaining multiple calls", builder =>
                builder
                    .WithOutcomeCollection()
                    .WithOutcomeCollection(opts => opts.CollectErrors = false)
                    .WithOutcomeCollection(name => $"Test_{name}"))
            .Then("all factories are added", builder =>
            {
                var factories = builder.GetDecoratorFactories();
                return factories.Count(f => f is OutcomeCollectionDecoratorFactory) == 3;
            })
            .AssertPassed();

    public interface ITestService
    {
        string Process();
    }
}

// Extension to access decorator factories for testing
public static class ExperimentFrameworkBuilderTestExtensions
{
    public static IReadOnlyList<IExperimentDecoratorFactory> GetDecoratorFactories(this ExperimentFrameworkBuilder builder)
    {
        // Use reflection to access the internal list of decorator factories
        var field = typeof(ExperimentFrameworkBuilder).GetField("_decoratorFactories",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (field != null)
        {
            return (field.GetValue(builder) as List<IExperimentDecoratorFactory>) ?? [];
        }

        return [];
    }
}
