using System.Security.Cryptography;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

namespace ExperimentFramework.Benchmarks;

// Simulated database service (must be outside class for source generator)
public interface IDatabase
{
    Task<List<Customer>> GetCustomersAsync();
    Task<Customer?> GetCustomerByIdAsync(int id);
}

public record Customer(int Id, string Name, string Email);

public class InMemoryDatabase : IDatabase
{
    private readonly List<Customer> _customers =
    [
        new(1, "Alice", "alice@example.com"),
        new(2, "Bob", "bob@example.com"),
        new(3, "Charlie", "charlie@example.com")
    ];

    public async Task<List<Customer>> GetCustomersAsync()
    {
        // Simulate database latency (5ms)
        await Task.Delay(5);
        return _customers;
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        // Simulate database latency (2ms)
        await Task.Delay(2);
        return _customers.FirstOrDefault(c => c.Id == id);
    }
}

public class CloudDatabase : IDatabase
{
    private readonly List<Customer> _customers =
    [
        new(1, "Alice", "alice@example.com"),
        new(2, "Bob", "bob@example.com"),
        new(3, "Charlie", "charlie@example.com"),
        new(4, "David", "david@example.com")
    ];

    public async Task<List<Customer>> GetCustomersAsync()
    {
        // Simulate cloud database latency (15ms)
        await Task.Delay(15);
        return _customers;
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id)
    {
        // Simulate cloud database latency (8ms)
        await Task.Delay(8);
        return _customers.FirstOrDefault(c => c.Id == id);
    }
}

// Simulated cache service with CPU-bound work
public interface ICache
{
    string ComputeHash(string input);
    Task<string> ComputeHashAsync(string input);
}

public class SimpleCache : ICache
{
    public string ComputeHash(string input)
    {
        // Simulate CPU-bound work (hashing)
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public Task<string> ComputeHashAsync(string input)
    {
        return Task.FromResult(ComputeHash(input));
    }
}

public class AdvancedCache : ICache
{
    public string ComputeHash(string input)
    {
        // Simulate more intensive CPU-bound work
        using var sha = SHA512.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public Task<string> ComputeHashAsync(string input)
    {
        return Task.FromResult(ComputeHash(input));
    }
}

/// <summary>
/// Benchmarks simulating real-world scenarios to demonstrate proxy overhead is negligible
/// compared to actual business logic (I/O, CPU work, etc.).
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class RealWorldScenarioBenchmarks
{

    private IServiceProvider _directDatabaseProvider = null!;
    private IServiceProvider _proxiedDatabaseProvider = null!;
    private IServiceProvider _directCacheProvider = null!;
    private IServiceProvider _proxiedCacheProvider = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Direct database (baseline)
        var directDbServices = new ServiceCollection();
        directDbServices.AddScoped<IDatabase, InMemoryDatabase>();
        _directDatabaseProvider = directDbServices.BuildServiceProvider();

        // Proxied database
        var config1 = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloudDb"] = "false"
            })
            .Build();

        var proxiedDbServices = new ServiceCollection();
        proxiedDbServices.AddSingleton<IConfiguration>(config1);
        proxiedDbServices.AddFeatureManagement();
        proxiedDbServices.AddScoped<InMemoryDatabase>();
        proxiedDbServices.AddScoped<CloudDatabase>();
        proxiedDbServices.AddScoped<IDatabase, InMemoryDatabase>();

        var dbExperiments = BenchmarkCompositionRoot.ConfigureBenchmarkExperiments();
        proxiedDbServices.AddExperimentFramework(dbExperiments);
        _proxiedDatabaseProvider = proxiedDbServices.BuildServiceProvider();

        // Direct cache (baseline)
        var directCacheServices = new ServiceCollection();
        directCacheServices.AddScoped<ICache, SimpleCache>();
        _directCacheProvider = directCacheServices.BuildServiceProvider();

