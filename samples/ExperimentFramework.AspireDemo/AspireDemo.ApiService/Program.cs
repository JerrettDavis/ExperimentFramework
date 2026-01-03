using System.Collections.Concurrent;
using AspireDemo.ApiService.Models;
using AspireDemo.ApiService.Services;
using ExperimentFramework;
using ExperimentFramework.Audit;
using ExperimentFramework.KillSwitch;
using ExperimentFramework.Models;
using ExperimentFramework.Targeting;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Experiment state management (in-memory for demo)
var killSwitch = new InMemoryKillSwitchProvider();
builder.Services.AddSingleton(killSwitch);
builder.Services.AddSingleton<IKillSwitchProvider>(killSwitch);
builder.Services.AddSingleton<ExperimentStateManager>();
builder.Services.AddSingleton<PluginStateManager>();
builder.Services.AddSingleton<RuntimeExperimentManager>();
builder.Services.AddSingleton<FeatureAuditService>();

// Register all experiment implementations
builder.Services.AddSingleton<TieredPricing>();
builder.Services.AddSingleton<FlatPricing>();
builder.Services.AddSingleton<VolumePricing>();
builder.Services.AddSingleton<StandardNotifications>();
builder.Services.AddSingleton<PersonalizedNotifications>();
builder.Services.AddSingleton<BasicRecommendations>();
builder.Services.AddSingleton<AiRecommendations>();
builder.Services.AddSingleton<CollaborativeRecommendations>();
builder.Services.AddSingleton<LightTheme>();
builder.Services.AddSingleton<DarkTheme>();
builder.Services.AddSingleton<SystemTheme>();

// Default interface registrations
builder.Services.AddSingleton<IPricingStrategy, TieredPricing>();
builder.Services.AddSingleton<INotificationService, StandardNotifications>();
builder.Services.AddSingleton<IRecommendationEngine, BasicRecommendations>();
builder.Services.AddSingleton<IThemeProvider, LightTheme>();

// Configure ExperimentFramework
builder.Services.AddExperimentTargeting();
builder.Services.AddExperimentAuditLogging();

// In-memory audit sink for demo
builder.Services.AddSingleton<InMemoryAuditSink>();
builder.Services.AddSingleton<IAuditSink>(sp => sp.GetRequiredService<InMemoryAuditSink>());

var experiments = ExperimentFrameworkBuilder.Create()
    .UseDispatchProxy()
    .WithTimeout(TimeSpan.FromSeconds(2), TimeoutAction.FallbackToDefault)
    .WithKillSwitch(killSwitch)
    .Define<IPricingStrategy>(c => c
        .UsingConfigurationKey("Experiments:Pricing")
        .AddDefaultTrial<TieredPricing>("tiered")
        .AddTrial<FlatPricing>("flat")
        .AddTrial<VolumePricing>("volume")
        .OnErrorRedirectAndReplayDefault())
    .Define<INotificationService>(c => c
        .UsingConfigurationKey("Experiments:Notifications")
        .AddDefaultTrial<StandardNotifications>("standard")
        .AddTrial<PersonalizedNotifications>("personalized")
        .OnErrorRedirectAndReplayDefault())
    .Define<IRecommendationEngine>(c => c
        .UsingConfigurationKey("Experiments:Recommendations")
        .AddDefaultTrial<BasicRecommendations>("basic")
        .AddTrial<AiRecommendations>("ai-powered")
        .AddTrial<CollaborativeRecommendations>("collaborative")
        .OnErrorRedirectAndReplayDefault())
    .Define<IThemeProvider>(c => c
        .UsingConfigurationKey("Experiments:Theme")
        .AddDefaultTrial<LightTheme>("light")
        .AddTrial<DarkTheme>("dark")
        .AddTrial<SystemTheme>("system")
        .OnErrorRedirectAndReplayDefault());

builder.Services.AddExperimentFramework(experiments);

var app = builder.Build();

app.UseCors();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ============================================================================
// Experiment Management Endpoints
// ============================================================================

app.MapGet("/", () => "ExperimentFramework Aspire Demo API");

app.MapGet("/api/experiments", (ExperimentStateManager state) =>
{
    return Results.Ok(state.GetAllExperiments());
})
.WithName("GetExperiments")
.WithTags("Experiments");

app.MapGet("/api/experiments/{name}", (string name, ExperimentStateManager state) =>
{
    var experiment = state.GetExperiment(name);
    return experiment is null ? Results.NotFound() : Results.Ok(experiment);
})
.WithName("GetExperiment")
.WithTags("Experiments");

app.MapPost("/api/experiments/{name}/activate/{variant}", async (string name, string variant, ExperimentStateManager state, IConfiguration config, InMemoryAuditSink auditSink) =>
{
    var previousVariant = state.GetActiveVariant(name);
    var success = state.SetActiveVariant(name, variant);
    if (!success) return Results.BadRequest(new { Error = "Invalid experiment or variant" });

    // Update configuration (in real app, this would persist)
    config[$"Experiments:{GetConfigKey(name)}"] = variant;

    // Record audit event
    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentModified,
        ExperimentName = name,
        SelectedTrialKey = variant,
        Details = new Dictionary<string, object>
        {
            ["previousVariant"] = previousVariant,
            ["newVariant"] = variant
        }
    });

    return Results.Ok(state.GetExperiment(name));
})
.WithName("ActivateVariant")
.WithTags("Experiments");

