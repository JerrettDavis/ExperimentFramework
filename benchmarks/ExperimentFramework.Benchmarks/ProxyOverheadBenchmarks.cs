using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using ExperimentFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace ExperimentFramework.Benchmarks;

// Test services (must be outside the benchmark class for source generator to access)
public interface ISimpleService
{
    string GetValue();
    Task<string> GetValueAsync();
}

public interface IGenericService<T>
{
    T GetItem();
    Task<T> GetItemAsync();
}

public class SimpleServiceV1 : ISimpleService
{
    public string GetValue() => "v1";
    public Task<string> GetValueAsync() => Task.FromResult("v1");
}

public class SimpleServiceV2 : ISimpleService
{
    public string GetValue() => "v2";
    public Task<string> GetValueAsync() => Task.FromResult("v2");
}

public class GenericServiceV1<T> : IGenericService<T>
{
    private readonly T _value;
    public GenericServiceV1() : this(default!) { }
    public GenericServiceV1(T value) => _value = value;
    public T GetItem() => _value;
    public Task<T> GetItemAsync() => Task.FromResult(_value);
}

public class GenericServiceV2<T> : IGenericService<T>
{
    private readonly T _value;
    public GenericServiceV2() : this(default!) { }
    public GenericServiceV2(T value) => _value = value;
    public T GetItem() => _value;
    public Task<T> GetItemAsync() => Task.FromResult(_value);
}

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ProxyOverheadBenchmarks
{

    // Service providers for different scenarios
    private IServiceProvider _directServiceProvider = null!;
    private IServiceProvider _proxiedFeatureFlagServiceProvider = null!;
    private IServiceProvider _proxiedConfigurationServiceProvider = null!;
    private IServiceProvider _proxiedGenericServiceProvider = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Direct service provider (baseline - no proxies)
        var directServices = new ServiceCollection();
        directServices.AddScoped<ISimpleService, SimpleServiceV1>();
        directServices.AddScoped<IGenericService<string>, GenericServiceV1<string>>();
        _directServiceProvider = directServices.BuildServiceProvider();

        // Proxied with feature flags
        var config1 = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseV2Service"] = "false"
            })
            .Build();

        var proxiedServices1 = new ServiceCollection();
        proxiedServices1.AddSingleton<IConfiguration>(config1);
        proxiedServices1.AddFeatureManagement();
        proxiedServices1.AddScoped<SimpleServiceV1>();
        proxiedServices1.AddScoped<SimpleServiceV2>();
        proxiedServices1.AddScoped<ISimpleService, SimpleServiceV1>();
        // Register all other services for composition root
        proxiedServices1.AddScoped<IGenericService<string>, GenericServiceV1<string>>();
        proxiedServices1.AddScoped<GenericServiceV1<string>>();
        proxiedServices1.AddScoped<GenericServiceV2<string>>();
        proxiedServices1.AddScoped<IDatabase, InMemoryDatabase>();
        proxiedServices1.AddScoped<InMemoryDatabase>();
        proxiedServices1.AddScoped<CloudDatabase>();
        proxiedServices1.AddScoped<ICache, SimpleCache>();
        proxiedServices1.AddScoped<SimpleCache>();
        proxiedServices1.AddScoped<AdvancedCache>();

        var experiments1 = BenchmarkCompositionRoot.ConfigureBenchmarkExperiments();
        proxiedServices1.AddExperimentFramework(experiments1);
        _proxiedFeatureFlagServiceProvider = proxiedServices1.BuildServiceProvider();

        // Proxied with configuration
        var config2 = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Service:Version"] = "v1"
            })
            .Build();

        var proxiedServices2 = new ServiceCollection();
        proxiedServices2.AddSingleton<IConfiguration>(config2);
        proxiedServices2.AddScoped<SimpleServiceV1>();
        proxiedServices2.AddScoped<SimpleServiceV2>();
        proxiedServices2.AddScoped<ISimpleService, SimpleServiceV1>();
        // Register all other services for composition root
        proxiedServices2.AddScoped<IGenericService<string>, GenericServiceV1<string>>();
        proxiedServices2.AddScoped<GenericServiceV1<string>>();
        proxiedServices2.AddScoped<GenericServiceV2<string>>();
        proxiedServices2.AddScoped<IDatabase, InMemoryDatabase>();
        proxiedServices2.AddScoped<InMemoryDatabase>();
        proxiedServices2.AddScoped<CloudDatabase>();
        proxiedServices2.AddScoped<ICache, SimpleCache>();
        proxiedServices2.AddScoped<SimpleCache>();
        proxiedServices2.AddScoped<AdvancedCache>();

        var experiments2 = BenchmarkCompositionRoot.ConfigureBenchmarkExperiments();
        proxiedServices2.AddExperimentFramework(experiments2);
        _proxiedConfigurationServiceProvider = proxiedServices2.BuildServiceProvider();

        // Proxied with generics
        var config3 = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseV2GenericService"] = "false"
            })
            .Build();

        var proxiedServices3 = new ServiceCollection();
        proxiedServices3.AddSingleton<IConfiguration>(config3);
        proxiedServices3.AddFeatureManagement();
        proxiedServices3.AddScoped<IGenericService<string>, GenericServiceV1<string>>();
        proxiedServices3.AddScoped<GenericServiceV1<string>>();
        proxiedServices3.AddScoped<GenericServiceV2<string>>();
        // Register all other services for composition root
        proxiedServices3.AddScoped<ISimpleService, SimpleServiceV1>();
        proxiedServices3.AddScoped<SimpleServiceV1>();
        proxiedServices3.AddScoped<SimpleServiceV2>();
        proxiedServices3.AddScoped<IDatabase, InMemoryDatabase>();
        proxiedServices3.AddScoped<InMemoryDatabase>();
        proxiedServices3.AddScoped<CloudDatabase>();
        proxiedServices3.AddScoped<ICache, SimpleCache>();
        proxiedServices3.AddScoped<SimpleCache>();
        proxiedServices3.AddScoped<AdvancedCache>();

        var experiments3 = BenchmarkCompositionRoot.ConfigureBenchmarkExperiments();
        proxiedServices3.AddExperimentFramework(experiments3);
        _proxiedGenericServiceProvider = proxiedServices3.BuildServiceProvider();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_directServiceProvider as IDisposable)?.Dispose();
        (_proxiedFeatureFlagServiceProvider as IDisposable)?.Dispose();
        (_proxiedConfigurationServiceProvider as IDisposable)?.Dispose();
        (_proxiedGenericServiceProvider as IDisposable)?.Dispose();
    }

    // ===== SYNCHRONOUS METHOD BENCHMARKS =====

    [Benchmark(Baseline = true, Description = "Direct: Sync method")]
    public string Direct_SyncMethod()
    {
        using var scope = _directServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        return service.GetValue();
    }

    [Benchmark(Description = "Proxied (FeatureFlag): Sync method")]
    public string Proxied_FeatureFlag_SyncMethod()
    {
        using var scope = _proxiedFeatureFlagServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        return service.GetValue();
    }

    [Benchmark(Description = "Proxied (Config): Sync method")]
    public string Proxied_Configuration_SyncMethod()
    {
        using var scope = _proxiedConfigurationServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        return service.GetValue();
    }

    // ===== ASYNCHRONOUS METHOD BENCHMARKS =====

    [Benchmark(Description = "Direct: Async method")]
    public async Task<string> Direct_AsyncMethod()
    {
        using var scope = _directServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        return await service.GetValueAsync();
    }

    [Benchmark(Description = "Proxied (FeatureFlag): Async method")]
    public async Task<string> Proxied_FeatureFlag_AsyncMethod()
    {
        using var scope = _proxiedFeatureFlagServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        return await service.GetValueAsync();
    }

    [Benchmark(Description = "Proxied (Config): Async method")]
    public async Task<string> Proxied_Configuration_AsyncMethod()
    {
        using var scope = _proxiedConfigurationServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        return await service.GetValueAsync();
    }

    // ===== GENERIC INTERFACE BENCHMARKS =====

    [Benchmark(Description = "Direct: Generic interface")]
    public string Direct_GenericInterface()
    {
        using var scope = _directServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGenericService<string>>();
        return service.GetItem();
    }

    [Benchmark(Description = "Proxied: Generic interface")]
    public string Proxied_GenericInterface()
    {
        using var scope = _proxiedGenericServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IGenericService<string>>();
        return service.GetItem();
    }

    // ===== REPEATED INVOCATION BENCHMARKS (WARMED UP) =====
    // These benchmarks simulate realistic production scenarios where the same proxy
    // is called multiple times, demonstrating performance after warmup

    [Benchmark(Description = "Direct: 10 repeated sync calls")]
    public string[] Direct_RepeatedSync_10Calls()
    {
        using var scope = _directServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        var results = new string[10];
        for (var i = 0; i < 10; i++)
        {
            results[i] = service.GetValue();
        }
        return results;
    }

    [Benchmark(Description = "Proxied (Config): 10 repeated sync calls")]
    public string[] Proxied_Configuration_RepeatedSync_10Calls()
    {
        using var scope = _proxiedConfigurationServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        var results = new string[10];
        for (var i = 0; i < 10; i++)
        {
            results[i] = service.GetValue();
        }
        return results;
    }

    [Benchmark(Description = "Direct: 100 repeated sync calls")]
    public string[] Direct_RepeatedSync_100Calls()
    {
        using var scope = _directServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        var results = new string[100];
        for (var i = 0; i < 100; i++)
        {
            results[i] = service.GetValue();
        }
        return results;
    }

    [Benchmark(Description = "Proxied (Config): 100 repeated sync calls")]
    public string[] Proxied_Configuration_RepeatedSync_100Calls()
    {
        using var scope = _proxiedConfigurationServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        var results = new string[100];
        for (var i = 0; i < 100; i++)
        {
            results[i] = service.GetValue();
        }
        return results;
    }

    [Benchmark(Description = "Direct: 10 repeated async calls")]
    public async Task<string[]> Direct_RepeatedAsync_10Calls()
    {
        using var scope = _directServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        var results = new string[10];
        for (var i = 0; i < 10; i++)
        {
            results[i] = await service.GetValueAsync();
        }
        return results;
    }

    [Benchmark(Description = "Proxied (Config): 10 repeated async calls")]
    public async Task<string[]> Proxied_Configuration_RepeatedAsync_10Calls()
    {
        using var scope = _proxiedConfigurationServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        var results = new string[10];
        for (var i = 0; i < 10; i++)
        {
            results[i] = await service.GetValueAsync();
        }
        return results;
    }

    [Benchmark(Description = "Direct: 100 repeated async calls")]
    public async Task<string[]> Direct_RepeatedAsync_100Calls()
    {
        using var scope = _directServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        var results = new string[100];
        for (var i = 0; i < 100; i++)
        {
            results[i] = await service.GetValueAsync();
        }
        return results;
    }

    [Benchmark(Description = "Proxied (Config): 100 repeated async calls")]
    public async Task<string[]> Proxied_Configuration_RepeatedAsync_100Calls()
    {
        using var scope = _proxiedConfigurationServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ISimpleService>();
        var results = new string[100];
        for (var i = 0; i < 100; i++)
        {
            results[i] = await service.GetValueAsync();
        }
        return results;
    }
}
