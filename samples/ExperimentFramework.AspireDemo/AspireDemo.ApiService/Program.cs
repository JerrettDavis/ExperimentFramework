using System.Collections.Concurrent;
using AspireDemo.ApiService.Data;
using AspireDemo.ApiService.Models;
using AspireDemo.ApiService.Services;
using ExperimentFramework;
using ExperimentFramework.Audit;
using ExperimentFramework.KillSwitch;
using ExperimentFramework.Models;
using ExperimentFramework.Targeting;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations
builder.AddServiceDefaults();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// Configure SQLite database for persistence
var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "AspireDemo", "experiments.db");
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

builder.Services.AddDbContextFactory<ExperimentDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Experiment state management with persistence
builder.Services.AddSingleton<PersistentKillSwitchProvider>();
builder.Services.AddSingleton<IKillSwitchProvider>(sp => sp.GetRequiredService<PersistentKillSwitchProvider>());
builder.Services.AddSingleton<ExperimentStateManager>();
builder.Services.AddSingleton<PluginStateManager>();
builder.Services.AddSingleton<RuntimeExperimentManager>();
builder.Services.AddSingleton<FeatureAuditService>();

// Blog plugin services
builder.Services.AddSingleton<BlogPluginStateManager>();
builder.Services.AddSingleton<BlogDataStore>();

// Advanced features services
builder.Services.AddSingleton<RolloutService>();
builder.Services.AddSingleton<TargetingService>();
builder.Services.AddSingleton<MetricsCollector>();
builder.Services.AddSingleton<HypothesisTestingService>();

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

// Persistent audit sink
builder.Services.AddSingleton<PersistentAuditSink>();
builder.Services.AddSingleton<IAuditSink>(sp => sp.GetRequiredService<PersistentAuditSink>());

var experiments = ExperimentFrameworkBuilder.Create()
    .UseDispatchProxy()
    .WithTimeout(TimeSpan.FromSeconds(2), TimeoutAction.FallbackToDefault)
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

// Initialize database and load persisted state
await using (var scope = app.Services.CreateAsyncScope())
{
    var contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ExperimentDbContext>>();
    await using var context = await contextFactory.CreateDbContextAsync();
    await context.Database.EnsureCreatedAsync();

    // Initialize kill switch provider from persisted state
    var killSwitch = scope.ServiceProvider.GetRequiredService<PersistentKillSwitchProvider>();
    await killSwitch.InitializeAsync();

    // Initialize audit sink from persisted state
    var auditSink = scope.ServiceProvider.GetRequiredService<PersistentAuditSink>();
    await auditSink.InitializeAsync();
}

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

app.MapPost("/api/experiments/{name}/activate/{variant}", async (string name, string variant, ExperimentStateManager state, IConfiguration config, PersistentAuditSink auditSink, BlogPluginStateManager? blogPlugins) =>
{
    var previousVariant = state.GetActiveVariant(name);
    var success = state.SetActiveVariant(name, variant);
    if (!success) return Results.BadRequest(new { Error = "Invalid experiment or variant" });

    // Update configuration (in real app, this would persist)
    config[$"Experiments:{GetConfigKey(name)}"] = variant;

    // Sync blog plugin state if this is a blog experiment
    if (blogPlugins != null)
    {
        switch (name)
        {
            case "blog-data-provider":
                blogPlugins.Activate("data", variant);
                break;
            case "blog-editor":
                blogPlugins.Activate("editor", variant);
                break;
            case "blog-auth":
                blogPlugins.Activate("auth", variant);
                break;
            case "blog-syndication":
                // Handle syndication variants
                if (variant == "none")
                {
                    // Deactivate all
                    blogPlugins.Deactivate("syndication", "devto");
                    blogPlugins.Deactivate("syndication", "hashnode");
                    blogPlugins.Deactivate("syndication", "medium");
                }
                else if (variant == "all")
                {
                    blogPlugins.Activate("syndication", "devto");
                    blogPlugins.Activate("syndication", "hashnode");
                    blogPlugins.Activate("syndication", "medium");
                }
                else
                {
                    // Deactivate all first, then activate the selected one
                    blogPlugins.Deactivate("syndication", "devto");
                    blogPlugins.Deactivate("syndication", "hashnode");
                    blogPlugins.Deactivate("syndication", "medium");
                    blogPlugins.Activate("syndication", variant);
                }
                break;
        }
    }

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
            ["newVariant"] = variant,
            ["source"] = "Dashboard"
        }
    });

    return Results.Ok(state.GetExperiment(name));
})
.WithName("ActivateVariant")
.WithTags("Experiments");

// ============================================================================
// Rollout Management Endpoints
// ============================================================================

app.MapPost("/api/rollout/{experimentName}/advance", async (string experimentName, RolloutService rollout, PersistentAuditSink auditSink) =>
{
    var success = rollout.AdvanceStage(experimentName);
    if (!success) return Results.BadRequest(new { Error = "Failed to advance rollout stage" });

    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentModified,
        ExperimentName = experimentName,
        Details = new Dictionary<string, object> { ["action"] = "advance-stage" }
    });

    return Results.Ok(new { Success = true });
})
.WithName("AdvanceRolloutStage")
.WithTags("Rollout");

app.MapPost("/api/rollout/{experimentName}/pause", async (string experimentName, RolloutService rollout, PersistentAuditSink auditSink) =>
{
    var success = rollout.PauseRollout(experimentName);
    if (!success) return Results.BadRequest(new { Error = "Failed to pause rollout" });

    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentStopped,
        ExperimentName = experimentName
    });

    return Results.Ok(new { Success = true });
})
.WithName("PauseRollout")
.WithTags("Rollout");

app.MapPost("/api/rollout/{experimentName}/rollback", async (string experimentName, RolloutService rollout, PersistentAuditSink auditSink) =>
{
    var success = rollout.RollbackRollout(experimentName);
    if (!success) return Results.BadRequest(new { Error = "Failed to rollback" });

    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentStopped,
        ExperimentName = experimentName,
        Details = new Dictionary<string, object> { ["action"] = "rollback" }
    });

    return Results.Ok(new { Success = true });
})
.WithName("RollbackRollout")
.WithTags("Rollout");

app.MapPost("/api/rollout/{experimentName}/resume", async (string experimentName, RolloutService rollout, PersistentAuditSink auditSink) =>
{
    var success = rollout.ResumeRollout(experimentName);
    if (!success) return Results.BadRequest(new { Error = "Failed to resume rollout. Can only resume paused or rolled-back rollouts." });

    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentStarted,
        ExperimentName = experimentName,
        Details = new Dictionary<string, object> { ["action"] = "resume" }
    });

    return Results.Ok(new { Success = true });
})
.WithName("ResumeRollout")
.WithTags("Rollout");

app.MapPost("/api/rollout/{experimentName}/restart", async (string experimentName, RolloutService rollout, PersistentAuditSink auditSink) =>
{
    var success = rollout.RestartRollout(experimentName);
    if (!success) return Results.BadRequest(new { Error = "Failed to restart rollout" });

    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentStarted,
        ExperimentName = experimentName,
        Details = new Dictionary<string, object> { ["action"] = "restart" }
    });

    return Results.Ok(new { Success = true });
})
.WithName("RestartRollout")
.WithTags("Rollout");