// ============================================================================
// Experiment Demo Endpoints
// ============================================================================

app.MapGet("/api/pricing/calculate", async (int units, ExperimentStateManager state, PluginStateManager plugins, InMemoryAuditSink auditSink) =>
{
    // Check if a plugin implementation is active for IPricingStrategy
    var pluginImpl = plugins.GetActiveImplementation("IPricingStrategy");
    if (pluginImpl != null)
    {
        // Simulate plugin pricing calculation
        var pluginResult = SimulatePluginPricing(pluginImpl.ImplementationType, units);
        state.RecordUsage("pricing-strategy", $"plugin:{pluginImpl.Alias ?? pluginImpl.ImplementationType}");

        await auditSink.RecordAsync(new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = AuditEventType.VariantSelected,
            ExperimentName = pluginImpl.PluginId,
            SelectedTrialKey = pluginImpl.Alias ?? pluginImpl.ImplementationType,
            ServiceType = "IPricingStrategy"
        });

        return Results.Ok(new
        {
            pluginResult.Strategy,
            pluginResult.UnitPrice,
            pluginResult.Total,
            Units = units,
            Variant = $"plugin:{pluginImpl.Alias ?? pluginImpl.ImplementationType}",
            PluginId = pluginImpl.PluginId,
            IsPlugin = true,
            Timestamp = DateTime.UtcNow
        });
    }

    var activeVariant = state.GetActiveVariant("pricing-strategy");
    IPricingStrategy pricing = activeVariant switch
    {
        "flat" => app.Services.GetRequiredService<FlatPricing>(),
        "volume" => app.Services.GetRequiredService<VolumePricing>(),
        _ => app.Services.GetRequiredService<TieredPricing>()
    };

    var result = pricing.Calculate(units);
    state.RecordUsage("pricing-strategy", activeVariant);

    // Record audit event
    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.VariantSelected,
        ExperimentName = "pricing-strategy",
        SelectedTrialKey = activeVariant,
        ServiceType = nameof(IPricingStrategy)
    });

    return Results.Ok(new
    {
        result.Strategy,
        result.UnitPrice,
        result.Total,
        Units = units,
        Variant = activeVariant,
        IsPlugin = false,
        Timestamp = DateTime.UtcNow
    });
})
.WithName("CalculatePricing")
.WithTags("Demo");

app.MapGet("/api/notifications/preview", async (string? userId, ExperimentStateManager state, InMemoryAuditSink auditSink) =>
{
    var activeVariant = state.GetActiveVariant("notification-style");
    INotificationService notifications = activeVariant switch
    {
        "personalized" => app.Services.GetRequiredService<PersonalizedNotifications>(),
        _ => app.Services.GetRequiredService<StandardNotifications>()
    };

    var result = notifications.GetWelcomeMessage(userId ?? "User");
    state.RecordUsage("notification-style", activeVariant);

    // Record audit event
    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.VariantSelected,
        ExperimentName = "notification-style",
        SelectedTrialKey = activeVariant,
        ServiceType = nameof(INotificationService)
    });

    return Results.Ok(new
    {
        result.Title,
        result.Message,
        result.Style,
        Variant = activeVariant,
        Timestamp = DateTime.UtcNow
    });
})
.WithName("PreviewNotification")
.WithTags("Demo");

app.MapGet("/api/recommendations", async (string? userId, ExperimentStateManager state, PluginStateManager plugins, InMemoryAuditSink auditSink) =>
{
    // Check if a plugin implementation is active for IRecommendationEngine
    var pluginImpl = plugins.GetActiveImplementation("IRecommendationEngine");
    if (pluginImpl != null)
    {
        // Simulate plugin recommendation
        var pluginResult = SimulatePluginRecommendations(pluginImpl.ImplementationType, userId ?? "anonymous");
        state.RecordUsage("recommendation-algorithm", $"plugin:{pluginImpl.Alias ?? pluginImpl.ImplementationType}");

        await auditSink.RecordAsync(new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = AuditEventType.VariantSelected,
            ExperimentName = pluginImpl.PluginId,
            SelectedTrialKey = pluginImpl.Alias ?? pluginImpl.ImplementationType,
            ServiceType = "IRecommendationEngine"
        });

        return Results.Ok(new
        {
            pluginResult.Algorithm,
            pluginResult.Items,
            pluginResult.Confidence,
            Variant = $"plugin:{pluginImpl.Alias ?? pluginImpl.ImplementationType}",
            PluginId = pluginImpl.PluginId,
            IsPlugin = true,
            Timestamp = DateTime.UtcNow
        });
    }

    var activeVariant = state.GetActiveVariant("recommendation-algorithm");
    IRecommendationEngine engine = activeVariant switch
    {
        "ai-powered" => app.Services.GetRequiredService<AiRecommendations>(),
        "collaborative" => app.Services.GetRequiredService<CollaborativeRecommendations>(),
        _ => app.Services.GetRequiredService<BasicRecommendations>()
    };

    var result = engine.GetRecommendations(userId ?? "anonymous");
    state.RecordUsage("recommendation-algorithm", activeVariant);

    // Record audit event
    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.VariantSelected,
        ExperimentName = "recommendation-algorithm",
        SelectedTrialKey = activeVariant,
        ServiceType = nameof(IRecommendationEngine)
    });

    return Results.Ok(new
    {
        result.Algorithm,
        result.Items,
        result.Confidence,
        Variant = activeVariant,
        IsPlugin = false,
        Timestamp = DateTime.UtcNow
    });
})
.WithName("GetRecommendations")
.WithTags("Demo");

