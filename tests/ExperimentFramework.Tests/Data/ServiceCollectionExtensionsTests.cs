using ExperimentFramework.Data;
using ExperimentFramework.Data.Recording;
using ExperimentFramework.Data.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Tests.Data;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddExperimentDataCollection_RegistersRequiredServices()
    {
        var services = new ServiceCollection();

        services.AddExperimentDataCollection();

        var provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IOutcomeStore>());
        Assert.NotNull(provider.GetService<IOutcomeRecorder>());
        Assert.NotNull(provider.GetService<OutcomeRecorderOptions>());
    }

    [Fact]
    public void AddExperimentDataCollection_RegistersInMemoryStoreByDefault()
    {
        var services = new ServiceCollection();

        services.AddExperimentDataCollection();

        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IOutcomeStore>();

        Assert.IsType<InMemoryOutcomeStore>(store);
    }

    [Fact]
    public void AddExperimentDataCollection_RegistersSingletonServices()
    {
        var services = new ServiceCollection();

        services.AddExperimentDataCollection();

        var provider = services.BuildServiceProvider();

        var store1 = provider.GetRequiredService<IOutcomeStore>();
        var store2 = provider.GetRequiredService<IOutcomeStore>();
        var recorder1 = provider.GetRequiredService<IOutcomeRecorder>();
        var recorder2 = provider.GetRequiredService<IOutcomeRecorder>();

        Assert.Same(store1, store2);
        Assert.Same(recorder1, recorder2);
    }

    [Fact]
    public void AddExperimentDataCollection_AppliesConfiguration()
    {
        var services = new ServiceCollection();

        services.AddExperimentDataCollection(opts =>
        {
            opts.AutoGenerateIds = false;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<OutcomeRecorderOptions>();

        Assert.False(options.AutoGenerateIds);
    }

    [Fact]
    public void AddExperimentDataCollection_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddExperimentDataCollection();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddExperimentDataCollection_Generic_RegistersCustomStore()
    {
        var services = new ServiceCollection();

        services.AddExperimentDataCollection<TestOutcomeStore>();

        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IOutcomeStore>();

        Assert.IsType<TestOutcomeStore>(store);
    }

    [Fact]
    public void AddExperimentDataCollection_Generic_AppliesConfiguration()
    {
        var services = new ServiceCollection();

        services.AddExperimentDataCollection<TestOutcomeStore>(opts =>
        {
            opts.AutoSetTimestamps = false;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<OutcomeRecorderOptions>();

        Assert.False(options.AutoSetTimestamps);
    }

    [Fact]
    public void AddExperimentDataCollectionNoop_RegistersNoopStore()
    {
        var services = new ServiceCollection();

        services.AddExperimentDataCollectionNoop();

        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IOutcomeStore>();

        Assert.Same(NoopOutcomeStore.Instance, store);
    }

    [Fact]
    public void AddExperimentDataCollectionNoop_RegistersRecorder()
    {
        var services = new ServiceCollection();

        services.AddExperimentDataCollectionNoop();

        var provider = services.BuildServiceProvider();
        var recorder = provider.GetRequiredService<IOutcomeRecorder>();

        Assert.NotNull(recorder);
        Assert.IsType<OutcomeRecorder>(recorder);
    }

    [Fact]
    public void AddExperimentDataCollectionNoop_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddExperimentDataCollectionNoop();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddExperimentDataCollection_DoesNotOverrideExistingRegistrations()
    {
        var services = new ServiceCollection();
        var customStore = new TestOutcomeStore();
        services.AddSingleton<IOutcomeStore>(customStore);

        services.AddExperimentDataCollection();

        var provider = services.BuildServiceProvider();
        var store = provider.GetRequiredService<IOutcomeStore>();

        Assert.Same(customStore, store);
    }

    [Fact]
    public void AddExperimentDataConfiguration_RegistersDecoratorHandler()
    {
        var services = new ServiceCollection();

        services.AddExperimentDataConfiguration();

        var provider = services.BuildServiceProvider();
        var handlers = provider.GetServices<ExperimentFramework.Configuration.Extensions.IConfigurationDecoratorHandler>().ToList();

        Assert.Contains(handlers, h => h.DecoratorType == "outcomeCollection");
    }

    [Fact]
    public void AddExperimentDataConfiguration_ReturnsServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddExperimentDataConfiguration();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddExperimentDataConfiguration_IsIdempotent()
    {
        var services = new ServiceCollection();

        services.AddExperimentDataConfiguration();
        services.AddExperimentDataConfiguration();

        var provider = services.BuildServiceProvider();
        var handlers = provider.GetServices<ExperimentFramework.Configuration.Extensions.IConfigurationDecoratorHandler>()
            .Where(h => h.DecoratorType == "outcomeCollection")
            .ToList();

        // TryAddEnumerable should prevent duplicates
        Assert.Single(handlers);
    }

    // Test implementation of IOutcomeStore
    private sealed class TestOutcomeStore : IOutcomeStore
    {
        public ValueTask RecordAsync(ExperimentFramework.Data.Models.ExperimentOutcome outcome, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask RecordBatchAsync(IEnumerable<ExperimentFramework.Data.Models.ExperimentOutcome> outcomes, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask<IReadOnlyList<ExperimentFramework.Data.Models.ExperimentOutcome>> QueryAsync(ExperimentFramework.Data.Models.OutcomeQuery query, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyList<ExperimentFramework.Data.Models.ExperimentOutcome>>(Array.Empty<ExperimentFramework.Data.Models.ExperimentOutcome>());

        public ValueTask<IReadOnlyDictionary<string, ExperimentFramework.Data.Models.OutcomeAggregation>> GetAggregationsAsync(string experimentName, string metricName, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyDictionary<string, ExperimentFramework.Data.Models.OutcomeAggregation>>(new Dictionary<string, ExperimentFramework.Data.Models.OutcomeAggregation>());

        public ValueTask<IReadOnlyList<string>> GetTrialKeysAsync(string experimentName, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public ValueTask<IReadOnlyList<string>> GetMetricNamesAsync(string experimentName, CancellationToken cancellationToken = default)
            => ValueTask.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

        public ValueTask<long> CountAsync(ExperimentFramework.Data.Models.OutcomeQuery query, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(0L);

        public ValueTask<long> DeleteAsync(ExperimentFramework.Data.Models.OutcomeQuery query, CancellationToken cancellationToken = default)
            => ValueTask.FromResult(0L);
    }
}