// ============================================================================
// Hypothesis Testing Endpoints
// ============================================================================

app.MapGet("/api/hypothesis/{experimentName}/results", (string experimentName, ExperimentStateManager state, HypothesisTestingService hypothesisService) =>
{
    var experiment = state.GetExperiment(experimentName);
    if (experiment?.Hypothesis == null)
        return Results.NotFound(new { Error = "Experiment or hypothesis not found" });

    var results = hypothesisService.CalculateResults(experimentName, experiment.Hypothesis, experiment.DefaultVariant);

    if (results != null)
    {
        experiment.Hypothesis.Results = results;
        if (results.IsSignificant && experiment.Hypothesis.Status == HypothesisStatus.Running)
        {
            experiment.Hypothesis.Status = HypothesisStatus.Completed;
        }
    }

    return Results.Ok(new
    {
        Hypothesis = experiment.Hypothesis,
        RealTimeResults = results,
        HasSufficientData = results != null
    });
})
.WithName("GetHypothesisResults")
.WithTags("HypothesisTesting");

app.MapPost("/api/metrics/seed-demo-data", (MetricsCollector metrics) =>
{
    // Seed realistic demo data for recommendation-algorithm experiment
    var random = new Random(42); // Consistent seed
    var experimentName = "recommendation-algorithm";

    // Simulate 1500 users in control (basic) variant
    for (int i = 0; i < 1500; i++)
    {
        var userId = $"user-basic-{i}";
        metrics.RecordVariantExposure(experimentName, "basic", userId);

        // 12.7% conversion rate for control
        if (random.NextDouble() < 0.127)
        {
            metrics.RecordConversion(experimentName, "basic", userId, "Click-Through Rate", 1.0);
        }
    }

    // Simulate 1500 users in treatment (ai-powered) variant
    for (int i = 0; i < 1500; i++)
    {
        var userId = $"user-ai-{i}";
        metrics.RecordVariantExposure(experimentName, "ai-powered", userId);

        // 18.9% conversion rate for treatment (48.8% increase)
        if (random.NextDouble() < 0.189)
        {
            metrics.RecordConversion(experimentName, "ai-powered", userId, "Click-Through Rate", 1.0);
        }
    }

    return Results.Ok(new
    {
        Message = "Demo data seeded successfully",
        ExperimentName = experimentName,
        ControlSamples = 1500,
        TreatmentSamples = 1500
    });
})
.WithName("SeedDemoData")
.WithTags("Metrics");

// ============================================================================
// Experiment Demo Endpoints
// ============================================================================