app.MapGet("/api/theme", async (ExperimentStateManager state, InMemoryAuditSink auditSink) =>
{
    var activeVariant = state.GetActiveVariant("ui-theme");
    IThemeProvider theme = activeVariant switch
    {
        "dark" => app.Services.GetRequiredService<DarkTheme>(),
        "system" => app.Services.GetRequiredService<SystemTheme>(),
        _ => app.Services.GetRequiredService<LightTheme>()
    };

    var result = theme.GetTheme();
    state.RecordUsage("ui-theme", activeVariant);

    // Record audit event
    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.VariantSelected,
        ExperimentName = "ui-theme",
        SelectedTrialKey = activeVariant,
        ServiceType = nameof(IThemeProvider)
    });

    return Results.Ok(new
    {
        result.Name,
        result.PrimaryColor,
        result.BackgroundColor,
        result.TextColor,
        result.AccentColor,
        Variant = activeVariant,
        Timestamp = DateTime.UtcNow
    });
})
.WithName("GetTheme")
.WithTags("Demo");

// ============================================================================
// Audit & Analytics Endpoints
// ============================================================================

app.MapGet("/api/audit", (InMemoryAuditSink auditSink, int? limit) =>
{
    var events = auditSink.GetEvents(limit ?? 100);
    return Results.Ok(events);
})
.WithName("GetAuditLog")
.WithTags("Analytics");

app.MapGet("/api/analytics/usage", (ExperimentStateManager state) =>
{
    return Results.Ok(state.GetUsageStats());
})
.WithName("GetUsageStats")
.WithTags("Analytics");

// ============================================================================
// Plugin Endpoints
// ============================================================================

app.MapGet("/api/plugins", (PluginStateManager plugins) =>
{
    return Results.Ok(plugins.GetAllPlugins());
})
.WithName("GetPlugins")
.WithTags("Plugins");

app.MapPost("/api/plugins/discover", async (PluginStateManager plugins, InMemoryAuditSink auditSink) =>
{
    var count = plugins.DiscoverPlugins();
    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentCreated,
        ExperimentName = "PluginSystem",
        SelectedTrialKey = $"Discovered {count} plugins"
    });
    return Results.Ok(new { LoadedCount = count });
})
.WithName("DiscoverPlugins")
.WithTags("Plugins");

app.MapPost("/api/plugins/{pluginId}/reload", async (string pluginId, PluginStateManager plugins, InMemoryAuditSink auditSink) =>
{
    var success = plugins.ReloadPlugin(pluginId);
    if (!success) return Results.NotFound();

    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentModified,
        ExperimentName = pluginId,
        SelectedTrialKey = "Plugin Reloaded"
    });
    return Results.Ok();
})
.WithName("ReloadPlugin")
.WithTags("Plugins");

app.MapDelete("/api/plugins/{pluginId}", async (string pluginId, PluginStateManager plugins, InMemoryAuditSink auditSink) =>
{
    var success = plugins.UnloadPlugin(pluginId);
    if (!success) return Results.NotFound();

    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentStopped,
        ExperimentName = pluginId,
        SelectedTrialKey = "Plugin Unloaded"
    });
    return Results.Ok();
})
.WithName("UnloadPlugin")
.WithTags("Plugins");

app.MapPost("/api/plugins/{pluginId}/use", async (string pluginId, string @interface, string impl, PluginStateManager plugins, InMemoryAuditSink auditSink) =>
{
    // Find the plugin and implementation
    var plugin = plugins.GetAllPlugins().FirstOrDefault(p => p.Id == pluginId);
    if (plugin == null) return Results.NotFound(new { Error = "Plugin not found" });

    var service = plugin.Services.FirstOrDefault(s => s.Interface == @interface);
    if (service == null) return Results.NotFound(new { Error = "Service interface not found in plugin" });

    var implementation = service.Implementations.FirstOrDefault(i => i.Alias == impl || i.Type == impl);
    if (implementation == null) return Results.NotFound(new { Error = "Implementation not found" });

    // Register as active implementation
    plugins.SetActiveImplementation(@interface, pluginId, implementation.Type, implementation.Alias);

    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.VariantSelected,
        ExperimentName = pluginId,
        SelectedTrialKey = implementation.Alias ?? implementation.Type,
        ServiceType = @interface
    });

    return Results.Ok(new {
        PluginId = pluginId,
        Interface = @interface,
        Implementation = implementation.Type,
        Alias = implementation.Alias,
        Active = true
    });
})
.WithName("UsePluginImplementation")
.WithTags("Plugins");

app.MapGet("/api/plugins/active", (PluginStateManager plugins) =>
{
    return Results.Ok(plugins.GetActiveImplementations());
})
.WithName("GetActivePluginImplementations")
.WithTags("Plugins");

app.MapDelete("/api/plugins/active/{interface}", async (string @interface, PluginStateManager plugins, InMemoryAuditSink auditSink) =>
{
    var cleared = plugins.ClearActiveImplementation(@interface);
    if (!cleared) return Results.NotFound();

    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentStopped,
        ExperimentName = "PluginSystem",
        SelectedTrialKey = $"Cleared {@interface}",
        ServiceType = @interface
    });

    return Results.Ok(new { Interface = @interface, Cleared = true });
})
.WithName("ClearActivePluginImplementation")
.WithTags("Plugins");

