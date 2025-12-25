# Advanced Topics

This guide covers advanced patterns and techniques for customizing and extending ExperimentFramework.

## Custom Decorators

Decorators provide cross-cutting concerns without modifying condition implementations. You can create custom decorators to add functionality like caching, retry logic, or custom telemetry.

### Implementing IExperimentDecorator

Decorators must implement the `IExperimentDecorator` interface:

```csharp
public interface IExperimentDecorator
{
    ValueTask<object?> InvokeAsync(
        InvocationContext context,
        Func<ValueTask<object?>> next);
}
```

The `InvocationContext` provides information about the current invocation:

```csharp
public sealed class InvocationContext
{
    public Type ServiceType { get; }
    public string MethodName { get; }
    public object?[] Arguments { get; }
    public string ConditionKey { get; }
}
```

### Example: Caching Decorator

Implement a decorator that caches method results:

```csharp
public class CachingDecorator : IExperimentDecorator
{
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheDuration;

    public CachingDecorator(IMemoryCache cache, TimeSpan cacheDuration)
    {
        _cache = cache;
        _cacheDuration = cacheDuration;
    }

    public async ValueTask<object?> InvokeAsync(
        InvocationContext context,
        Func<ValueTask<object?>> next)
    {
        var cacheKey = BuildCacheKey(context);

        if (_cache.TryGetValue(cacheKey, out object? cachedResult))
        {
            return cachedResult;
        }

        var result = await next().ConfigureAwait(false);

        _cache.Set(cacheKey, result, _cacheDuration);

        return result;
    }

    private static string BuildCacheKey(InvocationContext context)
    {
        var argsHash = string.Join(",", context.Arguments.Select(a => a?.GetHashCode() ?? 0));
        return $"{context.ServiceType.Name}:{context.MethodName}:{context.ConditionKey}:{argsHash}";
    }
}
```

### Decorator Factory

Create a factory to instantiate decorators with dependencies from DI:

```csharp
public class CachingDecoratorFactory : IExperimentDecoratorFactory
{
    private readonly TimeSpan _cacheDuration;

    public CachingDecoratorFactory(TimeSpan cacheDuration)
    {
        _cacheDuration = cacheDuration;
    }

    public IExperimentDecorator Create(IServiceProvider serviceProvider)
    {
        var cache = serviceProvider.GetRequiredService<IMemoryCache>();
        return new CachingDecorator(cache, _cacheDuration);
    }
}
```

Register the decorator:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddDecoratorFactory(new CachingDecoratorFactory(TimeSpan.FromMinutes(5)))
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDatabase>("false")
        .AddCondition<CloudDatabase>("true"));

services.AddMemoryCache();
services.AddExperimentFramework(experiments);
```

### Example: Retry Decorator

Implement retry logic for transient failures:

```csharp
public class RetryDecorator : IExperimentDecorator
{
    private readonly int _maxAttempts;
    private readonly TimeSpan _delay;
    private readonly ILogger<RetryDecorator> _logger;

    public RetryDecorator(int maxAttempts, TimeSpan delay, ILogger<RetryDecorator> logger)
    {
        _maxAttempts = maxAttempts;
        _delay = delay;
        _logger = logger;
    }

    public async ValueTask<object?> InvokeAsync(
        InvocationContext context,
        Func<ValueTask<object?>> next)
    {
        for (int attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            try
            {
                return await next().ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < _maxAttempts)
            {
                _logger.LogWarning(ex,
                    "Transient failure in {ServiceType}.{Method} (attempt {Attempt}/{Max})",
                    context.ServiceType.Name, context.MethodName, attempt, _maxAttempts);

                await Task.Delay(_delay).ConfigureAwait(false);
            }
        }

        // Final attempt - let exception propagate
        return await next().ConfigureAwait(false);
    }

    private static bool IsTransient(Exception ex)
    {
        return ex is TimeoutException
            || ex is HttpRequestException
            || (ex.InnerException != null && IsTransient(ex.InnerException));
    }
}
```

### Example: Circuit Breaker Decorator

Implement a circuit breaker pattern:

```csharp
public class CircuitBreakerDecorator : IExperimentDecorator
{
    private readonly int _failureThreshold;
    private readonly TimeSpan _resetTimeout;
    private int _consecutiveFailures;
    private DateTime _lastFailureTime;
    private bool _isOpen;

