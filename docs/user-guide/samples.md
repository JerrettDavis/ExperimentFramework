# Samples

This guide provides complete, working examples of common ExperimentFramework patterns and scenarios.

## ASP.NET Core Web API

Complete example of an ASP.NET Core API using experiments for database and cache implementations.

### Project Structure

```
MyApi/
├── Program.cs
├── appsettings.json
├── Controllers/
│   └── CustomersController.cs
├── Services/
│   ├── IDatabase.cs
│   ├── LocalDatabase.cs
│   ├── CloudDatabase.cs
│   ├── ICache.cs
│   ├── InMemoryCache.cs
│   └── RedisCache.cs
└── Models/
    └── Customer.cs
```

### Program.cs

```csharp
using ExperimentFramework;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add feature management
builder.Services.AddFeatureManagement();

// Register condition implementations
builder.Services.AddScoped<LocalDatabase>();
builder.Services.AddScoped<CloudDatabase>();
builder.Services.AddSingleton<InMemoryCache>();
builder.Services.AddSingleton<RedisCache>();

// Register default implementations
builder.Services.AddScoped<IDatabase, LocalDatabase>();
builder.Services.AddSingleton<ICache, InMemoryCache>();

// Define experiments
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l
        .AddBenchmarks()
        .AddErrorLogging())
    .Trial<IDatabase>(t => t
        .UsingFeatureFlag("UseCloudDb")
        .AddControl<LocalDatabase>("false")
        .AddVariant<CloudDatabase>("true")
        .OnErrorFallbackToControl())
    .Trial<ICache>(t => t
        .UsingConfigurationKey("Cache:Provider")
        .AddControl<InMemoryCache>("inmemory")
        .AddVariant<RedisCache>("redis")
        .OnErrorFallbackToControl());

builder.Services.AddExperimentFramework(experiments);

// Add OpenTelemetry experiment tracking
builder.Services.AddOpenTelemetryExperimentTracking();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### appsettings.json

```json
{
  "FeatureManagement": {
    "UseCloudDb": false
  },
  "Cache": {
    "Provider": "inmemory"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "ExperimentFramework": "Information"
    }
  }
}
```

### CustomersController.cs

```csharp
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IDatabase _database;
    private readonly ICache _cache;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(
        IDatabase database,
        ICache cache,
        ILogger<CustomersController> logger)
    {
        _database = database;
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
    {
        var cacheKey = "customers:all";

        // Try cache first
        var cached = await _cache.GetAsync<IEnumerable<Customer>>(cacheKey);
        if (cached != null)
        {
            _logger.LogInformation("Returning customers from cache");
            return Ok(cached);
        }

        // Fetch from database
        var customers = await _database.GetCustomersAsync();

        // Store in cache
        await _cache.SetAsync(cacheKey, customers, TimeSpan.FromMinutes(5));

        return Ok(customers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Customer>> GetCustomer(int id)
    {
        var cacheKey = $"customers:{id}";

        var cached = await _cache.GetAsync<Customer>(cacheKey);
        if (cached != null)
        {
            return Ok(cached);
        }

        var customer = await _database.GetCustomerAsync(id);
        if (customer == null)
        {
            return NotFound();
        }

        await _cache.SetAsync(cacheKey, customer, TimeSpan.FromMinutes(5));

        return Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<Customer>> CreateCustomer(Customer customer)
    {
        await _database.CreateCustomerAsync(customer);

        // Invalidate cache
        await _cache.RemoveAsync("customers:all");

        return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
    }
}
```

### Service Implementations

**IDatabase.cs**

```csharp
public interface IDatabase
{
    Task<IEnumerable<Customer>> GetCustomersAsync();
    Task<Customer?> GetCustomerAsync(int id);
    Task CreateCustomerAsync(Customer customer);
}
```

**LocalDatabase.cs**

```csharp
public class LocalDatabase : IDatabase
{
    private readonly ILogger<LocalDatabase> _logger;
    private readonly List<Customer> _customers = new()
    {
        new Customer { Id = 1, Name = "Alice", Email = "alice@example.com" },
        new Customer { Id = 2, Name = "Bob", Email = "bob@example.com" }
    };

    public LocalDatabase(ILogger<LocalDatabase> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<Customer>> GetCustomersAsync()
    {
        _logger.LogInformation("Fetching customers from local database");
        await Task.Delay(50); // Simulate database query
        return _customers;
    }

    public async Task<Customer?> GetCustomerAsync(int id)
    {
        _logger.LogInformation("Fetching customer {CustomerId} from local database", id);
        await Task.Delay(30);
        return _customers.FirstOrDefault(c => c.Id == id);
    }

    public async Task CreateCustomerAsync(Customer customer)
    {
        _logger.LogInformation("Creating customer in local database");
        await Task.Delay(40);
        customer.Id = _customers.Max(c => c.Id) + 1;
        _customers.Add(customer);
    }
}
```

**CloudDatabase.cs**

```csharp
public class CloudDatabase : IDatabase
{
    private readonly ILogger<CloudDatabase> _logger;
    private readonly IConfiguration _config;

    public CloudDatabase(ILogger<CloudDatabase> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public async Task<IEnumerable<Customer>> GetCustomersAsync()
    {
        _logger.LogInformation("Fetching customers from cloud database");
        await Task.Delay(30); // Faster cloud query
        return new List<Customer>
        {
            new Customer { Id = 1, Name = "Alice", Email = "alice@example.com" },
            new Customer { Id = 2, Name = "Bob", Email = "bob@example.com" },
            new Customer { Id = 3, Name = "Charlie", Email = "charlie@example.com" }
        };
    }

    public async Task<Customer?> GetCustomerAsync(int id)
    {
        _logger.LogInformation("Fetching customer {CustomerId} from cloud database", id);
        await Task.Delay(20);
        var customers = await GetCustomersAsync();
        return customers.FirstOrDefault(c => c.Id == id);
    }

    public async Task CreateCustomerAsync(Customer customer)
    {
        _logger.LogInformation("Creating customer in cloud database");
        await Task.Delay(25);
        customer.Id = 4; // Simulated ID from cloud DB
    }
}
```

**ICache.cs**

```csharp
public interface ICache
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    Task RemoveAsync(string key);
}
```

**InMemoryCache.cs**

```csharp
public class InMemoryCache : ICache
{
    private readonly ConcurrentDictionary<string, (object Value, DateTime Expiration)> _cache = new();
    private readonly ILogger<InMemoryCache> _logger;

    public InMemoryCache(ILogger<InMemoryCache> logger)
    {
        _logger = logger;
    }

    public Task<T?> GetAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.Expiration > DateTime.UtcNow)
            {
                _logger.LogInformation("Cache hit for key: {Key}", key);
                return Task.FromResult((T?)entry.Value);
            }

            _cache.TryRemove(key, out _);
        }

        _logger.LogInformation("Cache miss for key: {Key}", key);
        return Task.FromResult<T?>(default);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        _logger.LogInformation("Setting cache key: {Key}", key);
        _cache[key] = (value!, DateTime.UtcNow.Add(expiration));
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        _logger.LogInformation("Removing cache key: {Key}", key);
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
```

**RedisCache.cs**

```csharp
public class RedisCache : ICache
{
    private readonly ILogger<RedisCache> _logger;
    // In a real implementation, inject IConnectionMultiplexer or StackExchange.Redis client

    public RedisCache(ILogger<RedisCache> logger)
    {
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        _logger.LogInformation("Fetching from Redis: {Key}", key);
        await Task.Delay(10); // Simulate Redis latency
        return default; // Simplified for example
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
    {
        _logger.LogInformation("Setting Redis key: {Key}", key);
        await Task.Delay(10);
    }

    public async Task RemoveAsync(string key)
    {
        _logger.LogInformation("Removing Redis key: {Key}", key);
        await Task.Delay(10);
    }
}
```

**Customer.cs**

```csharp
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
```

## Background Service with Sticky Routing

Example of a worker service using sticky routing for A/B testing recommendation algorithms.

### Program.cs

```csharp
using ExperimentFramework;

var builder = Host.CreateApplicationBuilder(args);

// Register condition implementations
builder.Services.AddScoped<ContentBased>();
builder.Services.AddScoped<CollaborativeFiltering>();
builder.Services.AddScoped<HybridRecommendations>();

// Register default implementation
builder.Services.AddScoped<IRecommendationEngine, ContentBased>();

// Register identity provider
builder.Services.AddScoped<IExperimentIdentityProvider, SimulatedUserIdentityProvider>();

// Define experiment with sticky routing
var experiments = ExperimentFrameworkBuilder.Create()
    .AddLogger(l => l.AddBenchmarks())
    .Trial<IRecommendationEngine>(t => t
        .UsingStickyRouting("RecommendationExperiment")
        .AddControl<ContentBased>("control")
        .AddVariant<CollaborativeFiltering>("variant-a")
        .AddVariant<HybridRecommendations>("variant-b"));

builder.Services.AddExperimentFramework(experiments);

// Register worker
builder.Services.AddHostedService<RecommendationWorker>();

var host = builder.Build();
host.Run();
```

### SimulatedUserIdentityProvider.cs

```csharp
public class SimulatedUserIdentityProvider : IExperimentIdentityProvider
{
    private static readonly string[] SimulatedUsers = new[]
    {
        "user-001", "user-002", "user-003", "user-004", "user-005",
        "user-006", "user-007", "user-008", "user-009", "user-010"
    };

    private static int _currentIndex;

    public bool TryGetIdentity(out string identity)
    {
        // Cycle through simulated users
        identity = SimulatedUsers[Interlocked.Increment(ref _currentIndex) % SimulatedUsers.Length];
        return true;
    }
}
```

### RecommendationWorker.cs

```csharp
public class RecommendationWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RecommendationWorker> _logger;

    public RecommendationWorker(
        IServiceProvider serviceProvider,
        ILogger<RecommendationWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Recommendation worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _serviceProvider.CreateScope();
            var engine = scope.ServiceProvider.GetRequiredService<IRecommendationEngine>();

            var recommendations = await engine.GetRecommendationsAsync("product-123");

            _logger.LogInformation("Generated recommendations: {Recommendations}",
                string.Join(", ", recommendations));

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }

        _logger.LogInformation("Recommendation worker stopped");
    }
}
```

### IRecommendationEngine.cs

```csharp
public interface IRecommendationEngine
{
    Task<IEnumerable<string>> GetRecommendationsAsync(string productId);
}
```

### ContentBased.cs

```csharp
public class ContentBased : IRecommendationEngine
{
    private readonly ILogger<ContentBased> _logger;

    public ContentBased(ILogger<ContentBased> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetRecommendationsAsync(string productId)
    {
        _logger.LogInformation("Generating content-based recommendations for {ProductId}", productId);
        await Task.Delay(50);
        return new[] { "product-124", "product-125", "product-126" };
    }
}
```

### CollaborativeFiltering.cs

```csharp
public class CollaborativeFiltering : IRecommendationEngine
{
    private readonly ILogger<CollaborativeFiltering> _logger;

    public CollaborativeFiltering(ILogger<CollaborativeFiltering> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetRecommendationsAsync(string productId)
    {
        _logger.LogInformation("Generating collaborative filtering recommendations for {ProductId}", productId);
        await Task.Delay(75);
        return new[] { "product-200", "product-201", "product-202" };
    }
}
```

### HybridRecommendations.cs

```csharp
public class HybridRecommendations : IRecommendationEngine
{
    private readonly ILogger<HybridRecommendations> _logger;

    public HybridRecommendations(ILogger<HybridRecommendations> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<string>> GetRecommendationsAsync(string productId)
    {
        _logger.LogInformation("Generating hybrid recommendations for {ProductId}", productId);
        await Task.Delay(100);
        return new[] { "product-300", "product-124", "product-201" };
    }
}
```

## Variant Feature Flags with Targeting

Example using variant feature flags with user-specific targeting.

### appsettings.json

```json
{
  "FeatureManagement": {
    "EmailProvider": {
      "EnabledFor": [
        {
          "Name": "Microsoft.Targeting",
          "Parameters": {
            "Audience": {
              "Users": ["user1@example.com"],
              "Groups": [
                {
                  "Name": "BetaTesters",
                  "RolloutPercentage": 100
                }
              ],
              "DefaultRolloutPercentage": 0
            }
          }
        }
      ],
      "Variants": [
        {
          "Name": "smtp",
          "ConfigurationValue": "smtp"
        },
        {
          "Name": "sendgrid",
          "ConfigurationValue": "sendgrid"
        },
        {
          "Name": "mailgun",
          "ConfigurationValue": "mailgun"
        },
        {
          "Name": "ses",
          "ConfigurationValue": "ses"
        }
      ],
      "Allocation": {
        "DefaultWhenEnabled": "sendgrid",
        "User": [
          {
            "Variant": "ses",
            "Users": ["alice@example.com"]
          }
        ],
        "Group": [
          {
            "Variant": "mailgun",
            "Groups": ["BetaTesters"],
            "RolloutPercentage": 50
          },
          {
            "Variant": "sendgrid",
            "Groups": ["BetaTesters"],
            "RolloutPercentage": 50
          }
        ]
      }
    }
  }
}
```

### Program.cs

```csharp
using ExperimentFramework;
using Microsoft.FeatureManagement;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddFeatureManagement();

// Register email sender implementations
builder.Services.AddScoped<SmtpSender>();
builder.Services.AddScoped<SendGridSender>();
builder.Services.AddScoped<MailgunSender>();
builder.Services.AddScoped<AmazonSesSender>();

builder.Services.AddScoped<IEmailSender, SmtpSender>();

// Define experiment with variant feature flag
var experiments = ExperimentFrameworkBuilder.Create()
    .Trial<IEmailSender>(t => t
        .UsingVariantFeatureFlag("EmailProvider")
        .AddControl<SmtpSender>("smtp")
        .AddVariant<SendGridSender>("sendgrid")
        .AddVariant<MailgunSender>("mailgun")
        .AddVariant<AmazonSesSender>("ses"));

builder.Services.AddExperimentFramework(experiments);

var app = builder.Build();

app.MapPost("/send-email", async (IEmailSender emailSender, EmailRequest request) =>
{
    await emailSender.SendAsync(request.To, request.Subject, request.Body);
    return Results.Ok(new { Message = "Email sent successfully" });
});

app.Run();

public record EmailRequest(string To, string Subject, string Body);
```

## Custom Naming Convention

Example using a custom naming convention for feature flags.

### CustomNamingConvention.cs

```csharp
public class CustomNamingConvention : IExperimentNamingConvention
{
    public string FeatureFlagNameFor(Type serviceType)
    {
        // Convert IMyService -> my-service
        return ToKebabCase(RemoveInterfacePrefix(serviceType.Name));
    }

    public string VariantFlagNameFor(Type serviceType)
    {
        return $"{ToKebabCase(RemoveInterfacePrefix(serviceType.Name))}-variants";
    }

    public string ConfigurationKeyFor(Type serviceType)
    {
        return $"experiments:{ToKebabCase(RemoveInterfacePrefix(serviceType.Name))}";
    }

    private static string RemoveInterfacePrefix(string name)
    {
        if (name.StartsWith("I") && name.Length > 1 && char.IsUpper(name[1]))
        {
            return name.Substring(1);
        }
        return name;
    }

    private static string ToKebabCase(string input)
    {
        var result = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            if (i > 0 && char.IsUpper(c))
            {
                result.Append('-');
            }
            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }
}
```

### Usage

```csharp
var experiments = ExperimentFrameworkBuilder.Create()
    .UseNamingConvention(new CustomNamingConvention())
    .Trial<IPaymentProcessor>(t => t
        .UsingFeatureFlag()  // Uses "payment-processor" from convention
        .AddControl<StripePayment>("false")
        .AddVariant<NewPaymentProvider>("true"));
```

### appsettings.json

```json
{
  "FeatureManagement": {
    "payment-processor": false,
    "database": true,
    "cache": false
  }
}
```

## Testing Experiments

Example of testing experiments with different configurations.

### ExperimentTests.cs

```csharp
using ExperimentFramework;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using Xunit;

public class ExperimentTests
{
    [Theory]
    [InlineData(true, typeof(CloudDatabase))]
    [InlineData(false, typeof(LocalDatabase))]
    public async Task Database_experiment_selects_correct_condition(
        bool featureEnabled,
        Type expectedType)
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloudDb"] = featureEnabled.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<IDatabase, LocalDatabase>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Trial<IDatabase>(t => t
                .UsingFeatureFlag("UseCloudDb")
                .AddControl<LocalDatabase>("false")
                .AddVariant<CloudDatabase>("true"));

        services.AddExperimentFramework(experiments);

        var serviceProvider = services.BuildServiceProvider();

        // Act
        using var scope = serviceProvider.CreateScope();
        var database = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var connectionString = await database.GetConnectionStringAsync();

        // Assert
        Assert.Contains(expectedType.Name, connectionString);
    }

    [Fact]
    public async Task Sticky_routing_assigns_consistent_conditions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IExperimentIdentityProvider>(_ =>
            new FixedIdentityProvider("user-123"));

        services.AddScoped<ContentBased>();
        services.AddScoped<CollaborativeFiltering>();
        services.AddScoped<IRecommendationEngine, ContentBased>();

        var experiments = ExperimentFrameworkBuilder.Create()
            .Trial<IRecommendationEngine>(t => t
                .UsingStickyRouting("RecommendationExperiment")
                .AddControl<ContentBased>("control")
                .AddVariant<CollaborativeFiltering>("variant-a"));

        services.AddExperimentFramework(experiments);

        var serviceProvider = services.BuildServiceProvider();

        // Act - Multiple invocations
        var results = new List<IEnumerable<string>>();
        for (int i = 0; i < 5; i++)
        {
            using var scope = serviceProvider.CreateScope();
            var engine = scope.ServiceProvider.GetRequiredService<IRecommendationEngine>();
            results.Add(await engine.GetRecommendationsAsync("product-1"));
        }

        // Assert - All results should be identical
        var first = results[0];
        Assert.All(results, r => Assert.Equal(first, r));
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

## Next Steps

- [Getting Started](getting-started.md) - Build your first experiment
- [Core Concepts](core-concepts.md) - Understand framework fundamentals
- [Advanced Topics](advanced.md) - Explore custom decorators and patterns