// ============================================================================
// Configuration & DSL Endpoints
// ============================================================================

app.MapGet("/api/config/yaml", (RuntimeExperimentManager dslManager) =>
{
    var yaml = dslManager.ExportCurrentConfiguration();
    return Results.Text(yaml, "text/yaml");
})
.WithName("GetConfigYaml")
.WithTags("Configuration");

app.MapGet("/api/config/info", (ExperimentStateManager state, FeatureAuditService featureAudit) =>
{
    var info = new
    {
        Framework = new
        {
            Name = "ExperimentFramework",
            Version = "1.0.0-preview",
            Runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ProxyType = "DispatchProxy (Runtime)",
        },
        Server = new
        {
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
            StartTime = DateTime.UtcNow.AddMilliseconds(-Environment.TickCount64),
            UpTime = TimeSpan.FromMilliseconds(Environment.TickCount64).ToString(@"d\.hh\:mm\:ss"),
        },
        Experiments = new
        {
            Total = state.GetAllExperiments().Count(),
            Active = state.GetAllExperiments().Count(e => e.Status == "Active"),
            Categories = state.GetAllExperiments().GroupBy(e => e.Category).ToDictionary(g => g.Key, g => g.Count()),
        },
        Features = featureAudit.GetFeatures()
    };
    return Results.Ok(info);
})
.WithName("GetConfigInfo")
.WithTags("Configuration");

app.MapGet("/api/config/kill-switch", (ExperimentStateManager state) =>
{
    return Results.Ok(state.GetKillSwitchStatuses());
})
.WithName("GetKillSwitch")
.WithTags("Configuration");

app.MapPost("/api/config/kill-switch", (KillSwitchUpdate request, IKillSwitchProvider killSwitch, ExperimentStateManager state) =>
{
    var type = ExperimentTypeResolver.GetServiceType(request.Experiment);

    if (string.IsNullOrWhiteSpace(request.Variant))
    {
        if (request.Disabled)
            killSwitch.DisableExperiment(type);
        else
            killSwitch.EnableExperiment(type);
    }
    else
    {
        if (request.Disabled)
            killSwitch.DisableTrial(type, request.Variant);
        else
            killSwitch.EnableTrial(type, request.Variant);
    }

    return Results.Ok(state.GetKillSwitchStatus(request.Experiment));
})
.WithName("UpdateKillSwitch")
.WithTags("Configuration");

// ============================================================================
// DSL Configuration Endpoints
// ============================================================================

app.MapPost("/api/dsl/validate", (DslRequest request, RuntimeExperimentManager dslManager) =>
{
    var result = dslManager.Validate(request.Yaml);
    return Results.Ok(result);
})
.WithName("ValidateDsl")
.WithTags("DSL");

app.MapPost("/api/dsl/apply", async (DslRequest request, RuntimeExperimentManager dslManager, InMemoryAuditSink auditSink) =>
{
    var result = dslManager.Apply(request.Yaml);

    if (result.Success)
    {
        await auditSink.RecordAsync(new AuditEvent
        {
            EventId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow,
            EventType = AuditEventType.ExperimentModified,
            ExperimentName = "DSL",
            SelectedTrialKey = $"Applied {result.Changes.Count} changes",
            Details = new Dictionary<string, object>
            {
                ["changes"] = result.Changes.Select(c => $"{c.Action}: {c.Name}").ToList()
            }
        });
    }

    return Results.Ok(result);
})
.WithName("ApplyDsl")
.WithTags("DSL");

app.MapGet("/api/dsl/current", (RuntimeExperimentManager dslManager) =>
{
    var yaml = dslManager.ExportCurrentConfiguration();
    var (lastYaml, lastApplied) = dslManager.GetLastApplied();

    return Results.Ok(new
    {
        Yaml = yaml,
        LastApplied = lastApplied,
        HasUnappliedChanges = lastYaml != null && lastYaml != yaml
    });
})
.WithName("GetCurrentDsl")
.WithTags("DSL");

