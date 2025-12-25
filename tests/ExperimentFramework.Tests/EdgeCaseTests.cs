using ExperimentFramework.Decorators;
using ExperimentFramework.StickyRouting;
using ExperimentFramework.Telemetry;
using ExperimentFramework.Tests.TestInterfaces;
using ExperimentFramework.Validation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests;

[Feature("Edge cases and error paths are handled correctly")]
public sealed class EdgeCaseTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    // Helper to register common test services
    private static void RegisterCommonServices(IServiceCollection services)
    {
        services.AddScoped<StableService>();
        services.AddScoped<FailingService>();
        services.AddScoped<ServiceA>();
        services.AddScoped<ServiceB>();
        services.AddScoped<ITestService, StableService>();
    }

    [Scenario("AddExperimentFramework throws when services is null")]
    [Fact]
    public void AddExperimentFramework_throws_on_null_services()
    {
        IServiceCollection? services = null;
        var builder = ExperimentFrameworkBuilder.Create();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            services!.AddExperimentFramework(builder));

        Assert.Equal("services", ex.ParamName);
    }

    [Scenario("AddExperimentFramework throws when builder is null")]
    [Fact]
    public void AddExperimentFramework_throws_on_null_builder()
    {
        var services = new ServiceCollection();
        ExperimentFrameworkBuilder? builder = null;

        var ex = Assert.Throws<ArgumentNullException>(() =>
            services.AddExperimentFramework(builder!));

        Assert.Equal("builder", ex.ParamName);
    }

    [Scenario("AddExperimentFramework throws when service not registered")]
    [Fact]
    public void AddExperimentFramework_throws_when_service_not_registered()
    {
        var services = new ServiceCollection();
        // NOT registering ITestService

        var builder = ExperimentFrameworkBuilder.Create();
        builder.Define<ITestService>(c => c
            .UsingFeatureFlag("Test")
            .AddDefaultTrial<ServiceA>("false")
            .AddTrial<ServiceB>("true"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddExperimentFramework(builder));

        Assert.Contains("must be registered before calling AddExperimentFramework", ex.Message);
        Assert.Contains("ITestService", ex.Message);
    }

    [Scenario("AddExperimentFramework throws when no generated proxy found")]
    [Fact]
    public void AddExperimentFramework_throws_when_no_proxy_generated()
    {
        // Create a service with NO composition root or UseSourceGenerators()
        // This should fail because no proxy will be generated

        // First create a minimal non-test interface that won't have a proxy
        var services = new ServiceCollection();
        services.AddScoped<INonExistentTestService, NonExistentServiceImpl>();

        var builder = ExperimentFrameworkBuilder.Create();
        builder.Define<INonExistentTestService>(c => c
            .UsingFeatureFlag("Test")
            .AddDefaultTrial<NonExistentServiceImpl>("false"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddExperimentFramework(builder));

        Assert.Contains("No source-generated proxy found", ex.Message);
        Assert.Contains("INonExistentTestService", ex.Message);
    }

    [Scenario("ServiceExperimentBuilder throws when no trials configured")]
    [Fact]
    public void ServiceExperimentBuilder_throws_when_no_trials()
    {
        var builder = ExperimentFrameworkBuilder.Create();

        // Define an experiment but don't add any trials
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            builder.Define<ITestService>(c =>
            {
                c.UsingFeatureFlag("Test");
                // No AddDefaultTrial or AddTrial calls
            });
        });

        Assert.Contains("No trials were configured", ex.Message);
        Assert.Contains("ITestService", ex.Message);
    }

    [Scenario("ServiceExperimentBuilder uses first trial as default when not specified")]
    [Fact]
    public void ServiceExperimentBuilder_uses_first_trial_as_default()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:TestFeature"] = "false"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        RegisterCommonServices(services);

        var builder = ExperimentFrameworkBuilder.Create();
        builder.Define<ITestService>(c => c
            .UsingFeatureFlag("TestFeature")
            .AddTrial<ServiceA>("alpha")  // First trial, no AddDefaultTrial
            .AddTrial<ServiceB>("beta"));

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ITestService>();
        var result = service.Execute();

        // Should use first trial (alpha/ServiceA) as default
        Assert.Equal("ServiceA", result);

        sp.Dispose();
    }

    [Scenario("StickyTrialRouter throws when no trial keys")]
    [Fact]
    public void StickyTrialRouter_throws_when_no_trial_keys()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            StickyTrialRouter.SelectTrial("user123", "experiment", []));

        Assert.Contains("No trial keys available", ex.Message);
    }

    [Scenario("StickyTrialRouter handles single trial key")]
    [Fact]
    public void StickyTrialRouter_handles_single_trial()
    {
        var result = StickyTrialRouter.SelectTrial("user123", "experiment", ["only-trial"]);

        Assert.Equal("only-trial", result);
    }

    [Scenario("StickyTrialRouter produces consistent results")]
    [Fact]
    public void StickyTrialRouter_is_deterministic()
    {
        var trialKeys = new[] { "control", "variant-a", "variant-b" };

        var result1 = StickyTrialRouter.SelectTrial("user123", "exp1", trialKeys);
        var result2 = StickyTrialRouter.SelectTrial("user123", "exp1", trialKeys);
        var result3 = StickyTrialRouter.SelectTrial("user123", "exp1", trialKeys);

        Assert.Equal(result1, result2);
        Assert.Equal(result2, result3);
    }

    [Scenario("StickyTrialRouter isolates experiments by selector name")]
    [Fact]
    public void StickyTrialRouter_isolates_by_selector_name()
    {
        var trialKeys = new[] { "control", "variant-a", "variant-b" };

        var result1 = StickyTrialRouter.SelectTrial("user123", "experiment1", trialKeys);
        var result2 = StickyTrialRouter.SelectTrial("user123", "experiment2", trialKeys);

        // Different selector names should potentially select different trials
        // We can't assert they're different (hash might collide), but we can assert they're valid
        Assert.Contains(result1, trialKeys);
        Assert.Contains(result2, trialKeys);
    }

    [Scenario("Missing configuration value falls back to default")]
    [Fact]
    public Task Missing_config_value_uses_default()
        => Given("service with missing config", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())  // No TestConfig key
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            RegisterCommonServices(services);

            var builder = ExperimentFrameworkBuilder.Create();
            builder.Define<ITestService>(c => c
                .UsingConfigurationKey("TestConfig")
                .AddDefaultTrial<ServiceA>("alpha")
                .AddTrial<ServiceB>("beta"));

            services.AddExperimentFramework(builder);
            return services.BuildServiceProvider();
        })
        .When("invoke service", sp =>
        {
            using var scope = sp.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestService>();
            return (sp, service.Execute());
        })
        .Then("uses default trial", r => r.Item2 == "ServiceA")
        .Finally(r => r.sp.Dispose())
        .AssertPassed();

    [Scenario("Empty configuration value falls back to default")]
    [Fact]
    public Task Empty_config_value_uses_default()
        => Given("service with empty config", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestConfig"] = ""  // Empty string
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            RegisterCommonServices(services);

            var builder = ExperimentFrameworkBuilder.Create();
            builder.Define<ITestService>(c => c
                .UsingConfigurationKey("TestConfig")
                .AddDefaultTrial<ServiceA>("alpha")
                .AddTrial<ServiceB>("beta"));

            services.AddExperimentFramework(builder);
            return services.BuildServiceProvider();
        })
        .When("invoke service", sp =>
        {
            using var scope = sp.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestService>();
            return (sp, service.Execute());
        })
        .Then("uses default trial", r => r.Item2 == "ServiceA")
        .Finally(r => r.sp.Dispose())
        .AssertPassed();

    [Scenario("UseCustomProxy method can be called")]
    [Fact]
    public void UseCustomProxy_method_is_callable()
    {
        var builder = ExperimentFrameworkBuilder.Create();

        // Just verify the method exists and can be called
        var result = builder.UseCustomProxy<StableService>();

        Assert.NotNull(result);
        Assert.Same(builder, result);  // Should return same builder for chaining
    }

    [Scenario("AddOpenTelemetryExperimentTracking registers telemetry")]
    [Fact]
    public void AddOpenTelemetryExperimentTracking_registers_telemetry()
    {
        var services = new ServiceCollection();
        services.AddOpenTelemetryExperimentTracking();

        var sp = services.BuildServiceProvider();
        var telemetry = sp.GetRequiredService<IExperimentTelemetry>();

        Assert.NotNull(telemetry);
        Assert.IsType<OpenTelemetryExperimentTelemetry>(telemetry);

        sp.Dispose();
    }

    [Scenario("NoopExperimentTelemetry can be instantiated")]
    [Fact]
    public void NoopExperimentTelemetry_works()
    {
        var telemetry = NoopExperimentTelemetry.Instance;

        var scope = telemetry.StartInvocation(
            typeof(ITestService),
            "Execute",
            "test",
            "control",
            ["control", "variant"]);

        scope.RecordSuccess();
        scope.RecordFailure(new Exception("test"));
        scope.RecordFallback("fallback");
        scope.RecordVariant("variant", "source");
        scope.Dispose();

        // Should not throw
        Assert.NotNull(telemetry);
    }

    [Scenario("ExperimentRegistry handles multiple registrations")]
    [Fact]
    public void ExperimentRegistry_handles_multiple_types()
    {
        var config = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        RegisterCommonServices(services);
        services.AddScoped<ServiceC>();
        services.AddScoped<ServiceD>();
        services.AddScoped<IOtherService, ServiceC>();

        var builder = ExperimentFrameworkBuilder.Create();
        builder.Define<ITestService>(c => c
            .UsingFeatureFlag("F1")
            .AddDefaultTrial<ServiceA>("false")
            .AddTrial<ServiceB>("true"));
        builder.Define<IOtherService>(c => c
            .UsingFeatureFlag("F2")
            .AddDefaultTrial<ServiceC>("false")
            .AddTrial<ServiceD>("true"));

        services.AddExperimentFramework(builder);
        var sp = services.BuildServiceProvider();

        // Both should be registered
        using var scope = sp.CreateScope();
        var service1 = scope.ServiceProvider.GetRequiredService<ITestService>();
        var service2 = scope.ServiceProvider.GetRequiredService<IOtherService>();

        Assert.NotNull(service1);
        Assert.NotNull(service2);

        sp.Dispose();
    }

    [Scenario("AddDecoratorFactory registers custom decorator")]
    [Fact]
    public void AddDecoratorFactory_registers_decorator()
    {
        var builder = ExperimentFrameworkBuilder.Create();
        var decoratorFactory = new BenchmarkDecoratorFactory();

        var result = builder.AddDecoratorFactory(decoratorFactory);

        Assert.NotNull(result);
        Assert.Same(builder, result);  // Should return same builder for chaining
    }

    [Scenario("OnErrorRedirectAndReplayAny tries all trials")]
    [Fact]
    public Task OnErrorRedirectAndReplayAny_tries_all_trials()
        => Given("service with RedirectAndReplayAny policy", () =>
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["FeatureManagement:TestFailover"] = "true"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton<IConfiguration>(config);
            services.AddFeatureManagement();
            RegisterCommonServices(services);

            var builder = ExperimentFrameworkBuilder.Create();
            builder.Define<ITestService>(c => c
                .UsingFeatureFlag("TestFailover")
                .AddDefaultTrial<StableService>("false")
                .AddTrial<FailingService>("true")
                .OnErrorRedirectAndReplayAny());  // Try all trials if one fails

            services.AddExperimentFramework(builder);
            return services.BuildServiceProvider();
        })
        .When("invoke service with failing trial", sp =>
        {
            using var scope = sp.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ITestService>();
            return (sp, service.Execute());
        })
        .Then("falls back to working trial", r => r.Item2 == "StableService")
        .Finally(r => r.sp.Dispose())
        .AssertPassed();
}