        // Proxied cache
        var config2 = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseAdvancedCache"] = "false"
            })
            .Build();

        var proxiedCacheServices = new ServiceCollection();
        proxiedCacheServices.AddSingleton<IConfiguration>(config2);
        proxiedCacheServices.AddFeatureManagement();
        proxiedCacheServices.AddScoped<SimpleCache>();
        proxiedCacheServices.AddScoped<AdvancedCache>();
        proxiedCacheServices.AddScoped<ICache, SimpleCache>();

        var cacheExperiments = BenchmarkCompositionRoot.ConfigureBenchmarkExperiments();
        proxiedCacheServices.AddExperimentFramework(cacheExperiments);
        _proxiedCacheProvider = proxiedCacheServices.BuildServiceProvider();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_directDatabaseProvider as IDisposable)?.Dispose();
        (_proxiedDatabaseProvider as IDisposable)?.Dispose();
        (_directCacheProvider as IDisposable)?.Dispose();
        (_proxiedCacheProvider as IDisposable)?.Dispose();
    }

    // ===== I/O-BOUND SCENARIOS =====

    [Benchmark(Baseline = true, Description = "Direct: I/O-bound query (5ms delay)")]
    public async Task<List<Customer>> Direct_IOBound_GetCustomers()
    {
        using var scope = _directDatabaseProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        return await db.GetCustomersAsync();
    }

    [Benchmark(Description = "Proxied: I/O-bound query (5ms delay)")]
    public async Task<List<Customer>> Proxied_IOBound_GetCustomers()
    {
        using var scope = _proxiedDatabaseProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        return await db.GetCustomersAsync();
    }

    [Benchmark(Description = "Direct: I/O-bound single fetch (2ms delay)")]
    public async Task<Customer?> Direct_IOBound_GetCustomerById()
    {
        using var scope = _directDatabaseProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        return await db.GetCustomerByIdAsync(1);
    }

    [Benchmark(Description = "Proxied: I/O-bound single fetch (2ms delay)")]
    public async Task<Customer?> Proxied_IOBound_GetCustomerById()
    {
        using var scope = _proxiedDatabaseProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        return await db.GetCustomerByIdAsync(1);
    }

    // ===== CPU-BOUND SCENARIOS =====

    [Benchmark(Description = "Direct: CPU-bound work (SHA256)")]
    public string Direct_CPUBound_ComputeHash()
    {
        using var scope = _directCacheProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();
        return cache.ComputeHash("test-data-for-hashing");
    }

    [Benchmark(Description = "Proxied: CPU-bound work (SHA256)")]
    public string Proxied_CPUBound_ComputeHash()
    {
        using var scope = _proxiedCacheProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();
        return cache.ComputeHash("test-data-for-hashing");
    }

    [Benchmark(Description = "Direct: CPU-bound async (SHA256)")]
    public async Task<string> Direct_CPUBound_ComputeHashAsync()
    {
        using var scope = _directCacheProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();
        return await cache.ComputeHashAsync("test-data-for-hashing");
    }

    [Benchmark(Description = "Proxied: CPU-bound async (SHA256)")]
    public async Task<string> Proxied_CPUBound_ComputeHashAsync()
    {
        using var scope = _proxiedCacheProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();
        return await cache.ComputeHashAsync("test-data-for-hashing");
    }

    // ===== REPEATED INVOCATION SCENARIOS (WARMED UP) =====
    // These benchmarks demonstrate performance of repeated calls to the same proxy
    // instance, simulating realistic production workloads

    [Benchmark(Description = "Direct: 10 repeated CPU-bound calls")]
    public string[] Direct_CPUBound_Repeated_10Calls()
    {
        using var scope = _directCacheProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();
        var results = new string[10];
        for (var i = 0; i < 10; i++)
        {
            results[i] = cache.ComputeHash($"test-data-{i}");
        }
        return results;
    }

    [Benchmark(Description = "Proxied: 10 repeated CPU-bound calls")]
    public string[] Proxied_CPUBound_Repeated_10Calls()
    {
        using var scope = _proxiedCacheProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();
        var results = new string[10];
        for (var i = 0; i < 10; i++)
        {
            results[i] = cache.ComputeHash($"test-data-{i}");
        }
        return results;
    }

    [Benchmark(Description = "Direct: 100 repeated CPU-bound calls")]
    public string[] Direct_CPUBound_Repeated_100Calls()
    {
        using var scope = _directCacheProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();
        var results = new string[100];
        for (var i = 0; i < 100; i++)
        {
            results[i] = cache.ComputeHash($"test-data-{i}");
        }
        return results;
    }

    [Benchmark(Description = "Proxied: 100 repeated CPU-bound calls")]
    public string[] Proxied_CPUBound_Repeated_100Calls()
    {
        using var scope = _proxiedCacheProvider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICache>();
        var results = new string[100];
        for (var i = 0; i < 100; i++)
        {
            results[i] = cache.ComputeHash($"test-data-{i}");
        }
        return results;
    }

    [Benchmark(Description = "Direct: 10 repeated I/O-bound calls")]
    public async Task<Customer?[]> Direct_IOBound_Repeated_10Calls()
    {
        using var scope = _directDatabaseProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var results = new Customer?[10];
        for (var i = 0; i < 10; i++)
        {
            results[i] = await db.GetCustomerByIdAsync(1);
        }
        return results;
    }

    [Benchmark(Description = "Proxied: 10 repeated I/O-bound calls")]
    public async Task<Customer?[]> Proxied_IOBound_Repeated_10Calls()
    {
        using var scope = _proxiedDatabaseProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var results = new Customer?[10];
        for (var i = 0; i < 10; i++)
        {
            results[i] = await db.GetCustomerByIdAsync(1);
        }
        return results;
    }

    [Benchmark(Description = "Direct: 100 repeated I/O-bound calls")]
    public async Task<Customer?[]> Direct_IOBound_Repeated_100Calls()
    {
        using var scope = _directDatabaseProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var results = new Customer?[100];
        for (var i = 0; i < 100; i++)
        {
            results[i] = await db.GetCustomerByIdAsync(1);
        }
        return results;
    }

    [Benchmark(Description = "Proxied: 100 repeated I/O-bound calls")]
    public async Task<Customer?[]> Proxied_IOBound_Repeated_100Calls()
    {
        using var scope = _proxiedDatabaseProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var results = new Customer?[100];
        for (var i = 0; i < 100; i++)
        {
            results[i] = await db.GetCustomerByIdAsync(1);
        }
        return results;
    }
}