app.MapGet("/api/dsl/schema", () =>
{
    // Return a simplified JSON Schema for the DSL
    var schema = new
    {
        type = "object",
        properties = new
        {
            experiments = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    required = new[] { "name", "trials" },
                    properties = new
                    {
                        name = new { type = "string", description = "Unique experiment identifier" },
                        metadata = new
                        {
                            type = "object",
                            properties = new
                            {
                                displayName = new { type = "string" },
                                description = new { type = "string" },
                                category = new { type = "string" }
                            }
                        },
                        trials = new
                        {
                            type = "array",
                            items = new
                            {
                                type = "object",
                                required = new[] { "serviceType", "control" },
                                properties = new
                                {
                                    serviceType = new { type = "string", description = "Service interface type" },
                                    selectionMode = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            type = new { type = "string", @enum = new[] { "configurationKey", "featureFlag", "rollout", "targeting" } },
                                            key = new { type = "string" }
                                        }
                                    },
                                    control = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            key = new { type = "string" },
                                            typeName = new { type = "string" }
                                        }
                                    },
                                    conditions = new
                                    {
                                        type = "array",
                                        items = new
                                        {
                                            type = "object",
                                            properties = new
                                            {
                                                key = new { type = "string" },
                                                typeName = new { type = "string" },
                                                displayName = new { type = "string" }
                                            }
                                        }
                                    },
                                    errorPolicy = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            type = new { type = "string", @enum = new[] { "throw", "fallbackToControl", "fallbackTo", "tryInOrder" } }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    };

    return Results.Ok(schema);
})
.WithName("GetDslSchema")
.WithTags("DSL");

app.MapDefaultEndpoints();
app.Run();

// ============================================================================
// Helper Methods
// ============================================================================

static string GetConfigKey(string experimentName) => experimentName switch
{
    "pricing-strategy" => "Pricing",
    "notification-style" => "Notifications",
    "recommendation-algorithm" => "Recommendations",
    "ui-theme" => "Theme",
    _ => experimentName
};



static PricingResult SimulatePluginPricing(string implementationType, int units)
{
    return implementationType switch
    {
        "ContractPricing" => new PricingResult("Contract", 5.00m, units * 5.00m, "Enterprise contract rate"),
        "UsageBasedPricing" => new PricingResult("Usage-Based", CalculateUsagePrice(units), units * CalculateUsagePrice(units), "Pay-as-you-go pricing"),
        "HybridPricing" => new PricingResult("Hybrid", 4.50m, (units * 4.50m) + 50m, "Base fee + per-unit pricing"),
        _ => new PricingResult("Plugin Default", 6.00m, units * 6.00m, "Plugin pricing")
    };

    static decimal CalculateUsagePrice(int units) => units switch
    {
        < 50 => 12.00m,
        < 200 => 9.00m,
        < 500 => 6.50m,
        _ => 4.00m
    };
}

static RecommendationResult SimulatePluginRecommendations(string implementationType, string userId)
{
    return implementationType switch
    {
        "DeepLearningRecommender" => new RecommendationResult(
            "Deep Learning (Plugin)",
            ["Premium Gaming Laptop", "VR Headset Pro", "AI-Powered Smart Speaker", "Neural Interface Kit"],
            0.94
        ),
        "NeuralCollaborativeFiltering" => new RecommendationResult(
            "Neural CF (Plugin)",
            ["Ultra-Wide Monitor", "Streaming Deck", "Professional Webcam", "Studio Microphone"],
            0.91
        ),
        _ => new RecommendationResult(
            "Plugin Default",
            ["Recommended Item 1", "Recommended Item 2", "Recommended Item 3"],
            0.85
        )
    };
}


internal static class ExperimentTypeResolver
{
    private static readonly Dictionary<string, Type> ExperimentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pricing-strategy"] = typeof(PricingExperimentMarker),
        ["notification-style"] = typeof(NotificationExperimentMarker),
        ["recommendation-algorithm"] = typeof(RecommendationExperimentMarker),
        ["ui-theme"] = typeof(ThemeExperimentMarker)
    };

    public static Type GetServiceType(string experimentName) =>
        ExperimentTypes.TryGetValue(experimentName, out var type)
            ? type
            : typeof(DslExperimentMarker);

    private sealed class PricingExperimentMarker;
    private sealed class NotificationExperimentMarker;
    private sealed class RecommendationExperimentMarker;
    private sealed class ThemeExperimentMarker;
    private sealed class DslExperimentMarker;
}

// ============================================================================
// Experiment State Manager
// ============================================================================

public class ExperimentStateManager
{
    private readonly ConcurrentDictionary<string, ExperimentInfo> _experiments = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _usageStats = new();
    private readonly IKillSwitchProvider _killSwitch;

    public bool SupportsFeatureFlags => true;
    public bool SupportsCustomProviders => true;
    public bool HasUsage => _usageStats.Any(kvp => kvp.Value.Any());

    public ExperimentStateManager(IKillSwitchProvider killSwitch)
    {
        _killSwitch = killSwitch;

        // Initialize experiments
        _experiments["pricing-strategy"] = new ExperimentInfo
        {
            Name = "pricing-strategy",
            DisplayName = "Pricing Strategy",
            Description = "A/B test different pricing models to optimize revenue",
            ActiveVariant = "tiered",
            DefaultVariant = "tiered",
            Variants = [
                new VariantInfo { Name = "tiered", DisplayName = "Tiered Pricing", Description = "Volume-based tiers: $10 (<100), $8 (100-500), $6 (500+)" },
                new VariantInfo { Name = "flat", DisplayName = "Flat Rate", Description = "Simple $7.50 per unit for all quantities" },
                new VariantInfo { Name = "volume", DisplayName = "Volume Discount", Description = "Base $9 with 5-20% discounts based on volume" }
            ],
            Category = "Revenue",
            Status = "Active"
        };

        _experiments["notification-style"] = new ExperimentInfo
        {
            Name = "notification-style",
            DisplayName = "Notification Style",
            Description = "Test personalized vs standard notification messaging",
            ActiveVariant = "standard",
            DefaultVariant = "standard",
            Variants = [
                new VariantInfo { Name = "standard", DisplayName = "Standard", Description = "Generic welcome messages" },
                new VariantInfo { Name = "personalized", DisplayName = "Personalized", Description = "User-specific personalized messages" }
            ],
            Category = "Engagement",
            Status = "Active"
        };

        _experiments["recommendation-algorithm"] = new ExperimentInfo
        {
            Name = "recommendation-algorithm",
            DisplayName = "Recommendation Algorithm",
            Description = "Compare recommendation engine algorithms",
            ActiveVariant = "basic",
            DefaultVariant = "basic",
            Variants = [
                new VariantInfo { Name = "basic", DisplayName = "Basic", Description = "Rule-based popular items (60% confidence)" },
                new VariantInfo { Name = "ai-powered", DisplayName = "AI-Powered", Description = "ML-based personalized recommendations (89% confidence)" },
                new VariantInfo { Name = "collaborative", DisplayName = "Collaborative", Description = "User similarity-based filtering (78% confidence)" }
            ],
            Category = "Engagement",
            Status = "Active"
        };

        _experiments["ui-theme"] = new ExperimentInfo
        {
            Name = "ui-theme",
            DisplayName = "UI Theme",
            Description = "Test different visual themes for user preference",
            ActiveVariant = "system",
            DefaultVariant = "system",
            Variants = [
                new VariantInfo { Name = "system", DisplayName = "System Default", Description = "Follows user's OS preference"},
                new VariantInfo { Name = "light", DisplayName = "Light Theme", Description = "Clean white background with blue accents" },
                new VariantInfo { Name = "dark", DisplayName = "Dark Theme", Description = "Dark background for reduced eye strain" }
            ],
            Category = "UX",
            Status = "Active"
        };
    }