    public CircuitBreakerDecorator(int failureThreshold, TimeSpan resetTimeout)
    {
        _failureThreshold = failureThreshold;
        _resetTimeout = resetTimeout;
    }

    public async ValueTask<object?> InvokeAsync(
        InvocationContext context,
        Func<ValueTask<object?>> next)
    {
        // Check if circuit should reset
        if (_isOpen && DateTime.UtcNow - _lastFailureTime > _resetTimeout)
        {
            _isOpen = false;
            _consecutiveFailures = 0;
        }

        if (_isOpen)
        {
            throw new InvalidOperationException(
                $"Circuit breaker is open for {context.ServiceType.Name}.{context.MethodName}");
        }

        try
        {
            var result = await next().ConfigureAwait(false);
            _consecutiveFailures = 0;
            return result;
        }
        catch (Exception)
        {
            _consecutiveFailures++;
            _lastFailureTime = DateTime.UtcNow;

            if (_consecutiveFailures >= _failureThreshold)
            {
                _isOpen = true;
            }

            throw;
        }
    }
}
```

### Decorator Pipeline Order

Decorators execute in registration order. The order matters:

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .AddDecoratorFactory(new CircuitBreakerFactory())  // First: Check circuit
    .AddDecoratorFactory(new RetryFactory())           // Second: Retry if needed
    .AddDecoratorFactory(new CachingFactory())         // Third: Cache successful results
    .AddLogger(l => l.AddBenchmarks())                 // Fourth: Measure total time
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDatabase>("false")
        .AddCondition<CloudDatabase>("true"));
```

Execution flow:

```
Request
  └─ Circuit Breaker (checks if open)
      └─ Retry (handles transient failures)
          └─ Caching (checks cache, stores result)
              └─ Benchmark (measures time)
                  └─ Condition Execution
```

## Request-Scoped Consistency

Ensuring consistent condition selection within a request scope is critical for correctness.

### Using IFeatureManagerSnapshot

For scoped services, use `IFeatureManagerSnapshot` to ensure consistent feature evaluation:

```csharp
public class OrderService
{
    private readonly IDatabase _database;
    private readonly IPaymentProcessor _payment;

    public OrderService(IDatabase database, IPaymentProcessor payment)
    {
        _database = database;
        _payment = payment;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        // Both calls within this scope use the same feature flag evaluation
        var customer = await _database.GetCustomerAsync(order.CustomerId);
        await _database.SaveOrderAsync(order);

        // Payment processor also uses consistent evaluation
        await _payment.ChargeAsync(order.Total);
    }
}
```

Register services and feature management:

```csharp
services.AddScoped<OrderService>();
services.AddFeatureManagement();

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDatabase>("false")
        .AddCondition<CloudDatabase>("true"))
    .Trial<IPaymentProcessor>(t => t
        .UsingFeatureFlag("UseNewPayment")
        .AddControl<StripePayment>("false")
        .AddCondition<NewPaymentProvider>("true"));

services.AddExperimentFramework(experiments);
```

### Scoped Identity Provider

For sticky routing, implement a scoped identity provider:

```csharp
public class HttpContextIdentityProvider : IExperimentIdentityProvider
{
    private readonly IHttpContextAccessor _httpContext;
    private string? _cachedIdentity;

    public HttpContextIdentityProvider(IHttpContextAccessor httpContext)
    {
        _httpContext = httpContext;
    }

    public bool TryGetIdentity(out string identity)
    {
        if (_cachedIdentity != null)
        {
            identity = _cachedIdentity;
            return true;
        }

        var userId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(userId))
        {
            _cachedIdentity = userId;
            identity = userId;
            return true;
        }

        identity = string.Empty;
        return false;
    }
}
```

Register as scoped:

```csharp
services.AddHttpContextAccessor();
services.AddScoped<IExperimentIdentityProvider, HttpContextIdentityProvider>();
```

This ensures the same user identity is used for all sticky routing decisions within a request.

## Multi-Tenant Scenarios

ExperimentFramework supports multi-tenant applications through custom naming conventions and identity providers.

### Tenant-Aware Naming Convention

