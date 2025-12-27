using ExperimentFramework.Data.Decorators;
using ExperimentFramework.Data.Models;
using ExperimentFramework.Data.Recording;
using ExperimentFramework.Data.Storage;
using ExperimentFramework.Decorators;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Tests.Data;

public class OutcomeCollectionDecoratorFactoryTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithDefaults_CreatesFactory()
    {
        var factory = new OutcomeCollectionDecoratorFactory();

        Assert.NotNull(factory);
    }

    [Fact]
    public void Constructor_WithOptions_CreatesFactory()
    {
        var options = new OutcomeRecorderOptions
        {
            CollectDuration = true,
            CollectErrors = true
        };

        var factory = new OutcomeCollectionDecoratorFactory(options);

        Assert.NotNull(factory);
    }

    [Fact]
    public void Constructor_WithNameResolver_CreatesFactory()
    {
        var factory = new OutcomeCollectionDecoratorFactory(
            experimentNameResolver: name => $"Test_{name}");

        Assert.NotNull(factory);
    }

    [Fact]
    public void Constructor_WithAllParameters_CreatesFactory()
    {
        var options = new OutcomeRecorderOptions { CollectDuration = false };
        var factory = new OutcomeCollectionDecoratorFactory(
            options,
            experimentNameResolver: name => name.ToLowerInvariant());

        Assert.NotNull(factory);
    }

    #endregion

    #region Create Tests

    [Fact]
    public void Create_ReturnsDecorator()
    {
        var factory = new OutcomeCollectionDecoratorFactory();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);

        Assert.NotNull(decorator);
        Assert.IsAssignableFrom<IExperimentDecorator>(decorator);
    }

    [Fact]
    public void Create_WithoutOutcomeStore_UsesNoopStore()
    {
        var factory = new OutcomeCollectionDecoratorFactory();
        var serviceProvider = new ServiceCollection().BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);

        // Should not throw when store is not registered
        Assert.NotNull(decorator);
    }

    [Fact]
    public void Create_WithOutcomeStore_UsesRegisteredStore()
    {
        var factory = new OutcomeCollectionDecoratorFactory();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IOutcomeStore, InMemoryOutcomeStore>()
            .BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);

        Assert.NotNull(decorator);
    }

    #endregion

    #region InvokeAsync Tests

    [Fact]
    public async Task InvokeAsync_OnSuccess_RecordsSuccess()
    {
        var store = new InMemoryOutcomeStore();
        var factory = new OutcomeCollectionDecoratorFactory();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IOutcomeStore>(store)
            .BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);
        var ctx = new InvocationContext(typeof(ITestService), "DoWork", "trial-a", []);

        await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("result"));

        var outcomes = await store.QueryAsync(new OutcomeQuery());
        Assert.NotEmpty(outcomes);
    }

    [Fact]
    public async Task InvokeAsync_OnSuccess_ReturnsResult()
    {
        var store = new InMemoryOutcomeStore();
        var factory = new OutcomeCollectionDecoratorFactory();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IOutcomeStore>(store)
            .BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);
        var ctx = new InvocationContext(typeof(ITestService), "DoWork", "trial-a", []);

        var result = await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("expected"));

        Assert.Equal("expected", result);
    }

    [Fact]
    public async Task InvokeAsync_OnException_RecordsErrorAndRethrows()
    {
        var options = new OutcomeRecorderOptions { CollectErrors = true };
        var store = new InMemoryOutcomeStore();
        var factory = new OutcomeCollectionDecoratorFactory(options);
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IOutcomeStore>(store)
            .BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);
        var ctx = new InvocationContext(typeof(ITestService), "DoWork", "trial-a", []);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            decorator.InvokeAsync(ctx, () => throw new InvalidOperationException("Test error")).AsTask());

        var outcomes = await store.QueryAsync(new OutcomeQuery());
        Assert.NotEmpty(outcomes);
    }

    [Fact]
    public async Task InvokeAsync_WithDurationCollection_RecordsDuration()
    {
        var options = new OutcomeRecorderOptions { CollectDuration = true };
        var store = new InMemoryOutcomeStore();
        var factory = new OutcomeCollectionDecoratorFactory(options);
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IOutcomeStore>(store)
            .BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);
        var ctx = new InvocationContext(typeof(ITestService), "DoWork", "trial-a", []);

        await decorator.InvokeAsync(ctx, async () =>
        {
            await Task.Delay(50);
            return "result";
        });

        var outcomes = await store.QueryAsync(new OutcomeQuery { MetricName = options.DurationMetricName });
        Assert.NotEmpty(outcomes);
    }

    [Fact]
    public async Task InvokeAsync_WithoutDurationCollection_DoesNotRecordDuration()
    {
        var options = new OutcomeRecorderOptions { CollectDuration = false };
        var store = new InMemoryOutcomeStore();
        var factory = new OutcomeCollectionDecoratorFactory(options);
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IOutcomeStore>(store)
            .BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);
        var ctx = new InvocationContext(typeof(ITestService), "DoWork", "trial-a", []);

        await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("result"));

        var outcomes = await store.QueryAsync(new OutcomeQuery { MetricName = options.DurationMetricName });
        Assert.Empty(outcomes);
    }

    [Fact]
    public async Task InvokeAsync_WithInterfaceType_RemovesIPrefix()
    {
        var store = new InMemoryOutcomeStore();
        var factory = new OutcomeCollectionDecoratorFactory();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IOutcomeStore>(store)
            .BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);
        var ctx = new InvocationContext(typeof(ITestService), "DoWork", "trial-a", []);

        await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("result"));

        var outcomes = await store.QueryAsync(new OutcomeQuery());
        var outcome = outcomes.First();

        // "ITestService" should become "TestService"
        Assert.Equal("TestService", outcome.ExperimentName);
    }

    [Fact]
    public async Task InvokeAsync_WithClassType_KeepsFullName()
    {
        var store = new InMemoryOutcomeStore();
        var factory = new OutcomeCollectionDecoratorFactory();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IOutcomeStore>(store)
            .BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);
        var ctx = new InvocationContext(typeof(TestServiceImpl), "DoWork", "trial-a", []);

        await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("result"));

        var outcomes = await store.QueryAsync(new OutcomeQuery());
        var outcome = outcomes.First();

        Assert.Equal("TestServiceImpl", outcome.ExperimentName);
    }

    [Fact]
    public async Task InvokeAsync_WithNameResolver_UsesCustomName()
    {
        var store = new InMemoryOutcomeStore();
        var factory = new OutcomeCollectionDecoratorFactory(
            experimentNameResolver: name => $"Custom_{name}");
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IOutcomeStore>(store)
            .BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);
        var ctx = new InvocationContext(typeof(ITestService), "DoWork", "trial-a", []);

        await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("result"));

        var outcomes = await store.QueryAsync(new OutcomeQuery());
        var outcome = outcomes.First();

        Assert.Equal("Custom_TestService", outcome.ExperimentName);
    }

    [Fact]
    public async Task InvokeAsync_RecordsTrialKey()
    {
        var store = new InMemoryOutcomeStore();
        var factory = new OutcomeCollectionDecoratorFactory();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IOutcomeStore>(store)
            .BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);
        var ctx = new InvocationContext(typeof(ITestService), "DoWork", "my-trial-key", []);

        await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("result"));

        var outcomes = await store.QueryAsync(new OutcomeQuery());
        var outcome = outcomes.First();

        Assert.Equal("my-trial-key", outcome.TrialKey);
    }

    [Fact]
    public async Task InvokeAsync_GeneratesUniqueSubjectId()
    {
        var store = new InMemoryOutcomeStore();
        var factory = new OutcomeCollectionDecoratorFactory();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<IOutcomeStore>(store)
            .BuildServiceProvider();

        var decorator = factory.Create(serviceProvider);
        var ctx = new InvocationContext(typeof(ITestService), "DoWork", "trial-a", []);

        // Two invocations should have different subject IDs
        await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("result"));
        await decorator.InvokeAsync(ctx, () => new ValueTask<object?>("result"));

        var outcomes = await store.QueryAsync(new OutcomeQuery());
        var subjectIds = outcomes.Select(o => o.SubjectId).Distinct().ToList();

        Assert.True(subjectIds.Count >= 2);
    }

    #endregion

    #region Test Helpers

    public interface ITestService
    {
        string DoWork();
    }

    public class TestServiceImpl : ITestService
    {
        public string DoWork() => "done";
    }

    #endregion
}