    public IEnumerable<ExperimentInfo> GetAllExperiments() => _experiments.Values;

    public ExperimentInfo? GetExperiment(string name) =>
        _experiments.TryGetValue(name, out var exp) ? exp : null;

    public string GetActiveVariant(string name)
    {
        if (!_experiments.TryGetValue(name, out var exp)) return "default";

        var serviceType = ExperimentTypeResolver.GetServiceType(name);
        if (_killSwitch.IsExperimentDisabled(serviceType))
        {
            return exp.DefaultVariant;
        }

        var activeVariant = exp.ActiveVariant;
        return _killSwitch.IsTrialDisabled(serviceType, activeVariant)
            ? exp.DefaultVariant
            : activeVariant;
    }

    public bool SetActiveVariant(string name, string variant)
    {
        if (!_experiments.TryGetValue(name, out var exp)) return false;
        if (!exp.Variants.Any(v => v.Name == variant)) return false;

        var serviceType = ExperimentTypeResolver.GetServiceType(name);
        if (_killSwitch.IsTrialDisabled(serviceType, variant)) return false;

        exp.ActiveVariant = variant;
        exp.LastModified = DateTime.UtcNow;
        return true;
    }

    public void RecordUsage(string experiment, string variant)
    {
        var expStats = _usageStats.GetOrAdd(experiment, _ => new ConcurrentDictionary<string, int>());
        expStats.AddOrUpdate(variant, 1, (_, count) => count + 1);
    }

    public Dictionary<string, Dictionary<string, int>> GetUsageStats() =>
        _usageStats.ToDictionary(x => x.Key, x => x.Value.ToDictionary(y => y.Key, y => y.Value));

    public IEnumerable<KillSwitchStatus> GetKillSwitchStatuses()
    {
        foreach (var experiment in _experiments.Values)
        {
            yield return GetKillSwitchStatus(experiment.Name);
        }
    }

    public KillSwitchStatus GetKillSwitchStatus(string experimentName)
    {
        var type = ExperimentTypeResolver.GetServiceType(experimentName);
        var disabledVariants = _experiments.TryGetValue(experimentName, out var exp)
            ? exp.Variants.Where(v => _killSwitch.IsTrialDisabled(type, v.Name)).Select(v => v.Name).ToList()
            : new List<string>();

        return new KillSwitchStatus(
            experimentName,
            _killSwitch.IsExperimentDisabled(type),
            disabledVariants);
    }

    // DSL Support Methods

    public IEnumerable<ExperimentInfo> GetDslExperiments() =>
        _experiments.Values.Where(e => e.Source == ExperimentSource.Dsl);

    public bool AddExperiment(ExperimentInfo experiment)
    {
        experiment.Source = ExperimentSource.Dsl;
        experiment.LastModified = DateTime.UtcNow;
        return _experiments.TryAdd(experiment.Name, experiment);
    }

    public bool UpdateExperiment(string name, ExperimentInfo updated)
    {
        if (!_experiments.TryGetValue(name, out var existing)) return false;

        updated.LastModified = DateTime.UtcNow;
        updated.Source = existing.Source; // Preserve source
        _experiments[name] = updated;
        return true;
    }

    public bool RemoveExperiment(string name)
    {
        if (!_experiments.TryGetValue(name, out var exp)) return false;
        if (exp.Source != ExperimentSource.Dsl) return false; // Only allow removing DSL experiments

        return _experiments.TryRemove(name, out _);
    }

    public Dictionary<string, ExperimentInfo> CreateSnapshot() =>
        _experiments.ToDictionary(kvp => kvp.Key, kvp => new ExperimentInfo
        {
            Name = kvp.Value.Name,
            DisplayName = kvp.Value.DisplayName,
            Description = kvp.Value.Description,
            ActiveVariant = kvp.Value.ActiveVariant,
            DefaultVariant = kvp.Value.DefaultVariant,
            Variants = kvp.Value.Variants.ToList(),
            Category = kvp.Value.Category,
            Status = kvp.Value.Status,
            LastModified = kvp.Value.LastModified,
            Source = kvp.Value.Source
        });

    public void RestoreSnapshot(Dictionary<string, ExperimentInfo> snapshot)
    {
        _experiments.Clear();
        foreach (var kvp in snapshot)
        {
            _experiments[kvp.Key] = kvp.Value;
        }
    }
}