```csharp
public class TenantNamingConvention : IExperimentNamingConvention
{
    private readonly ITenantProvider _tenantProvider;

    public TenantNamingConvention(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public string FeatureFlagNameFor(Type serviceType)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        return $"{tenantId}_{serviceType.Name}";
    }

    public string VariantFlagNameFor(Type serviceType)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        return $"{tenantId}_{serviceType.Name}_Variants";
    }

    public string ConfigurationKeyFor(Type serviceType)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        return $"Tenants:{tenantId}:Experiments:{serviceType.Name}";
    }
}
```

Configuration per tenant:

```json
{
  "Tenants": {
    "tenant-a": {
      "Experiments": {
        "IDatabase": "postgres"
      }
    },
    "tenant-b": {
      "Experiments": {
        "IDatabase": "mysql"
      }
    }
  }
}
```

### Tenant-Aware Identity Provider

For sticky routing in multi-tenant scenarios:

```csharp
public class TenantUserIdentityProvider : IExperimentIdentityProvider
{
    private readonly ITenantProvider _tenantProvider;
    private readonly IHttpContextAccessor _httpContext;

    public TenantUserIdentityProvider(
        ITenantProvider tenantProvider,
        IHttpContextAccessor httpContext)
    {
        _tenantProvider = tenantProvider;
        _httpContext = httpContext;
    }

    public bool TryGetIdentity(out string identity)
    {
        var tenantId = _tenantProvider.GetCurrentTenantId();
        var userId = _httpContext.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(tenantId) && !string.IsNullOrEmpty(userId))
        {
            identity = $"{tenantId}:{userId}";
            return true;
        }

        identity = string.Empty;
        return false;
    }
}
```

This ensures users in different tenants can receive different condition assignments.

## Performance Considerations

### Minimizing Overhead

The framework is designed for minimal overhead, but you can optimize further:

**Use Singleton Services When Possible**

```csharp
// Singleton services avoid proxy creation overhead per request
services.AddSingleton<ICache, InMemoryCache>();

var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<ICache>(t => t
        .UsingFeatureFlag("UseRedisCache")
        .AddControl<InMemoryCache>("false")
        .AddCondition<RedisCache>("true"));
```

**Disable Telemetry in Production**

If you're not using telemetry, the default no-op implementation has near-zero overhead:

```csharp
// No need to register OpenTelemetry if not using it
// Default NoopExperimentTelemetry is automatically registered
```

**Cache Configuration Values**

For configuration-based selection, .NET's `IConfiguration` already caches values. No additional caching needed.

### Benchmarking

Measure the overhead of your experiment setup:

```csharp
[MemoryDiagnoser]
public class ExperimentBenchmark
{
    private IServiceProvider _serviceProvider;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingFeatureFlag("UseCloudDb")
                .AddControl<LocalDatabase>("false")
                .AddCondition<CloudDatabase>("true"));

        services.AddExperimentFramework(experiments);
        _serviceProvider = services.BuildServiceProvider();
    }

    [Benchmark]
    public async Task<string> InvokeExperiment()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        return await db.GetConnectionStringAsync();
    }
}
```

## Testing Strategies

### Testing with Feature Flags

Override feature flag values in tests:

```csharp
public class OrderServiceTests
{
    [Fact]
    public async Task ProcessOrder_uses_cloud_database_when_flag_enabled()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloudDb"] = "true"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<OrderService>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingFeatureFlag("UseCloudDb")
                .AddControl<LocalDatabase>("false")
                .AddCondition<CloudDatabase>("true"));

        services.AddExperimentFramework(experiments);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        using var scope = serviceProvider.CreateScope();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();
        await orderService.ProcessOrderAsync(new Order());

        // Assert
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        Assert.IsType<CloudDatabase>(db);
    }
}
```

### Testing with Mock Identity Provider

For sticky routing tests:

