# Admin API

The Admin package provides RESTful API endpoints for managing experiments at runtime. This enables operational dashboards, monitoring tools, and administrative interfaces.

## Installation

```bash
dotnet add package ExperimentFramework.Admin
```

## Quick Start

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register experiment services
builder.Services.AddExperimentFramework(experiments);
builder.Services.AddExperimentAdmin();

var app = builder.Build();

// Map admin endpoints
app.MapExperimentAdminApi();

app.Run();
```

## API Endpoints

### List All Experiments

```http
GET /api/experiments
```

**Response:**
```json
{
  "experiments": [
    {
      "name": "IPaymentProcessor",
      "serviceType": "IPaymentProcessor",
      "isActive": true,
      "trialCount": 3
    },
    {
      "name": "ISearchService",
      "serviceType": "ISearchService",
      "isActive": true,
      "trialCount": 2
    }
  ]
}
```

### Get Experiment Details

```http
GET /api/experiments/{name}
```

**Response:**
```json
{
  "name": "IPaymentProcessor",
  "serviceType": "IPaymentProcessor",
  "isActive": true,
  "trials": [
    {
      "key": "stripe",
      "implementationType": "StripePaymentProcessor",
      "isControl": true
    },
    {
      "key": "adyen",
      "implementationType": "AdyenPaymentProcessor",
      "isControl": false
    },
    {
      "key": "braintree",
      "implementationType": "BraintreePaymentProcessor",
      "isControl": false
    }
  ]
}
```

### Get Experiment Status

```http
GET /api/experiments/{name}/status
```

**Response:**
```json
{
  "name": "IPaymentProcessor",
  "isActive": true,
  "status": "Active"
}
```

### Toggle Experiment

```http
POST /api/experiments/{name}/toggle
```

**Response:**
```json
{
  "name": "IPaymentProcessor",
  "isActive": false,
  "status": "Inactive"
}
```

## Configuration

### Custom Route Prefix

```csharp
app.MapExperimentAdminApi("/admin/experiments");
```

### Adding Authentication

```csharp
app.MapExperimentAdminApi()
    .RequireAuthorization("AdminPolicy");
```

### Adding Rate Limiting

```csharp
app.MapExperimentAdminApi()
    .RequireRateLimiting("AdminRateLimit");
```

### Full Configuration Example

```csharp
var group = app.MapExperimentAdminApi("/api/v1/experiments");

group.RequireAuthorization(policy =>
{
    policy.RequireRole("Admin", "ExperimentManager");
});

group.AddEndpointFilter<AuditLoggingFilter>();

group.WithOpenApi(operation =>
{
    operation.Tags = new[] { new OpenApiTag { Name = "Experiment Administration" } };
    return operation;
});
```

## Experiment Registry

The Admin API requires an `IExperimentRegistry` to query experiment information:

### Built-in Registry

The framework provides a default registry populated from your experiment definitions:

```csharp
services.AddExperimentFramework(experiments);
services.AddExperimentAdmin(); // Registers the default registry
```

### Custom Registry

Implement `IExperimentRegistry` for custom behavior:

```csharp
public interface IExperimentRegistry
{
    IEnumerable<ExperimentInfo> GetAllExperiments();
    ExperimentInfo? GetExperiment(string name);
}

public interface IMutableExperimentRegistry : IExperimentRegistry
{
    void SetExperimentActive(string name, bool isActive);
}
```

#### Database-Backed Registry

```csharp
public class DatabaseExperimentRegistry : IMutableExperimentRegistry
{
    private readonly ExperimentDbContext _dbContext;

    public IEnumerable<ExperimentInfo> GetAllExperiments()
    {
        return _dbContext.Experiments
            .Select(e => new ExperimentInfo
            {
                Name = e.Name,
                ServiceType = Type.GetType(e.ServiceTypeName),
                IsActive = e.IsActive,
                Trials = e.Trials.Select(t => new TrialInfo
                {
                    Key = t.Key,
                    ImplementationType = Type.GetType(t.ImplementationTypeName),
                    IsControl = t.IsControl
                }).ToList()
            })
            .ToList();
    }

    public ExperimentInfo? GetExperiment(string name)
    {
        var entity = _dbContext.Experiments
            .Include(e => e.Trials)
            .FirstOrDefault(e => e.Name == name);

        if (entity == null) return null;

        return MapToInfo(entity);
    }

    public void SetExperimentActive(string name, bool isActive)
    {
        var experiment = _dbContext.Experiments.Find(name);
        if (experiment != null)
        {
            experiment.IsActive = isActive;
            _dbContext.SaveChanges();
        }
    }
}

// Register
services.AddScoped<IExperimentRegistry, DatabaseExperimentRegistry>();
services.AddScoped<IMutableExperimentRegistry, DatabaseExperimentRegistry>();
```

## OpenAPI Integration

Add OpenAPI documentation for the admin endpoints:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Experiment Admin API",
        Version = "v1"
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapExperimentAdminApi();
```

## Dashboard Integration

### Minimal Dashboard Example