app.MapGet("/api/pricing/calculate", async (int units, ExperimentStateManager state, PluginStateManager plugins, PersistentAuditSink auditSink) =>
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

app.MapGet("/api/notifications/preview", async (string? userId, ExperimentStateManager state, PersistentAuditSink auditSink) =>
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

app.MapGet("/api/recommendations", async (string? userId, ExperimentStateManager state, PluginStateManager plugins, PersistentAuditSink auditSink) =>
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

app.MapGet("/api/theme", async (ExperimentStateManager state, PersistentAuditSink auditSink) =>
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

app.MapGet("/api/audit", (PersistentAuditSink auditSink, int? limit) =>
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

app.MapPost("/api/plugins/discover", async (PluginStateManager plugins, PersistentAuditSink auditSink) =>
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

app.MapPost("/api/plugins/{pluginId}/reload", async (string pluginId, PluginStateManager plugins, PersistentAuditSink auditSink) =>
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

app.MapDelete("/api/plugins/{pluginId}", async (string pluginId, PluginStateManager plugins, PersistentAuditSink auditSink) =>
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

app.MapPost("/api/plugins/{pluginId}/use", async (string pluginId, string @interface, string impl, PluginStateManager plugins, PersistentAuditSink auditSink) =>
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

app.MapDelete("/api/plugins/active/{interface}", async (string @interface, PluginStateManager plugins, PersistentAuditSink auditSink) =>
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
// Blog API Endpoints
// ============================================================================

app.MapGet("/api/blog/posts", (BlogDataStore store, string? status, string? category) =>
{
    var posts = store.GetPosts(status);
    if (!string.IsNullOrEmpty(category))
    {
        posts = posts.Where(p => p.Categories.Any(c =>
            c.Slug.Equals(category, StringComparison.OrdinalIgnoreCase) ||
            c.Name.Equals(category, StringComparison.OrdinalIgnoreCase)
        )).ToList();
    }
    return Results.Ok(posts);
})
.WithName("GetBlogPosts")
.WithTags("Blog");

app.MapGet("/api/blog/posts/{slug}", (string slug, BlogDataStore store) =>
{
    var post = store.GetPostBySlug(slug);
    return post is null ? Results.NotFound() : Results.Ok(post);
})
.WithName("GetBlogPost")
.WithTags("Blog");

app.MapGet("/api/blog/categories", (BlogDataStore store) =>
{
    return Results.Ok(store.GetCategories());
})
.WithName("GetBlogCategories")
.WithTags("Blog");

app.MapGet("/api/blog/authors", (BlogDataStore store) =>
{
    return Results.Ok(store.GetAuthors());
})
.WithName("GetBlogAuthors")
.WithTags("Blog");

app.MapGet("/api/blog/stats", (BlogDataStore store) =>
{
    return Results.Ok(store.GetStats());
})
.WithName("GetBlogStats")
.WithTags("Blog");

app.MapGet("/api/blog/search", (string q, BlogDataStore store) =>
{
    if (string.IsNullOrWhiteSpace(q))
        return Results.Ok(Array.Empty<BlogPostDto>());

    var query = q.ToLowerInvariant();
    var results = store.GetPosts()
        .Where(p =>
            p.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            p.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)))
        .Take(10)
        .ToList();

    return Results.Ok(results);
})
.WithName("SearchBlogPosts")
.WithTags("Blog");

// Blog Plugin Management Endpoints

app.MapGet("/api/blog/plugins", (BlogPluginStateManager blogPlugins, IKillSwitchProvider killSwitch) =>
{
    // Helper to get active plugin respecting killswitches
    string GetActivePluginWithKillSwitch(string experimentName, string currentActive, string defaultValue)
    {
        var type = ExperimentTypeResolver.GetServiceType(experimentName);

        // If entire experiment is disabled, return default
        if (killSwitch.IsExperimentDisabled(type))
            return defaultValue;

        // If current active variant is disabled, return default
        if (killSwitch.IsTrialDisabled(type, currentActive))
            return defaultValue;

        return currentActive;
    }

    // Get active plugins respecting killswitches
    var activeDataProvider = GetActivePluginWithKillSwitch(
        "blog-data-provider",
        blogPlugins.ActiveDataProviderAlias,
        "in-memory");

    var activeEditor = GetActivePluginWithKillSwitch(
        "blog-editor",
        blogPlugins.ActiveEditorAlias,
        "markdown");

    var activeAuth = GetActivePluginWithKillSwitch(
        "blog-auth",
        blogPlugins.ActiveAuthAlias,
        "simple-token");

    return Results.Ok(new
    {
        Data = new
        {
            Options = BlogPluginStateManager.DataProviders,
            Active = activeDataProvider
        },
        Editor = new
        {
            Options = BlogPluginStateManager.Editors,
            Active = activeEditor
        },
        Syndication = new
        {
            Options = BlogPluginStateManager.Syndicators,
            Active = blogPlugins.ActiveSyndicatorAliases // Syndication doesn't use killswitches (multi-select)
        },
        Auth = new
        {
            Options = BlogPluginStateManager.AuthProviders,
            Active = activeAuth
        }
    });
})
.WithName("GetBlogPlugins")
.WithTags("BlogPlugins");

app.MapPost("/api/blog/plugins/activate", async (BlogPluginActivateRequest request, BlogPluginStateManager blogPlugins, ExperimentStateManager state, PersistentAuditSink auditSink) =>
{
    var success = blogPlugins.Activate(request.PluginType, request.Alias);
    if (!success)
        return Results.BadRequest(new { Error = $"Invalid plugin type or alias: {request.PluginType}/{request.Alias}" });

    // Sync with experiment state
    var experimentName = request.PluginType.ToLowerInvariant() switch
    {
        "data" => "blog-data-provider",
        "editor" => "blog-editor",
        "auth" => "blog-auth",
        "syndication" => "blog-syndication",
        _ => null
    };

    if (experimentName != null)
    {
        state.SetActiveVariant(experimentName, request.Alias);
    }

    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentModified,
        ExperimentName = $"Blog.{request.PluginType}",
        SelectedTrialKey = request.Alias,
        Details = new Dictionary<string, object>
        {
            ["pluginType"] = request.PluginType,
            ["alias"] = request.Alias,
            ["source"] = "BlogAdmin"
        }
    });

    return Results.Ok(new { PluginType = request.PluginType, Alias = request.Alias, Activated = true });
})
.WithName("ActivateBlogPlugin")
.WithTags("BlogPlugins");

app.MapPost("/api/blog/plugins/deactivate", async (BlogPluginActivateRequest request, BlogPluginStateManager blogPlugins, PersistentAuditSink auditSink) =>
{
    var success = blogPlugins.Deactivate(request.PluginType, request.Alias);
    if (!success)
        return Results.BadRequest(new { Error = "Only syndication plugins can be deactivated" });

    await auditSink.RecordAsync(new AuditEvent
    {
        EventId = Guid.NewGuid().ToString(),
        Timestamp = DateTimeOffset.UtcNow,
        EventType = AuditEventType.ExperimentStopped,
        ExperimentName = $"Blog.{request.PluginType}",
        SelectedTrialKey = request.Alias
    });

    return Results.Ok(new { PluginType = request.PluginType, Alias = request.Alias, Deactivated = true });
})
.WithName("DeactivateBlogPlugin")
.WithTags("BlogPlugins");

app.MapGet("/api/blog/editor/config", (BlogPluginStateManager blogPlugins, IKillSwitchProvider killSwitch) =>
{
    // Check killswitch first
    var type = ExperimentTypeResolver.GetServiceType("blog-editor");
    var activeEditorAlias = blogPlugins.ActiveEditorAlias;

    // If experiment disabled or active variant disabled, use default (markdown)
    if (killSwitch.IsExperimentDisabled(type) || killSwitch.IsTrialDisabled(type, activeEditorAlias))
    {
        activeEditorAlias = "markdown";
    }

    var activeEditor = BlogPluginStateManager.Editors.FirstOrDefault(e => e.Alias == activeEditorAlias);
    if (activeEditor == null)
        return Results.NotFound();

    // Return editor-specific configuration
    var config = activeEditor.Alias switch
    {
        "markdown" => new { Type = "markdown", ScriptUrl = (string?)null, StyleUrl = (string?)null },
        "tinymce" => new { Type = "tinymce", ScriptUrl = "https://cdn.tiny.cloud/1/no-api-key/tinymce/6/tinymce.min.js", StyleUrl = (string?)null },
        "quill" => new { Type = "quill", ScriptUrl = "https://cdn.quilljs.com/1.3.6/quill.min.js", StyleUrl = "https://cdn.quilljs.com/1.3.6/quill.snow.css" },
        _ => new { Type = "markdown", ScriptUrl = (string?)null, StyleUrl = (string?)null }
    };

    return Results.Ok(new
    {
        Editor = activeEditor,
        Config = config
    });
})
.WithName("GetBlogEditorConfig")
.WithTags("BlogPlugins");

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

app.MapPost("/api/dsl/apply", async (DslRequest request, RuntimeExperimentManager dslManager, PersistentAuditSink auditSink) =>
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
        ["ui-theme"] = typeof(ThemeExperimentMarker),
        ["blog-data-provider"] = typeof(BlogDataProviderMarker),
        ["blog-editor"] = typeof(BlogEditorMarker),
        ["blog-auth"] = typeof(BlogAuthMarker),
        ["blog-syndication"] = typeof(BlogSyndicationMarker)
    };

    public static Type GetServiceType(string experimentName) =>
        ExperimentTypes.TryGetValue(experimentName, out var type)
            ? type
            : typeof(DslExperimentMarker);

    private sealed class PricingExperimentMarker;
    private sealed class NotificationExperimentMarker;
    private sealed class RecommendationExperimentMarker;
    private sealed class ThemeExperimentMarker;
    private sealed class BlogDataProviderMarker;
    private sealed class BlogEditorMarker;
    private sealed class BlogAuthMarker;
    private sealed class BlogSyndicationMarker;
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
            ActiveVariant = "ai-powered",
            DefaultVariant = "basic",
            Variants = [
                new VariantInfo { Name = "basic", DisplayName = "Basic", Description = "Rule-based popular items (60% confidence)" },
                new VariantInfo { Name = "ai-powered", DisplayName = "AI-Powered", Description = "ML-based personalized recommendations (89% confidence)" },
                new VariantInfo { Name = "collaborative", DisplayName = "Collaborative", Description = "User similarity-based filtering (78% confidence)" }
            ],
            Category = "Engagement",
            Status = "Active",
            Hypothesis = new HypothesisInfo
            {
                Statement = "AI-powered recommendations will increase user engagement (click-through rate) by at least 15% compared to basic recommendations",
                TestType = "Superiority",
                PrimaryMetric = "Click-Through Rate",
                SecondaryMetrics = ["Add to Cart Rate", "Time on Site", "Pages per Session"],
                ExpectedEffectSize = 0.15,
                AlphaLevel = 0.05,
                PowerLevel = 0.80,
                MinimumSampleSize = 1000,
                StartDate = DateTime.UtcNow.AddDays(-14),
                Status = HypothesisStatus.Running
                // Results will be calculated dynamically from real metrics
            }
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

        // Blog Plugin Experiments
        _experiments["blog-data-provider"] = new ExperimentInfo
        {
            Name = "blog-data-provider",
            DisplayName = "Blog Data Provider",
            Description = "Choose how blog data is stored and retrieved (TechBlog app)",
            ActiveVariant = "inmemory",
            DefaultVariant = "inmemory",
            Variants = [
                new VariantInfo { Name = "inmemory", DisplayName = "In-Memory", Description = "Fast volatile storage - resets on restart" },
                new VariantInfo { Name = "sqlite", DisplayName = "SQLite", Description = "Persistent file-based storage" },
                new VariantInfo { Name = "postgres", DisplayName = "PostgreSQL", Description = "Production-ready relational database" }
            ],
            Category = "Blog",
            Status = "Active",
            Rollout = new RolloutConfig
            {
                Enabled = true,
                Percentage = 50,
                Status = RolloutStatus.InProgress,
                StartDate = DateTime.UtcNow.AddDays(-3),
                Stages = [
                    new RolloutStage { Name = "Initial Test", Percentage = 5, ScheduledDate = DateTime.UtcNow.AddDays(-3), ExecutedDate = DateTime.UtcNow.AddDays(-3), Status = RolloutStageStatus.Completed },
                    new RolloutStage { Name = "Early Adopters", Percentage = 25, ScheduledDate = DateTime.UtcNow.AddDays(-2), ExecutedDate = DateTime.UtcNow.AddDays(-2), Status = RolloutStageStatus.Completed },
                    new RolloutStage { Name = "Half Rollout", Percentage = 50, ScheduledDate = DateTime.UtcNow.AddDays(-1), ExecutedDate = DateTime.UtcNow.AddDays(-1), Status = RolloutStageStatus.Active },
                    new RolloutStage { Name = "Majority", Percentage = 75, ScheduledDate = DateTime.UtcNow.AddDays(1), Status = RolloutStageStatus.Pending },
                    new RolloutStage { Name = "Full Rollout", Percentage = 100, ScheduledDate = DateTime.UtcNow.AddDays(3), Status = RolloutStageStatus.Pending }
                ]
            }
        };

        _experiments["blog-editor"] = new ExperimentInfo
        {
            Name = "blog-editor",
            DisplayName = "Blog Content Editor",
            Description = "Select the editor for writing blog posts (TechBlog app)",
            ActiveVariant = "markdown",
            DefaultVariant = "markdown",
            Variants = [
                new VariantInfo { Name = "markdown", DisplayName = "Markdown", Description = "Developer-friendly markdown with live preview" },
                new VariantInfo { Name = "tinymce", DisplayName = "TinyMCE", Description = "Full WYSIWYG with rich formatting" },
                new VariantInfo { Name = "quill", DisplayName = "Quill", Description = "Modern lightweight rich text editor" }
            ],
            Category = "Blog",
            Status = "Active",
            TargetingRules = [
                new TargetingRule
                {
                    Name = "Premium Users → TinyMCE",
                    Description = "Premium tier users get advanced WYSIWYG editor",
                    Variant = "tinymce",
                    Priority = 1,
                    Enabled = true,
                    Conditions = [
                        new TargetingCondition { Attribute = "plan", Operator = "equals", Value = "premium" }
                    ]
                },
                new TargetingRule
                {
                    Name = "Developers → Markdown",
                    Description = "Users with developer role prefer markdown",
                    Variant = "markdown",
                    Priority = 2,
                    Enabled = true,
                    Conditions = [
                        new TargetingCondition { Attribute = "role", Operator = "in", Value = "developer,engineer,tech-writer" }
                    ]
                },
                new TargetingRule
                {
                    Name = "Content Team → Quill",
                    Description = "Marketing and content team gets clean modern editor",
                    Variant = "quill",
                    Priority = 3,
                    Enabled = true,
                    Conditions = [
                        new TargetingCondition { Attribute = "department", Operator = "in", Value = "marketing,content" }
                    ]
                }
            ]
        };

        _experiments["blog-auth"] = new ExperimentInfo
        {
            Name = "blog-auth",
            DisplayName = "Blog Authentication",
            Description = "Configure how blog users authenticate (TechBlog app)",
            ActiveVariant = "jwt",
            DefaultVariant = "jwt",
            Variants = [
                new VariantInfo { Name = "jwt", DisplayName = "JWT", Description = "JSON Web Token with refresh tokens" },
                new VariantInfo { Name = "oauth", DisplayName = "OAuth 2.0", Description = "Sign in with GitHub/Google" },
                new VariantInfo { Name = "apikey", DisplayName = "API Key", Description = "Simple API key for integrations" }
            ],
            Category = "Blog",
            Status = "Active"
        };

        _experiments["blog-syndication"] = new ExperimentInfo
        {
            Name = "blog-syndication",
            DisplayName = "Blog Syndication",
            Description = "Enable cross-posting to other platforms (TechBlog app)",
            ActiveVariant = "none",
            DefaultVariant = "none",
            Variants = [
                new VariantInfo { Name = "none", DisplayName = "Disabled", Description = "No cross-posting enabled" },
                new VariantInfo { Name = "devto", DisplayName = "DEV Community", Description = "Publish to dev.to" },
                new VariantInfo { Name = "hashnode", DisplayName = "Hashnode", Description = "Publish to Hashnode" },
                new VariantInfo { Name = "medium", DisplayName = "Medium", Description = "Publish to Medium" },
                new VariantInfo { Name = "all", DisplayName = "All Platforms", Description = "Cross-post to all platforms" }
            ],
            Category = "Blog",
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
    public RolloutConfig? Rollout { get; set; }
    public List<TargetingRule> TargetingRules { get; set; } = [];
    public HypothesisInfo? Hypothesis { get; set; }
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
// Rollout Models
// ============================================================================

public class RolloutConfig
{
    public bool Enabled { get; set; }
    public int Percentage { get; set; } = 0;
    public List<RolloutStage> Stages { get; set; } = [];
    public DateTime? StartDate { get; set; }
    public RolloutStatus Status { get; set; } = RolloutStatus.NotStarted;
}

public class RolloutStage
{
    public required string Name { get; set; }
    public int Percentage { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? ExecutedDate { get; set; }
    public RolloutStageStatus Status { get; set; } = RolloutStageStatus.Pending;
}

public enum RolloutStatus
{
    NotStarted,
    InProgress,
    Completed,
    Paused,
    RolledBack
}

public enum RolloutStageStatus
{
    Pending,
    Active,
    Completed,
    Skipped
}

// ============================================================================
// Targeting Models
// ============================================================================

public class TargetingRule
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Variant { get; set; }
    public List<TargetingCondition> Conditions { get; set; } = [];
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; } = 0;
}

public class TargetingCondition
{
    public required string Attribute { get; set; }
    public required string Operator { get; set; } // equals, in, regex, greaterThan, lessThan
    public required string Value { get; set; }
}

public class UserContext
{
    public string UserId { get; set; } = "";
    public Dictionary<string, string> Attributes { get; set; } = new();
}

// ============================================================================
// Hypothesis Testing Models
// ============================================================================

public class HypothesisInfo
{
    public required string Statement { get; set; }
    public required string TestType { get; set; } // Superiority, NonInferiority, Equivalence, TwoSided
    public required string PrimaryMetric { get; set; }
    public List<string> SecondaryMetrics { get; set; } = [];
    public double ExpectedEffectSize { get; set; }
    public double AlphaLevel { get; set; } = 0.05;
    public double PowerLevel { get; set; } = 0.80;
    public int MinimumSampleSize { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public HypothesisStatus Status { get; set; } = HypothesisStatus.Draft;
    public HypothesisResults? Results { get; set; }
}

public class HypothesisResults
{
    public int ControlSamples { get; set; }
    public int TreatmentSamples { get; set; }
    public double ControlMean { get; set; }
    public double TreatmentMean { get; set; }
    public double EffectSize { get; set; }
    public double PValue { get; set; }
    public double ConfidenceIntervalLower { get; set; }
    public double ConfidenceIntervalUpper { get; set; }
    public bool IsSignificant { get; set; }
    public required string Conclusion { get; set; }
    public DateTime AnalyzedAt { get; set; }
}

public enum HypothesisStatus
{
    Draft,
    Registered,
    Running,
    Analyzing,
    Completed,
    Inconclusive
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

// ============================================================================
// Blog Plugin State Manager
// ============================================================================

public class BlogPluginStateManager
{
    public string ActiveDataProviderAlias { get; private set; } = "inmemory";
    public string ActiveEditorAlias { get; private set; } = "markdown";
    public List<string> ActiveSyndicatorAliases { get; private set; } = [];
    public string ActiveAuthAlias { get; private set; } = "jwt";

    public bool Activate(string pluginType, string alias)
    {
        switch (pluginType.ToLowerInvariant())
        {
            case "data":
                if (!DataProviders.Any(p => p.Alias == alias)) return false;
                ActiveDataProviderAlias = alias;
                return true;
            case "editor":
                if (!Editors.Any(e => e.Alias == alias)) return false;
                ActiveEditorAlias = alias;
                return true;
            case "auth":
                if (!AuthProviders.Any(a => a.Alias == alias)) return false;
                ActiveAuthAlias = alias;
                return true;
            case "syndication":
                if (!Syndicators.Any(s => s.Alias == alias)) return false;
                if (!ActiveSyndicatorAliases.Contains(alias))
                    ActiveSyndicatorAliases.Add(alias);
                return true;
            default:
                return false;
        }
    }

    public bool Deactivate(string pluginType, string alias)
    {
        if (pluginType.ToLowerInvariant() == "syndication")
        {
            return ActiveSyndicatorAliases.Remove(alias);
        }
        return false;
    }

    public static readonly List<BlogPluginOptionInfo> DataProviders =
    [
        new("inmemory", "InMemoryDataProvider", "In-Memory", "Fast volatile storage - resets on restart", ["Fast reads", "No setup", "Volatile"]),
        new("sqlite", "SqliteDataProvider", "SQLite", "Persistent file-based storage - survives restarts", ["Persistent", "File-based", "ACID"]),
        new("postgres", "PostgresDataProvider", "PostgreSQL", "Production-ready relational database (simulated)", ["Scalable", "Production-ready", "Concurrent"])
    ];

    public static readonly List<BlogPluginOptionInfo> Editors =
    [
        new("markdown", "MarkdownEditor", "Markdown", "Developer-friendly markdown with live preview", ["Code blocks", "Live preview", "Fast"]),
        new("tinymce", "TinyMceEditor", "TinyMCE", "Full WYSIWYG with rich formatting and media", ["WYSIWYG", "Tables", "Media"]),
        new("quill", "QuillEditor", "Quill", "Modern lightweight rich text editor", ["Clean output", "Customizable", "Mobile-friendly"])
    ];

    public static readonly List<BlogPluginOptionInfo> Syndicators =
    [
        new("devto", "DevToSyndicator", "DEV Community", "Publish to dev.to", ["Drafts", "Canonical URLs"]) { Color = "#0a0a0a" },
        new("hashnode", "HashnodeSyndicator", "Hashnode", "Publish to Hashnode", ["Drafts", "Scheduling", "Canonical"]) { Color = "#2962ff" },
        new("medium", "MediumSyndicator", "Medium", "Publish to Medium", ["Canonical URLs", "Large audience"]) { Color = "#000000" }
    ];

    public static readonly List<BlogPluginOptionInfo> AuthProviders =
    [
        new("jwt", "JwtAuthProvider", "JWT", "JSON Web Token with refresh tokens", ["Stateless", "Scalable"]),
        new("oauth", "OAuthProvider", "OAuth 2.0", "Sign in with GitHub/Google", ["Social login", "Delegated auth"]),
        new("apikey", "ApiKeyAuthProvider", "API Key", "Simple API key for integrations", ["Simple", "Server-to-server"])
    ];
}

public record BlogPluginOptionInfo(
    string Alias,
    string TypeName,
    string Name,
    string Description,
    List<string> Features)
{
    public string? Color { get; init; }
}

// ============================================================================
// Blog Data Models (for API)
// ============================================================================

public class BlogPostDto
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required string Content { get; set; }
    public string? Excerpt { get; set; }
    public string? FeaturedImage { get; set; }
    public string Status { get; set; } = "Draft";
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public required BlogAuthorDto Author { get; set; }
    public List<BlogCategoryDto> Categories { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public int ViewCount { get; set; }
    public int ReadTimeMinutes { get; set; }
    public Dictionary<string, string> SyndicationLinks { get; set; } = [];
}

public class BlogAuthorDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string? TwitterHandle { get; set; }
    public string? GitHubHandle { get; set; }
    public int PostCount { get; set; }
}

public class BlogCategoryDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string? Description { get; set; }
    public string? Color { get; set; }
    public int PostCount { get; set; }
}

public class BlogStatsDto
{
    public int TotalPosts { get; set; }
    public int PublishedPosts { get; set; }
    public int DraftPosts { get; set; }
    public int TotalViews { get; set; }
    public int TotalAuthors { get; set; }
    public int TotalCategories { get; set; }
}

// Blog Data Store (in-memory for demo)
public class BlogDataStore
{
    private static readonly List<BlogAuthorDto> Authors =
    [
        new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Alice Chen",
            Email = "alice@techblog.dev",
            Bio = "Senior software engineer specializing in distributed systems and cloud architecture.",
            AvatarUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=alice",
            TwitterHandle = "alicechen_dev",
            GitHubHandle = "alicechendev",
            PostCount = 5
        },
        new()
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Bob Martinez",
            Email = "bob@techblog.dev",
            Bio = "Full-stack developer with a passion for developer experience and tooling.",
            AvatarUrl = "https://api.dicebear.com/7.x/avataaars/svg?seed=bob",
            TwitterHandle = "bobmartinez",
            GitHubHandle = "bobmdev",
            PostCount = 3
        }
    ];

    private static readonly List<BlogCategoryDto> Categories =
    [
        new() { Id = Guid.Parse("aaaa1111-1111-1111-1111-111111111111"), Name = "Tutorials", Slug = "tutorials", Description = "Step-by-step guides", Color = "#3b82f6", PostCount = 4 },
        new() { Id = Guid.Parse("aaaa2222-2222-2222-2222-222222222222"), Name = "Architecture", Slug = "architecture", Description = "Software design patterns", Color = "#10b981", PostCount = 2 },
        new() { Id = Guid.Parse("aaaa3333-3333-3333-3333-333333333333"), Name = ".NET", Slug = "dotnet", Description = ".NET ecosystem articles", Color = "#8b5cf6", PostCount = 5 },
        new() { Id = Guid.Parse("aaaa4444-4444-4444-4444-444444444444"), Name = "DevOps", Slug = "devops", Description = "CI/CD and infrastructure", Color = "#f59e0b", PostCount = 2 },
        new() { Id = Guid.Parse("aaaa5555-5555-5555-5555-555555555555"), Name = "Cloud", Slug = "cloud", Description = "Cloud computing topics", Color = "#06b6d4", PostCount = 3 }
    ];

    private static readonly List<BlogPostDto> Posts =
    [
        new()
        {
            Id = Guid.Parse("bbbb1111-1111-1111-1111-111111111111"),
            Title = "Getting Started with ExperimentFramework",
            Slug = "getting-started-experimentframework",
            Content = "# Getting Started with ExperimentFramework\n\nExperimentFramework is a powerful library for running A/B tests and feature experiments in .NET applications.\n\n## Installation\n\n```bash\ndotnet add package ExperimentFramework\n```\n\n## Basic Usage\n\nDefine your experiments using the fluent API:\n\n```csharp\nvar experiments = ExperimentFrameworkBuilder.Create()\n    .Define<IPricingStrategy>(c => c\n        .AddDefaultTrial<StandardPricing>(\"standard\")\n        .AddTrial<PremiumPricing>(\"premium\"))\n    .Build();\n```\n\n## Next Steps\n\n- Configure targeting rules\n- Set up analytics tracking\n- Create your first experiment",
            Excerpt = "Learn how to integrate ExperimentFramework into your .NET applications for A/B testing and feature experiments.",
            FeaturedImage = "https://images.unsplash.com/photo-1558494949-ef010cbdcc31?w=1200",
            Status = "Published",
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            PublishedAt = DateTime.UtcNow.AddDays(-7),
            Author = Authors[0],
            Categories = [Categories[0], Categories[2]],
            Tags = ["experimentation", "a-b-testing", "dotnet"],
            ViewCount = 342,
            ReadTimeMinutes = 5
        },
        new()
        {
            Id = Guid.Parse("bbbb2222-2222-2222-2222-222222222222"),
            Title = "Plugin Architecture Best Practices",
            Slug = "plugin-architecture-best-practices",
            Content = "# Plugin Architecture Best Practices\n\nBuilding extensible applications with plugins requires careful design.\n\n## Core Principles\n\n1. **Isolation**: Plugins should be isolated from the host\n2. **Discoverability**: Easy to find and load plugins\n3. **Hot Reload**: Support updating without restart\n\n## Implementation Strategies\n\n### Assembly Loading\n\nUse `AssemblyLoadContext` for proper isolation:\n\n```csharp\npublic class PluginLoadContext : AssemblyLoadContext\n{\n    // Custom loading logic\n}\n```\n\n### Interface Contracts\n\nDefine clear contracts between host and plugins.",
            Excerpt = "Best practices for designing extensible applications with a robust plugin architecture.",
            FeaturedImage = "https://images.unsplash.com/photo-1555949963-ff9fe0c870eb?w=1200",
            Status = "Published",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            PublishedAt = DateTime.UtcNow.AddDays(-5),
            Author = Authors[1],
            Categories = [Categories[1], Categories[2]],
            Tags = ["architecture", "plugins", "extensibility"],
            ViewCount = 256,
            ReadTimeMinutes = 8
        },
        new()
        {
            Id = Guid.Parse("bbbb3333-3333-3333-3333-333333333333"),
            Title = "Building Modern APIs with .NET 10",
            Slug = "building-modern-apis-dotnet-10",
            Content = "# Building Modern APIs with .NET 10\n\n.NET 10 brings exciting features for API development.\n\n## Minimal APIs\n\n```csharp\nvar app = WebApplication.Create();\napp.MapGet(\"/api/items\", () => Results.Ok(items));\napp.Run();\n```\n\n## Performance Improvements\n\n- Native AOT support\n- Improved JSON serialization\n- Better memory management",
            Excerpt = "Explore the new features in .NET 10 for building high-performance APIs.",
            FeaturedImage = "https://images.unsplash.com/photo-1627398242454-45a1465c2479?w=1200",
            Status = "Published",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            PublishedAt = DateTime.UtcNow.AddDays(-3),
            Author = Authors[0],
            Categories = [Categories[2], Categories[0]],
            Tags = ["dotnet", "api", "web-development"],
            ViewCount = 189,
            ReadTimeMinutes = 6
        },
        new()
        {
            Id = Guid.Parse("bbbb4444-4444-4444-4444-444444444444"),
            Title = "CI/CD with GitHub Actions for .NET",
            Slug = "cicd-github-actions-dotnet",
            Content = "# CI/CD with GitHub Actions for .NET\n\nAutomate your .NET build and deployment pipeline.\n\n## Workflow Configuration\n\n```yaml\nname: .NET CI\non: [push, pull_request]\njobs:\n  build:\n    runs-on: ubuntu-latest\n    steps:\n      - uses: actions/checkout@v4\n      - uses: actions/setup-dotnet@v4\n      - run: dotnet build\n      - run: dotnet test\n```",
            Excerpt = "Set up continuous integration and deployment for your .NET projects using GitHub Actions.",
            Status = "Draft",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            Author = Authors[1],
            Categories = [Categories[3], Categories[2]],
            Tags = ["devops", "github-actions", "ci-cd"],
            ViewCount = 0,
            ReadTimeMinutes = 7
        }
    ];

    public List<BlogAuthorDto> GetAuthors() => Authors;
    public List<BlogCategoryDto> GetCategories() => Categories;
    public List<BlogPostDto> GetPosts(string? status = null)
    {
        var posts = Posts.AsEnumerable();
        if (!string.IsNullOrEmpty(status))
            posts = posts.Where(p => p.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        return posts.OrderByDescending(p => p.PublishedAt ?? p.CreatedAt).ToList();
    }
    public BlogPostDto? GetPostBySlug(string slug) => Posts.FirstOrDefault(p => p.Slug == slug);
    public BlogStatsDto GetStats() => new()
    {
        TotalPosts = Posts.Count,
        PublishedPosts = Posts.Count(p => p.Status == "Published"),
        DraftPosts = Posts.Count(p => p.Status == "Draft"),
        TotalViews = Posts.Sum(p => p.ViewCount),
        TotalAuthors = Authors.Count,
        TotalCategories = Categories.Count
    };
}

public record BlogPluginActivateRequest(string PluginType, string Alias);

// ============================================================================
// Rollout Service - Functional hash-based bucketing
// ============================================================================

public class RolloutService
{
    private readonly ExperimentStateManager _experimentState;

    public RolloutService(ExperimentStateManager experimentState)
    {
        _experimentState = experimentState;
    }

    public string EvaluateVariant(string experimentName, string userId, ExperimentInfo experiment)
    {
        if (experiment.Rollout == null || !experiment.Rollout.Enabled)
            return experiment.ActiveVariant;

        // Hash-based deterministic bucketing
        var bucket = GetBucket(userId, experimentName);

        // Check if user falls within rollout percentage
        if (bucket < experiment.Rollout.Percentage)
        {
            // User is in rollout, gets the active variant
            return experiment.ActiveVariant;
        }
        else
        {
            // User is not in rollout, gets the default variant
            return experiment.DefaultVariant;
        }
    }

    private int GetBucket(string userId, string experimentName)
    {
        // Stable hash-based bucketing (0-99)
        var combined = $"{userId}:{experimentName}";
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(combined));
        var hashValue = BitConverter.ToUInt32(hash, 0);
        return (int)(hashValue % 100);
    }

    public bool AdvanceStage(string experimentName)
    {
        var experiment = _experimentState.GetExperiment(experimentName);
        if (experiment?.Rollout == null) return false;

        var currentStage = experiment.Rollout.Stages.FirstOrDefault(s => s.Status == RolloutStageStatus.Active);
        var nextStage = experiment.Rollout.Stages
            .Where(s => s.Status == RolloutStageStatus.Pending)
            .OrderBy(s => s.Percentage)
            .FirstOrDefault();

        if (nextStage == null) return false;

        if (currentStage != null)
        {
            currentStage.Status = RolloutStageStatus.Completed;
        }

        nextStage.Status = RolloutStageStatus.Active;
        nextStage.ExecutedDate = DateTime.UtcNow;
        experiment.Rollout.Percentage = nextStage.Percentage;

        if (experiment.Rollout.Stages.All(s => s.Status == RolloutStageStatus.Completed))
        {
            experiment.Rollout.Status = RolloutStatus.Completed;
        }
        else
        {
            experiment.Rollout.Status = RolloutStatus.InProgress;
        }

        return true;
    }

    public bool PauseRollout(string experimentName)
    {
        var experiment = _experimentState.GetExperiment(experimentName);
        if (experiment?.Rollout == null) return false;

        experiment.Rollout.Status = RolloutStatus.Paused;
        return true;
    }

    public bool RollbackRollout(string experimentName)
    {
        var experiment = _experimentState.GetExperiment(experimentName);
        if (experiment?.Rollout == null) return false;

        experiment.Rollout.Status = RolloutStatus.RolledBack;
        experiment.Rollout.Percentage = 0;

        foreach (var stage in experiment.Rollout.Stages)
        {
            if (stage.Status == RolloutStageStatus.Active)
                stage.Status = RolloutStageStatus.Pending;
        }

        return true;
    }

    public bool ResumeRollout(string experimentName)
    {
        var experiment = _experimentState.GetExperiment(experimentName);
        if (experiment?.Rollout == null) return false;

        // Can only resume if paused or rolled back
        if (experiment.Rollout.Status != RolloutStatus.Paused &&
            experiment.Rollout.Status != RolloutStatus.RolledBack)
            return false;

        // If rolled back, reset all stages and start from the beginning
        if (experiment.Rollout.Status == RolloutStatus.RolledBack)
        {
            // Reset all stages to pending
            foreach (var stage in experiment.Rollout.Stages)
            {
                stage.Status = RolloutStageStatus.Pending;
                stage.ExecutedDate = null;
            }

            // Start with first stage
            var firstStage = experiment.Rollout.Stages.OrderBy(s => s.Percentage).FirstOrDefault();
            if (firstStage != null)
            {
                firstStage.Status = RolloutStageStatus.Active;
                firstStage.ExecutedDate = DateTime.UtcNow;
                experiment.Rollout.Percentage = firstStage.Percentage;
            }
        }

        experiment.Rollout.Status = RolloutStatus.InProgress;
        return true;
    }

    public bool RestartRollout(string experimentName)
    {
        var experiment = _experimentState.GetExperiment(experimentName);
        if (experiment?.Rollout == null) return false;

        // Reset all stages
        foreach (var stage in experiment.Rollout.Stages)
        {
            stage.Status = RolloutStageStatus.Pending;
            stage.ExecutedDate = null;
        }

        // Start with first stage
        var firstStage = experiment.Rollout.Stages.OrderBy(s => s.Percentage).FirstOrDefault();
        if (firstStage != null)
        {
            firstStage.Status = RolloutStageStatus.Active;
            firstStage.ExecutedDate = DateTime.UtcNow;
            experiment.Rollout.Percentage = firstStage.Percentage;
        }

        experiment.Rollout.Status = RolloutStatus.InProgress;
        if (experiment.Rollout.StartDate == null)
        {
            experiment.Rollout.StartDate = DateTime.UtcNow;
        }

        return true;
    }
}

// ============================================================================
// Targeting Service - Functional rule evaluation
// ============================================================================

public class TargetingService
{
    public string EvaluateVariant(string experimentName, UserContext userContext, ExperimentInfo experiment)
    {
        if (!experiment.TargetingRules.Any())
            return experiment.ActiveVariant;

        // Evaluate rules in priority order
        var matchedRule = experiment.TargetingRules
            .Where(r => r.Enabled)
            .OrderBy(r => r.Priority)
            .FirstOrDefault(r => EvaluateRule(r, userContext));

        return matchedRule?.Variant ?? experiment.ActiveVariant;
    }

    private bool EvaluateRule(TargetingRule rule, UserContext userContext)
    {
        // All conditions must match (AND logic)
        foreach (var condition in rule.Conditions)
        {
            if (!EvaluateCondition(condition, userContext))
                return false;
        }
        return true;
    }

    private bool EvaluateCondition(TargetingCondition condition, UserContext userContext)
    {
        if (!userContext.Attributes.TryGetValue(condition.Attribute, out var actualValue))
            return false;

        return condition.Operator.ToLowerInvariant() switch
        {
            "equals" => actualValue.Equals(condition.Value, StringComparison.OrdinalIgnoreCase),
            "in" => condition.Value.Split(',').Select(v => v.Trim()).Contains(actualValue, StringComparer.OrdinalIgnoreCase),
            "regex" => System.Text.RegularExpressions.Regex.IsMatch(actualValue, condition.Value),
            "greaterthan" => double.TryParse(actualValue, out var av) && double.TryParse(condition.Value, out var cv) && av > cv,
            "lessthan" => double.TryParse(actualValue, out var av2) && double.TryParse(condition.Value, out var cv2) && av2 < cv2,
            _ => false
        };
    }

    public UserContext GetUserContextFromRequest(HttpContext httpContext)
    {
        var userId = httpContext.Request.Headers["X-User-Id"].FirstOrDefault()
                     ?? httpContext.Connection.RemoteIpAddress?.ToString()
                     ?? Guid.NewGuid().ToString();

        var attributes = new Dictionary<string, string>
        {
            ["userId"] = userId,
            ["ipAddress"] = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        };

        // Extract from headers (for demo purposes)
        if (httpContext.Request.Headers.TryGetValue("X-User-Plan", out var plan))
            attributes["plan"] = plan.ToString();
        if (httpContext.Request.Headers.TryGetValue("X-User-Role", out var role))
            attributes["role"] = role.ToString();
        if (httpContext.Request.Headers.TryGetValue("X-User-Department", out var dept))
            attributes["department"] = dept.ToString();
        if (httpContext.Request.Headers.TryGetValue("X-User-Region", out var region))
            attributes["region"] = region.ToString();

        return new UserContext { UserId = userId, Attributes = attributes };
    }
}

// ============================================================================
// Metrics Collector - Real metrics tracking
// ============================================================================

public class MetricsCollector
{
    private readonly ConcurrentDictionary<string, ExperimentMetrics> _metrics = new();

    public void RecordVariantExposure(string experimentName, string variant, string userId)
    {
        var metrics = _metrics.GetOrAdd(experimentName, _ => new ExperimentMetrics());
        metrics.RecordExposure(variant, userId);
    }

    public void RecordConversion(string experimentName, string variant, string userId, string metricName, double value)
    {
        var metrics = _metrics.GetOrAdd(experimentName, _ => new ExperimentMetrics());
        metrics.RecordConversion(variant, userId, metricName, value);
    }

    public ExperimentMetrics? GetMetrics(string experimentName)
    {
        return _metrics.TryGetValue(experimentName, out var metrics) ? metrics : null;
    }

    public Dictionary<string, ExperimentMetrics> GetAllMetrics() =>
        _metrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
}

public class ExperimentMetrics
{
    private readonly ConcurrentDictionary<string, VariantMetrics> _variantMetrics = new();

    public void RecordExposure(string variant, string userId)
    {
        var metrics = _variantMetrics.GetOrAdd(variant, _ => new VariantMetrics());
        metrics.RecordExposure(userId);
    }

    public void RecordConversion(string variant, string userId, string metricName, double value)
    {
        var metrics = _variantMetrics.GetOrAdd(variant, _ => new VariantMetrics());
        metrics.RecordConversion(userId, metricName, value);
    }

    public VariantMetrics? GetVariantMetrics(string variant) =>
        _variantMetrics.TryGetValue(variant, out var metrics) ? metrics : null;

    public Dictionary<string, VariantMetrics> GetAllVariantMetrics() =>
        _variantMetrics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
}

public class VariantMetrics
{
    private readonly HashSet<string> _uniqueUsers = [];
    private readonly List<ConversionEvent> _conversions = [];
    private readonly object _lock = new();

    public void RecordExposure(string userId)
    {
        lock (_lock)
        {
            _uniqueUsers.Add(userId);
        }
    }

    public void RecordConversion(string userId, string metricName, double value)
    {
        lock (_lock)
        {
            _conversions.Add(new ConversionEvent
            {
                UserId = userId,
                MetricName = metricName,
                Value = value,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    public int UniqueUsers => _uniqueUsers.Count;
    public int TotalConversions => _conversions.Count;
    public double ConversionRate => UniqueUsers > 0 ? (double)_conversions.Select(c => c.UserId).Distinct().Count() / UniqueUsers : 0;
    public double AverageValue(string metricName) =>
        _conversions.Where(c => c.MetricName == metricName).Select(c => c.Value).DefaultIfEmpty(0).Average();
}

public class ConversionEvent
{
    public required string UserId { get; init; }
    public required string MetricName { get; init; }
    public double Value { get; init; }
    public DateTime Timestamp { get; init; }
}

// ============================================================================
// Hypothesis Testing Service - Real statistical calculations
// ============================================================================

public class HypothesisTestingService
{
    private readonly MetricsCollector _metricsCollector;

    public HypothesisTestingService(MetricsCollector metricsCollector)
    {
        _metricsCollector = metricsCollector;
    }

    public HypothesisResults? CalculateResults(string experimentName, HypothesisInfo hypothesis, string controlVariant)
    {
        var metrics = _metricsCollector.GetMetrics(experimentName);
        if (metrics == null) return null;

        var allVariants = metrics.GetAllVariantMetrics();
        var control = allVariants.TryGetValue(controlVariant, out var c) ? c : null;
        var treatment = allVariants.FirstOrDefault(kvp => kvp.Key != controlVariant).Value;

        if (control == null || treatment == null) return null;

        var controlSamples = control.UniqueUsers;
        var treatmentSamples = treatment.UniqueUsers;

        if (controlSamples < hypothesis.MinimumSampleSize / 2 ||
            treatmentSamples < hypothesis.MinimumSampleSize / 2)
            return null;

        // Use conversion rate as the metric
        var controlMean = control.ConversionRate;
        var treatmentMean = treatment.ConversionRate;

        // Calculate effect size (relative difference)
        var effectSize = controlMean > 0 ? (treatmentMean - controlMean) / controlMean : 0;

        // Simple z-test for proportions (approximation)
        var pooledProportion = (control.TotalConversions + treatment.TotalConversions) /
                              (double)(controlSamples + treatmentSamples);
        var standardError = Math.Sqrt(pooledProportion * (1 - pooledProportion) *
                                      (1.0 / controlSamples + 1.0 / treatmentSamples));
        var zScore = standardError > 0 ? (treatmentMean - controlMean) / standardError : 0;
        var pValue = CalculatePValue(zScore);

        // 95% confidence interval for difference
        var ciMargin = 1.96 * standardError;
        var difference = treatmentMean - controlMean;

        var isSignificant = pValue < hypothesis.AlphaLevel;

        var conclusion = isSignificant
            ? $"Treatment shows statistically significant improvement (p={pValue:0.####}, α={hypothesis.AlphaLevel}). " +
              $"{hypothesis.PrimaryMetric} increased by {effectSize:P1} " +
              $"(95% CI: {(difference - ciMargin):P2} to {(difference + ciMargin):P2}). Recommend full rollout."
            : $"No statistically significant difference detected (p={pValue:0.####}, α={hypothesis.AlphaLevel}). " +
              $"Observed effect of {effectSize:P1} is not sufficient to conclude superiority. " +
              $"Consider collecting more data or re-evaluating the hypothesis.";

        return new HypothesisResults
        {
            ControlSamples = controlSamples,
            TreatmentSamples = treatmentSamples,
            ControlMean = controlMean,
            TreatmentMean = treatmentMean,
            EffectSize = effectSize,
            PValue = pValue,
            ConfidenceIntervalLower = difference - ciMargin,
            ConfidenceIntervalUpper = difference + ciMargin,
            IsSignificant = isSignificant,
            Conclusion = conclusion,
            AnalyzedAt = DateTime.UtcNow
        };
    }

    private double CalculatePValue(double zScore)
    {
        // Two-tailed p-value approximation using normal distribution
        var absZ = Math.Abs(zScore);

        // Abramowitz and Stegun approximation for cumulative distribution function
        var t = 1.0 / (1.0 + 0.2316419 * absZ);
        var pdf = Math.Exp(-absZ * absZ / 2.0) / Math.Sqrt(2.0 * Math.PI);
        var cdf = 1.0 - pdf * (0.319381530 * t +
                              -0.356563782 * t * t +
                               1.781477937 * t * t * t +
                              -1.821255978 * t * t * t * t +
                               1.330274429 * t * t * t * t * t);

        return 2.0 * (1.0 - cdf); // Two-tailed
    }
}