// Test service for the "no proxy" scenario
public interface INonExistentTestService
{
    string Execute();
}

public class NonExistentServiceImpl : INonExistentTestService
{
    public string Execute() => "NonExistent";
}

/// <summary>
/// Tests for TrialConflictException.
/// </summary>
public sealed class TrialConflictExceptionTests
{
    [Fact]
    public void TrialConflictException_with_single_conflict()
    {
        var conflict = new TrialConflict
        {
            Type = TrialConflictType.DuplicateServiceRegistration,
            ServiceType = typeof(TestInterfaces.ITestService),
            Description = "Duplicate registration for ITestService with variant-a"
        };

        var exception = new TrialConflictException(conflict);

        Assert.Contains("ITestService", exception.Message);
        Assert.Contains("variant-a", exception.Message);
        Assert.Single(exception.Conflicts);
    }

    [Fact]
    public void TrialConflictException_with_multiple_conflicts()
    {
        var conflicts = new List<TrialConflict>
        {
            new TrialConflict
            {
                Type = TrialConflictType.DuplicateServiceRegistration,
                ServiceType = typeof(TestInterfaces.ITestService),
                Description = "Duplicate registration for ITestService"
            },
            new TrialConflict
            {
                Type = TrialConflictType.OverlappingTimeWindows,
                ServiceType = typeof(TestInterfaces.IDatabase),
                Description = "Overlapping time windows for IDatabase"
            }
        };

        var exception = new TrialConflictException(conflicts);

        Assert.Contains("ITestService", exception.Message);
        Assert.Contains("IDatabase", exception.Message);
        Assert.Equal(2, exception.Conflicts.Count);
    }