```html
<!DOCTYPE html>
<html>
<head>
    <title>Experiment Dashboard</title>
    <style>
        .experiment { padding: 10px; margin: 10px; border: 1px solid #ccc; }
        .active { background-color: #e8f5e9; }
        .inactive { background-color: #ffebee; }
        .toggle-btn { padding: 5px 10px; cursor: pointer; }
    </style>
</head>
<body>
    <h1>Experiment Dashboard</h1>
    <div id="experiments"></div>

    <script>
        async function loadExperiments() {
            const response = await fetch('/api/experiments');
            const data = await response.json();

            const container = document.getElementById('experiments');
            container.innerHTML = data.experiments.map(exp => `
                <div class="experiment ${exp.isActive ? 'active' : 'inactive'}">
                    <h3>${exp.name}</h3>
                    <p>Service: ${exp.serviceType}</p>
                    <p>Variants: ${exp.trialCount}</p>
                    <p>Status: ${exp.isActive ? 'Active' : 'Inactive'}</p>
                    <button class="toggle-btn" onclick="toggle('${exp.name}')">
                        ${exp.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                </div>
            `).join('');
        }

        async function toggle(name) {
            await fetch(`/api/experiments/${name}/toggle`, { method: 'POST' });
            loadExperiments();
        }

        loadExperiments();
    </script>
</body>
</html>
```

### React Component Example

```tsx
import { useState, useEffect } from 'react';

interface Experiment {
    name: string;
    serviceType: string;
    isActive: boolean;
    trialCount: number;
}

export function ExperimentDashboard() {
    const [experiments, setExperiments] = useState<Experiment[]>([]);

    useEffect(() => {
        fetchExperiments();
    }, []);

    async function fetchExperiments() {
        const response = await fetch('/api/experiments');
        const data = await response.json();
        setExperiments(data.experiments);
    }

    async function toggleExperiment(name: string) {
        await fetch(`/api/experiments/${name}/toggle`, { method: 'POST' });
        fetchExperiments();
    }

    return (
        <div>
            <h1>Experiments</h1>
            {experiments.map(exp => (
                <div key={exp.name} className={`experiment ${exp.isActive ? 'active' : 'inactive'}`}>
                    <h3>{exp.name}</h3>
                    <p>Service: {exp.serviceType}</p>
                    <p>Variants: {exp.trialCount}</p>
                    <button onClick={() => toggleExperiment(exp.name)}>
                        {exp.isActive ? 'Deactivate' : 'Activate'}
                    </button>
                </div>
            ))}
        </div>
    );
}
```

## Extending the API

### Adding Custom Endpoints

```csharp
var group = app.MapExperimentAdminApi();

// Add custom metrics endpoint
group.MapGet("/{name}/metrics", async (string name, IMetricsService metrics) =>
{
    var experimentMetrics = await metrics.GetExperimentMetricsAsync(name);
    return Results.Ok(experimentMetrics);
});

// Add assignment preview
group.MapPost("/{name}/preview", async (
    string name,
    PreviewRequest request,
    IExperimentSelector selector) =>
{
    var variant = await selector.PreviewSelectionAsync(name, request.UserId);
    return Results.Ok(new { variant });
});
```

### Custom Filters

```csharp
public class AuditLoggingFilter : IEndpointFilter
{
    private readonly IAuditSink _auditSink;

    public AuditLoggingFilter(IAuditSink auditSink)
    {
        _auditSink = auditSink;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var result = await next(context);

        // Log admin action
        await _auditSink.RecordAsync(new AuditEvent
        {
            EventType = "AdminAction",
            ExperimentName = context.HttpContext.Request.RouteValues["name"]?.ToString() ?? "",
            Timestamp = DateTimeOffset.UtcNow,
            UserId = context.HttpContext.User.Identity?.Name,
            Metadata = new Dictionary<string, object>
            {
                ["Method"] = context.HttpContext.Request.Method,
                ["Path"] = context.HttpContext.Request.Path.Value ?? ""
            }
        });

        return result;
    }
}
```

## Best Practices

1. **Secure the endpoints**: Always add authentication/authorization
2. **Audit all changes**: Log who made changes and when
3. **Use HTTPS**: Admin endpoints should only be accessible over HTTPS
4. **Rate limit**: Protect against abuse
5. **Test toggle behavior**: Ensure toggling experiments doesn't cause issues

## Troubleshooting

### Toggle not working

**Symptom**: POST to toggle returns success but experiment state unchanged.

**Cause**: Registry is not mutable.

**Solution**: Implement `IMutableExperimentRegistry`:

```csharp
// The default in-memory registry is immutable
// Implement a mutable registry for runtime changes
services.AddScoped<IMutableExperimentRegistry, MutableExperimentRegistry>();
```

### Experiments not appearing

**Symptom**: GET /api/experiments returns empty list.

**Cause**: Registry not registered or not populated.

**Solution**: Ensure `AddExperimentAdmin()` is called after `AddExperimentFramework()`:

```csharp
services.AddExperimentFramework(experiments);  // First
services.AddExperimentAdmin();                  // After
```

### 404 on endpoints

**Symptom**: All admin endpoints return 404.

**Cause**: Endpoints not mapped.

**Solution**: Call `MapExperimentAdminApi()` in the app configuration:

```csharp
var app = builder.Build();
app.MapExperimentAdminApi();  // Don't forget this!
app.Run();
```