```csharp
public class StickyRoutingTests
{
    [Fact]
    public async Task StickyRouting_assigns_same_user_to_same_condition()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IExperimentIdentityProvider>(_ =>
            new FixedIdentityProvider("user-123"));

        services.AddScoped<ContentBased>();
        services.AddScoped<CollaborativeFiltering>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Trial<IRecommendationEngine>(t => t
                .UsingStickyRouting("RecommendationExperiment")
                .AddControl<ContentBased>("control")
                .AddVariant<CollaborativeFiltering>("variant-a"));

        services.AddExperimentFramework(experiments);

        var serviceProvider = services.BuildServiceProvider();

        // Act - Multiple invocations
        string? firstResult, secondResult;
        using (var scope = serviceProvider.CreateScope())
        {
            var engine = scope.ServiceProvider.GetRequiredService<IRecommendationEngine>();
            firstResult = await engine.GetRecommendationsAsync("product-1");
        }

        using (var scope = serviceProvider.CreateScope())
        {
            var engine = scope.ServiceProvider.GetRequiredService<IRecommendationEngine>();
            secondResult = await engine.GetRecommendationsAsync("product-1");
        }

        // Assert
        Assert.Equal(firstResult, secondResult);
    }

    private sealed class FixedIdentityProvider : IExperimentIdentityProvider
    {
        private readonly string _identity;

        public FixedIdentityProvider(string identity)
        {
            _identity = identity;
        }

        public bool TryGetIdentity(out string identity)
        {
            identity = _identity;
            return true;
        }
    }
}
```

### Testing Condition Implementations Directly

Test condition implementations independently of the framework:

```csharp
public class CloudDatabaseTests
{
    [Fact]
    public async Task GetCustomersAsync_returns_all_customers()
    {
        // Arrange
        var logger = new Mock<ILogger<CloudDatabase>>();
        var config = new Mock<IConfiguration>();
        var database = new CloudDatabase(logger.Object, config.Object);

        // Act
        var customers = await database.GetCustomersAsync();

        // Assert
        Assert.NotEmpty(customers);
        Assert.All(customers, c =>
        {
            Assert.NotNull(c.Name);
            Assert.NotNull(c.Email);
        });
    }
}
```

This approach tests the actual implementation without experiment overhead.

## Custom Telemetry Providers

Integrate with your preferred observability platform.

### Datadog Integration

```csharp
public class DatadogExperimentTelemetry : IExperimentTelemetry
{
    private readonly IMetrics _metrics;

    public DatadogExperimentTelemetry(IMetrics metrics)
    {
        _metrics = metrics;
    }

    public IExperimentTelemetryScope StartInvocation(
        Type serviceType,
        string methodName,
        string selectorName,
        string conditionKey,
        IReadOnlyList<string> candidateKeys)
    {
        return new DatadogScope(_metrics, serviceType, methodName, conditionKey);
    }

    private class DatadogScope : IExperimentTelemetryScope
    {
        private readonly IMetrics _metrics;
        private readonly Type _serviceType;
        private readonly string _methodName;
        private readonly string _conditionKey;
        private readonly long _startTimestamp;
        private string _outcome = "success";

        public DatadogScope(IMetrics metrics, Type serviceType, string methodName, string conditionKey)
        {
            _metrics = metrics;
            _serviceType = serviceType;
            _methodName = methodName;
            _conditionKey = conditionKey;
            _startTimestamp = Stopwatch.GetTimestamp();
        }

        public void RecordSuccess()
        {
            _outcome = "success";
        }

        public void RecordFailure(Exception exception)
        {
            _outcome = "failure";
        }

        public void RecordFallback(string fallbackKey)
        {
            _metrics.Increment("experiment.fallback",
                tags: new[] { $"service:{_serviceType.Name}", $"condition:{_conditionKey}" });
        }

        public void RecordVariant(string variantName, string variantSource)
        {
            // Record variant metrics if needed
        }

        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_startTimestamp).TotalMilliseconds;

            _metrics.Histogram("experiment.duration", elapsed,
                tags: new[]
                {
                    $"service:{_serviceType.Name}",
                    $"method:{_methodName}",
                    $"condition:{_conditionKey}",
                    $"outcome:{_outcome}"
                });

            _metrics.Increment("experiment.invocations",
                tags: new[]
                {
                    $"service:{_serviceType.Name}",
                    $"condition:{_conditionKey}",
                    $"outcome:{_outcome}"
                });
        }
    }
}
```

Register with DI:

```csharp
services.AddSingleton<IExperimentTelemetry, DatadogExperimentTelemetry>();
```

## Next Steps

- [Samples](samples.md) - Complete working examples of advanced patterns
- [Core Concepts](core-concepts.md) - Review fundamental framework concepts
- [Telemetry](telemetry.md) - Learn about built-in telemetry options