    [Fact]
    public void TrialConflictException_throws_when_null_list()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new TrialConflictException((IReadOnlyList<TrialConflict>)null!));
    }

    [Fact]
    public void TrialConflict_has_all_properties()
    {
        var conflict = new TrialConflict
        {
            Type = TrialConflictType.InvalidFallbackKey,
            ServiceType = typeof(TestInterfaces.IDatabase),
            Description = "Invalid fallback key 'cloud' for IDatabase",
            ExperimentNames = new[] { "exp1", "exp2" }
        };

        Assert.Equal(typeof(TestInterfaces.IDatabase), conflict.ServiceType);
        Assert.Equal(TrialConflictType.InvalidFallbackKey, conflict.Type);
        Assert.Contains("cloud", conflict.Description);
        Assert.Equal(2, conflict.ExperimentNames!.Count);
    }

    [Fact]
    public void TrialConflictType_has_expected_values()
    {
        Assert.Equal(0, (int)TrialConflictType.OverlappingTimeWindows);
        Assert.Equal(1, (int)TrialConflictType.ExcessivePercentageAllocation);
        Assert.Equal(2, (int)TrialConflictType.DuplicateServiceRegistration);
        Assert.Equal(3, (int)TrialConflictType.InvalidFallbackKey);
    }
}