public class ExperimentInfo
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
    public required string ActiveVariant { get; set; }
    public string DefaultVariant { get; set; } = "default";
    public required List<VariantInfo> Variants { get; set; }
    public required string Category { get; set; }
    public required string Status { get; set; }
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public ExperimentSource Source { get; set; } = ExperimentSource.Code;
}

public enum ExperimentSource
{
    Code,
    Dsl
}

public record DslRequest(string Yaml);

public class VariantInfo
{
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public required string Description { get; set; }
}

// ============================================================================
// In-Memory Audit Sink
// ============================================================================

public class InMemoryAuditSink : IAuditSink
{
    private readonly ConcurrentQueue<AuditLogEntry> _events = new();
    private const int MaxEvents = 1000;

    public ValueTask RecordAsync(AuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            Timestamp = DateTime.UtcNow,
            EventType = auditEvent.EventType.ToString(),
            ExperimentName = auditEvent.ExperimentName,
            TrialName = auditEvent.SelectedTrialKey,
            Details = auditEvent.ToString() ?? ""
        };

        _events.Enqueue(entry);

        // Keep only last MaxEvents
        while (_events.Count > MaxEvents && _events.TryDequeue(out _)) { }

        return ValueTask.CompletedTask;
    }

    public IEnumerable<AuditLogEntry> GetEvents(int limit) =>
        _events.Reverse().Take(limit).ToList();
}

public class AuditLogEntry
{
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = "";
    public string? ExperimentName { get; set; }
    public string? TrialName { get; set; }
    public string Details { get; set; } = "";
}

// ============================================================================
// Pricing Strategy Implementations
// ============================================================================

public interface IPricingStrategy
{
    PricingResult Calculate(int units);
}

public record PricingResult(string Strategy, decimal UnitPrice, decimal Total, string Details);

public class TieredPricing : IPricingStrategy
{
    public PricingResult Calculate(int units)
    {
        var (price, tier) = units switch
        {
            < 100 => (10.00m, "Standard"),
            < 500 => (8.00m, "Bronze"),
            _ => (6.00m, "Gold")
        };
        return new PricingResult("Tiered", price, units * price, $"Tier: {tier}");
    }
}

public class FlatPricing : IPricingStrategy
{
    public PricingResult Calculate(int units)
    {
        const decimal price = 7.50m;
        return new PricingResult("Flat", price, units * price, "Simple flat rate");
    }
}

public class VolumePricing : IPricingStrategy
{
    public PricingResult Calculate(int units)
    {
        const decimal basePrice = 9.00m;
        var discount = units switch
        {
            >= 1000 => 0.20m,
            >= 500 => 0.15m,
            >= 100 => 0.10m,
            >= 50 => 0.05m,
            _ => 0m
        };
        var finalPrice = basePrice * (1 - discount);
        return new PricingResult("Volume", finalPrice, units * finalPrice, $"Discount: {discount:P0}");
    }
}

// ============================================================================
// Notification Service Implementations
// ============================================================================

public interface INotificationService
{
    NotificationResult GetWelcomeMessage(string userId);
}

public record NotificationResult(string Title, string Message, string Style);

public class StandardNotifications : INotificationService
{
    public NotificationResult GetWelcomeMessage(string userId) =>
        new("Welcome!", "Thank you for using our service.", "standard");
}

public class PersonalizedNotifications : INotificationService
{
    public NotificationResult GetWelcomeMessage(string userId) =>
        new($"Welcome back, {userId}!", $"We've missed you! Check out what's new since your last visit.", "personalized");
}

// ============================================================================
// Recommendation Engine Implementations
// ============================================================================

public interface IRecommendationEngine
{
    RecommendationResult GetRecommendations(string userId);
}

public record RecommendationResult(string Algorithm, List<string> Items, double Confidence);

public class BasicRecommendations : IRecommendationEngine
{
    public RecommendationResult GetRecommendations(string userId) =>
        new("Basic (Popular Items)", ["Wireless Headphones", "Smart Watch", "USB-C Hub", "Laptop Stand"], 0.60);
}

public class AiRecommendations : IRecommendationEngine
{
    public RecommendationResult GetRecommendations(string userId) =>
        new("AI-Powered", ["Mechanical Keyboard", "4K Monitor", "Standing Desk", "Ergonomic Chair"], 0.89);
}

public class CollaborativeRecommendations : IRecommendationEngine
{
    public RecommendationResult GetRecommendations(string userId) =>
        new("Collaborative Filtering", ["Noise-Canceling Headphones", "Webcam HD", "Ring Light", "Microphone"], 0.78);
}

// ============================================================================
// Theme Provider Implementations
// ============================================================================

public interface IThemeProvider
{
    ThemeResult GetTheme();
}

public record ThemeResult(string Name, string PrimaryColor, string BackgroundColor, string TextColor, string AccentColor);

public class LightTheme : IThemeProvider
{
    public ThemeResult GetTheme() =>
        new("Light", "#2563eb", "#ffffff", "#1f2937", "#3b82f6");
}

public class DarkTheme : IThemeProvider
{
    public ThemeResult GetTheme() =>
        new("Dark", "#60a5fa", "#1f2937", "#f9fafb", "#93c5fd");
}

public class SystemTheme : IThemeProvider
{
    public ThemeResult GetTheme() =>
        new("System", "#8b5cf6", "#f3f4f6", "#374151", "#a78bfa");
}

// ============================================================================
// Plugin State Manager (Demo)
// ============================================================================

