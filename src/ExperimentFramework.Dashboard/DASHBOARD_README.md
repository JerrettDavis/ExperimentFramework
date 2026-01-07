# ExperimentFramework.Dashboard

Enterprise-grade, embeddable web dashboard for managing A/B tests, feature flags, and experiments in .NET applications.

## Features

- **Experiment Management**: Create, configure, and manage experiments with an intuitive web UI
- **Analytics Dashboard**: Visualize experiment performance and statistical analysis
- **Governance Workflows**: Approval workflows, lifecycle management, versioning, and audit trails
- **Rollout Management**: Gradual rollout with stage-based deployment
- **Targeting Rules**: Advanced user targeting and segmentation
- **Configuration DSL**: YAML-based configuration with Monaco editor
- **Plugin System**: Discover and manage experiment plugins
- **Multi-Tenancy**: Built-in support for multi-tenant scenarios via `ITenantResolver`
- **Authorization**: Delegates to ASP.NET Core authorization (no user management)

## Installation

```bash
dotnet add package ExperimentFramework.Dashboard
```

## Quick Start

### 1. Add Dashboard Services

```csharp
using ExperimentFramework.Dashboard;

var builder = WebApplication.CreateBuilder(args);

// Configure dashboard
builder.Services.AddExperimentDashboard(options =>
{
    options.PathBase = "/dashboard";
    options.Title = "My Experiments";
    options.EnableAnalytics = true;
    options.EnableGovernanceUI = true;
});
```

### 2. Map Dashboard Endpoint

```csharp
var app = builder.Build();

// Map dashboard at /dashboard
app.MapExperimentDashboard("/dashboard");

app.Run();
```

### 3. Access Dashboard

Navigate to `https://localhost:5001/dashboard` to access the dashboard.

## Configuration Options

```csharp
builder.Services.AddExperimentDashboard(options =>
{
    // Basic settings
    options.PathBase = "/dashboard";
    options.Title = "Experiment Dashboard";
    options.ItemsPerPage = 25;

    // Features
    options.EnableAnalytics = true;
    options.EnableGovernanceUI = true;

    // Authorization
    options.RequireAuthorization = true;
    options.AuthorizationPolicy = "DashboardAccess";

    // Multi-tenancy
    options.TenantResolver = new HttpHeaderTenantResolver("X-Tenant-Id");
});
```

## Multi-Tenancy

Configure tenant resolution using built-in or custom resolvers:

```csharp
// HTTP Header
options.TenantResolver = new HttpHeaderTenantResolver("X-Tenant-Id");

// Subdomain
options.TenantResolver = new SubdomainTenantResolver();

// Claims
options.TenantResolver = new ClaimTenantResolver("tenant_id");

// Composite (try multiple strategies)
options.TenantResolver = new CompositeTenantResolver(
    new ClaimTenantResolver("tenant_id"),
    new HttpHeaderTenantResolver("X-Tenant-Id")
);
```

## Authorization

The dashboard delegates authorization to your existing ASP.NET Core setup:

```csharp
// Configure authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("DashboardAccess", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin", "Experimenter");
    });
});

// Enable in dashboard
builder.Services.AddExperimentDashboard(options =>
{
    options.RequireAuthorization = true;
    options.AuthorizationPolicy = "DashboardAccess";
});

// Use authentication middleware
app.UseAuthentication();
app.UseAuthorization();
```

## Extensibility

### Custom Tenant Resolver

```csharp
public class CustomTenantResolver : ITenantResolver
{
    public Task<TenantContext?> ResolveAsync(HttpContext context)
    {
        // Your custom logic
        var tenantId = /* ... */;

        return Task.FromResult(new TenantContext
        {
            TenantId = tenantId,
            DisplayName = "My Tenant",
            Environment = "Production"
        });
    }
}
```

### Custom Data Provider

```csharp
public class CustomDataProvider : IDashboardDataProvider
{
    public Task<IEnumerable<ExperimentInfo>> GetExperimentsAsync(
        string? tenantId,
        CancellationToken ct = default)
    {
        // Your custom data access logic
    }
}

// Register
builder.Services.AddSingleton<IDashboardDataProvider, CustomDataProvider>();
```

## API Endpoints

The dashboard exposes REST APIs at `/dashboard/api`:

- `GET /api/experiments` - List all experiments
- `GET /api/experiments/{name}` - Get experiment details
- `POST /api/experiments/{name}/toggle` - Toggle experiment active state
- `GET /api/configuration/yaml` - Export configuration as YAML
- `GET /api/governance/{name}/state` - Get governance state
- `GET /api/analytics/{name}/statistics` - Get statistical analysis
- `GET /api/rollout/{name}/stages` - Get rollout stages
- `GET /api/targeting/{name}/rules` - Get targeting rules

## Requirements

- .NET 10.0 or later
- ASP.NET Core
- ExperimentFramework package

## License

MIT

## Links

- [GitHub Repository](https://github.com/yourusername/ExperimentFramework)
- [Documentation](https://github.com/yourusername/ExperimentFramework/wiki)
- [Sample Application](https://github.com/yourusername/ExperimentFramework/tree/main/samples/ExperimentFramework.DashboardHost)