public class PluginStateManager
{
    private readonly ConcurrentDictionary<string, PluginInfo> _plugins = new();
    private readonly ConcurrentDictionary<string, ActivePluginImplementation> _activeImplementations = new();

    public IEnumerable<PluginInfo> GetAllPlugins() => _plugins.Values;

    public ActivePluginImplementation? GetActiveImplementation(string interfaceName)
    {
        _activeImplementations.TryGetValue(interfaceName, out var impl);
        return impl;
    }

    public IReadOnlyDictionary<string, ActivePluginImplementation> GetActiveImplementations() =>
        _activeImplementations.ToDictionary(x => x.Key, x => x.Value);

    public bool SetActiveImplementation(string interfaceName, string pluginId, string implementationType, string? alias)
    {
        if (!_plugins.ContainsKey(pluginId)) return false;

        _activeImplementations[interfaceName] = new ActivePluginImplementation
        {
            PluginId = pluginId,
            Interface = interfaceName,
            ImplementationType = implementationType,
            Alias = alias,
            ActivatedAt = DateTime.UtcNow
        };
        return true;
    }

    public bool ClearActiveImplementation(string interfaceName)
    {
        return _activeImplementations.TryRemove(interfaceName, out _);
    }

    public int DiscoverPlugins()
    {
        // Simulate discovering and loading demo plugins
        var newPlugins = new List<PluginInfo>
        {
            new()
            {
                Id = "enterprise-pricing",
                Name = "Enterprise Pricing Plugin",
                Version = "2.1.0",
                Description = "Advanced pricing strategies for enterprise customers including volume licensing, contract pricing, and usage-based billing",
                Status = "Healthy",
                IsolationMode = "Shared",
                SupportsHotReload = true,
                LoadTime = DateTime.UtcNow,
                Path = "/plugins/enterprise-pricing/enterprise-pricing.dll",
                Services =
                [
                    new PluginServiceInfo
                    {
                        Interface = "IPricingStrategy",
                        Implementations =
                        [
                            new PluginImplementationInfo { Type = "ContractPricing", Alias = "contract" },
                            new PluginImplementationInfo { Type = "UsageBasedPricing", Alias = "usage" },
                            new PluginImplementationInfo { Type = "HybridPricing", Alias = "hybrid" }
                        ]
                    }
                ]
            },
            new()
            {
                Id = "ml-recommendations",
                Name = "ML Recommendations Engine",
                Version = "3.0.0-beta",
                Description = "Machine learning powered recommendation engine using collaborative filtering and deep learning models",
                Status = "Healthy",
                IsolationMode = "Isolated",
                SupportsHotReload = true,
                LoadTime = DateTime.UtcNow,
                Path = "/plugins/ml-recommendations/ml-recommendations.dll",
                Services =
                [
                    new PluginServiceInfo
                    {
                        Interface = "IRecommendationEngine",
                        Implementations =
                        [
                            new PluginImplementationInfo { Type = "DeepLearningRecommender", Alias = "deep-learning" },
                            new PluginImplementationInfo { Type = "NeuralCollaborativeFiltering", Alias = "neural-cf" }
                        ]
                    }
                ]
            },
            new()
            {
                Id = "analytics-export",
                Name = "Analytics Export Plugin",
                Version = "1.5.2",
                Description = "Export experiment analytics to various formats and destinations including BigQuery, Snowflake, and S3",
                Status = "Healthy",
                IsolationMode = "Shared",
                SupportsHotReload = false,
                LoadTime = DateTime.UtcNow,
                Path = "/plugins/analytics-export/analytics-export.dll",
                Services =
                [
                    new PluginServiceInfo
                    {
                        Interface = "IAnalyticsExporter",
                        Implementations =
                        [
                            new PluginImplementationInfo { Type = "BigQueryExporter", Alias = "bigquery" },
                            new PluginImplementationInfo { Type = "SnowflakeExporter", Alias = "snowflake" },
                            new PluginImplementationInfo { Type = "S3Exporter", Alias = "s3" }
                        ]
                    }
                ]
            }
        };

        int loadedCount = 0;
        foreach (var plugin in newPlugins)
        {
            if (_plugins.TryAdd(plugin.Id, plugin))
            {
                loadedCount++;
            }
        }

        return loadedCount;
    }

    public bool ReloadPlugin(string pluginId)
    {
        if (!_plugins.TryGetValue(pluginId, out var plugin)) return false;
        plugin.LoadTime = DateTime.UtcNow;
        return true;
    }

    public bool UnloadPlugin(string pluginId)
    {
        return _plugins.TryRemove(pluginId, out _);
    }
}

public class PluginInfo
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public required string Description { get; set; }
    public required string Status { get; set; }
    public required string IsolationMode { get; set; }
    public required bool SupportsHotReload { get; set; }
    public DateTime LoadTime { get; set; }
    public required string Path { get; set; }
    public required List<PluginServiceInfo> Services { get; set; }
}

public class PluginServiceInfo
{
    public required string Interface { get; set; }
    public required List<PluginImplementationInfo> Implementations { get; set; }
}

public class PluginImplementationInfo
{
    public required string Type { get; set; }
    public string? Alias { get; set; }
}

public class ActivePluginImplementation
{
    public required string PluginId { get; set; }
    public required string Interface { get; set; }
    public required string ImplementationType { get; set; }
    public string? Alias { get; set; }
    public DateTime ActivatedAt { get; set; }
}
