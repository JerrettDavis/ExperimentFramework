using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using ExperimentFramework.StickyRouting;
using ExperimentFramework.Tests.TestInterfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests;

[Feature("Selection modes determine how trials are chosen at runtime")]
public sealed class SelectionModeTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    private sealed record TestState(
        ServiceProvider ServiceProvider);

    private sealed record InvocationResult<T>(
        TestState State,
        T Result);

    // Helper to register all test services required by composition root
    private static void RegisterAllTestServices(IServiceCollection services)
    {
        // ITestService implementations
        services.AddScoped<StableService>();
        services.AddScoped<FailingService>();
        services.AddScoped<UnstableService>();
        services.AddScoped<AlsoFailingService>();
        services.AddScoped<ServiceA>();
        services.AddScoped<ServiceB>();

        // IDatabase implementations
        services.AddScoped<LocalDatabase>();
        services.AddScoped<CloudDatabase>();
        services.AddScoped<ControlDatabase>();
        services.AddScoped<ExperimentalDatabase>();

        // ITaxProvider implementations
        services.AddScoped<DefaultTaxProvider>();
        services.AddScoped<OkTaxProvider>();
        services.AddScoped<TxTaxProvider>();

        // IVariantService implementations
        services.AddScoped<ControlVariant>();
        services.AddScoped<ControlImpl>();
        services.AddScoped<VariantA>();
        services.AddScoped<VariantB>();

        // IMyService implementations
        services.AddScoped<MyServiceV1>();
        services.AddScoped<MyServiceV2>();

        // IOtherService implementations
        services.AddScoped<ServiceC>();
        services.AddScoped<ServiceD>();

        // IVariantTestService implementations
        services.AddScoped<ControlService>();
        services.AddScoped<VariantAService>();
        services.AddScoped<VariantBService>();

        // IAsyncService implementations
        services.AddScoped<AsyncServiceV1>();
        services.AddScoped<AsyncServiceV2>();

        // IGenericRepository implementations
        services.AddScoped<GenericRepositoryV1<TestEntity>>();
        services.AddScoped<GenericRepositoryV2<TestEntity>>();

        // INestedGenericService implementations
        services.AddScoped<NestedGenericServiceV1>();
        services.AddScoped<NestedGenericServiceV2>();

        // IRedirectSpecificService implementations
        services.AddScoped<PrimaryService>();
        services.AddScoped<SecondaryService>();
        services.AddScoped<NoopFallbackService>();

        // IRedirectOrderedService implementations
        services.AddScoped<CloudService>();
        services.AddScoped<LocalCacheService>();
        services.AddScoped<InMemoryCacheService>();
        services.AddScoped<StaticDataService>();
        services.AddScoped<AlwaysFailsService1>();
        services.AddScoped<AlwaysFailsService2>();
        services.AddScoped<AlwaysFailsService3>();

        // Default registrations
        services.AddScoped<ITestService, StableService>();
        services.AddScoped<IDatabase, LocalDatabase>();
        services.AddScoped<ITaxProvider, DefaultTaxProvider>();
        services.AddScoped<IVariantService, ControlVariant>();
        services.AddScoped<IMyService, MyServiceV1>();
        services.AddScoped<IOtherService, ServiceC>();
        services.AddScoped<IVariantTestService, ControlService>();
        services.AddScoped<IAsyncService, AsyncServiceV1>();
        services.AddScoped<IGenericRepository<TestEntity>, GenericRepositoryV1<TestEntity>>();
        services.AddScoped<INestedGenericService, NestedGenericServiceV1>();
        services.AddScoped<IRedirectSpecificService, PrimaryService>();
        services.AddScoped<IRedirectOrderedService, CloudService>();
    }

    private static TestState SetupBooleanFeatureFlag(bool featureEnabled)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UseCloudDb"] = featureEnabled.ToString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();

        RegisterAllTestServices(services);

        var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider());
    }

    private static TestState SetupConfigurationValue(string configValue)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["TaxProvider"] = configValue
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);

        RegisterAllTestServices(services);

        var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider());
    }

    private static TestState SetupStickyRouting(string identity)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureManagement:UserVariant"] = "false" // Fallback if no identity
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddFeatureManagement();
        services.AddExperimentStickyRouting(); // Register the StickyRouting provider
        services.AddScoped<IExperimentIdentityProvider>(_ => new TestIdentityProvider(identity));

        RegisterAllTestServices(services);

        var experiments = ExperimentTestCompositionRoot.ConfigureTestExperiments();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider());
    }

    private static InvocationResult<string> InvokeDatabase(TestState state)
    {
        using var scope = state.ServiceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IDatabase>();
        var result = db.GetName();
        return new InvocationResult<string>(state, result);
    }

    private static InvocationResult<decimal> InvokeTaxProvider(TestState state)
    {
        using var scope = state.ServiceProvider.CreateScope();
        var tax = scope.ServiceProvider.GetRequiredService<ITaxProvider>();
        var result = tax.CalculateTax(100m);
        return new InvocationResult<decimal>(state, result);
    }

    private static InvocationResult<string> InvokeTestService(TestState state)
    {
        using var scope = state.ServiceProvider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IVariantService>();
        var result = service.GetName();
        return new InvocationResult<string>(state, result);
    }

    [Scenario("Boolean feature flag routes to control when disabled")]
    [Fact]
    public Task Boolean_flag_false_routes_to_control()
        => Given("feature flag disabled", () => SetupBooleanFeatureFlag(false))
            .When("invoke database service", InvokeDatabase)
            .Then("local database is used", r => r.Result == "LocalDatabase")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Boolean feature flag routes to experiment when enabled")]
    [Fact]
    public Task Boolean_flag_true_routes_to_experiment()
        => Given("feature flag enabled", () => SetupBooleanFeatureFlag(true))
            .When("invoke database service", InvokeDatabase)
            .Then("cloud database is used", r => r.Result == "CloudDatabase")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Configuration value routes to default when empty")]
    [Fact]
    public Task Config_empty_routes_to_default()
        => Given("empty configuration value", () => SetupConfigurationValue(""))
            .When("invoke tax provider", InvokeTaxProvider)
            .Then("default tax provider is used", r => r.Result == 0m)
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Configuration value routes to OK provider")]
    [Fact]
    public Task Config_OK_routes_to_OK_provider()
        => Given("configuration value set to OK", () => SetupConfigurationValue("OK"))
            .When("invoke tax provider", InvokeTaxProvider)
            .Then("OK tax provider is used", r => r.Result == 4.5m)
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Configuration value routes to TX provider")]
    [Fact]
    public Task Config_TX_routes_to_TX_provider()
        => Given("configuration value set to TX", () => SetupConfigurationValue("TX"))
            .When("invoke tax provider", InvokeTaxProvider)
            .Then("TX tax provider is used", r => r.Result == 6.25m)
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Sticky routing provides consistent routing for same identity")]
    [Fact]
    public Task Sticky_routing_consistent_for_same_identity()
        => Given("sticky routing with identity", () => SetupStickyRouting("user-123"))
            .When("invoke service first time", InvokeTestService)
            .Then("variant is selected", r => !string.IsNullOrEmpty(r.Result))
            .When("invoke service second time", r => InvokeTestService(r.State))
            .Then("same variant selected", r =>
            {
                var firstCall = r.Result;
                var secondCall = InvokeTestService(r.State).Result;
                return firstCall == secondCall;
            })
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    [Scenario("Sticky routing distributes different identities")]
    [Fact]
    public Task Sticky_routing_distributes_identities()
        => Given("multiple identities", () => Enumerable.Range(1, 60).Select(i => $"user-{i}").ToList())
            .When("invoke service for each identity", identities =>
            {
                var results = new List<string>();
                foreach (var identity in identities)
                {
                    var state = SetupStickyRouting(identity);
                    var result = InvokeTestService(state);
                    results.Add(result.Result);
                    state.ServiceProvider.Dispose();
                }
                return results;
            })
            .Then("all variants are used", results =>
            {
                var variants = results.Distinct().ToList();
                return variants.Contains("ControlVariant") &&
                       variants.Contains("VariantA") &&
                       variants.Contains("VariantB");
            })
            .AssertPassed();

    [Scenario("OpenFeature selection falls back to default when not configured")]
    [Fact]
    public Task OpenFeature_falls_back_to_default()
        => Given("OpenFeature selection without provider", SetupOpenFeatureWithoutProvider)
            .When("invoke database service", InvokeDatabase)
            .Then("default database is used", r => r.Result == "LocalDatabase")
            .Finally(r => r.State.ServiceProvider.Dispose())
            .AssertPassed();

    private TestState SetupOpenFeatureWithoutProvider()
    {
        var services = new ServiceCollection();
        RegisterAllTestServices(services);

        var experiments = ExperimentFrameworkBuilder.Create()
            .Define<IDatabase>(c => c
                .UsingCustomMode("OpenFeature", "database-experiment")
                .AddDefaultTrial<LocalDatabase>("local")
                .AddTrial<CloudDatabase>("cloud")
                .OnErrorRedirectAndReplayDefault())
            .UseDispatchProxy();

        services.AddExperimentFramework(experiments);

        return new TestState(services.BuildServiceProvider());
    }

    // Test identity provider for sticky routing tests
    private sealed class TestIdentityProvider(string userId) : IExperimentIdentityProvider
    {
        public bool TryGetIdentity(out string identity)
        {
            identity = userId;
            return !string.IsNullOrEmpty(identity);
        }
    }
}

/// <summary>
/// Tests for selection mode infrastructure: registry, providers, factories, and attributes.
/// </summary>
public sealed class SelectionModeInfrastructureTests
{
    #region SelectionModeRegistry Tests

    [Fact]
    public void SelectionModeRegistry_Register_adds_factory()
    {
        var registry = new SelectionModeRegistry();
        var factory = new TestSelectionModeProviderFactory("TestMode");

        registry.Register(factory);

        Assert.True(registry.IsRegistered("TestMode"));
        Assert.Equal(1, registry.Count);
    }

    [Fact]
    public void SelectionModeRegistry_Register_throws_when_factory_null()
    {
        var registry = new SelectionModeRegistry();

        Assert.Throws<ArgumentNullException>(() => registry.Register(null!));
    }

    [Fact]
    public void SelectionModeRegistry_GetProvider_returns_provider()
    {
        var registry = new SelectionModeRegistry();
        var factory = new TestSelectionModeProviderFactory("TestMode");
        registry.Register(factory);

        var services = new ServiceCollection().BuildServiceProvider();
        var provider = registry.GetProvider("TestMode", services);

        Assert.NotNull(provider);
        Assert.Equal("TestMode", provider!.ModeIdentifier);
    }

    [Fact]
    public void SelectionModeRegistry_GetProvider_returns_null_for_unknown_mode()
    {
        var registry = new SelectionModeRegistry();
        var services = new ServiceCollection().BuildServiceProvider();

        var provider = registry.GetProvider("UnknownMode", services);

        Assert.Null(provider);
    }

    [Fact]
    public void SelectionModeRegistry_GetProvider_returns_null_for_empty_mode()
    {
        var registry = new SelectionModeRegistry();
        var services = new ServiceCollection().BuildServiceProvider();

        Assert.Null(registry.GetProvider("", services));
        Assert.Null(registry.GetProvider(null!, services));
    }

    [Fact]
    public void SelectionModeRegistry_IsRegistered_returns_false_for_empty()
    {
        var registry = new SelectionModeRegistry();

        Assert.False(registry.IsRegistered(""));
        Assert.False(registry.IsRegistered(null!));
    }

    [Fact]
    public void SelectionModeRegistry_RegisteredModes_returns_all_modes()
    {
        var registry = new SelectionModeRegistry();
        registry.Register(new TestSelectionModeProviderFactory("Mode1"));
        registry.Register(new TestSelectionModeProviderFactory("Mode2"));

        var modes = registry.RegisteredModes.ToList();

        Assert.Contains("Mode1", modes);
        Assert.Contains("Mode2", modes);
    }

    [Fact]
    public void SelectionModeRegistry_is_case_insensitive()
    {
        var registry = new SelectionModeRegistry();
        registry.Register(new TestSelectionModeProviderFactory("TestMode"));

        Assert.True(registry.IsRegistered("testmode"));
        Assert.True(registry.IsRegistered("TESTMODE"));
        Assert.True(registry.IsRegistered("TestMode"));
    }

    #endregion

    #region SelectionModeAttribute Tests

    [Fact]
    public void SelectionModeAttribute_stores_mode_identifier()
    {
        var attr = new SelectionModeAttribute("CustomMode");

        Assert.Equal("CustomMode", attr.ModeIdentifier);
    }

    [Fact]
    public void SelectionModeAttribute_throws_when_null()
    {
        Assert.Throws<ArgumentNullException>(() => new SelectionModeAttribute(null!));
    }

    #endregion

    #region SelectionModeProviderFactory Tests

    [Fact]
    public void SelectionModeProviderFactory_generic_reads_attribute()
    {
        var factory = new SelectionModeProviderFactory<TestAttributedProvider>();

        Assert.Equal("TestAttributed", factory.ModeIdentifier);
    }

    [Fact]
    public void SelectionModeProviderFactory_generic_creates_provider()
    {
        var factory = new SelectionModeProviderFactory<TestAttributedProvider>();
        var services = new ServiceCollection().BuildServiceProvider();

        var provider = factory.Create(services);

        Assert.NotNull(provider);
        Assert.IsType<TestAttributedProvider>(provider);
    }

    [Fact]
    public void SelectionModeProviderFactory_with_explicit_identifier()
    {
        var factory = new SelectionModeProviderFactory<TestAttributedProvider>("OverriddenMode");

        Assert.Equal("OverriddenMode", factory.ModeIdentifier);
    }

    [Fact]
    public void SelectionModeProviderFactory_throws_when_no_attribute()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new SelectionModeProviderFactory<TestNonAttributedProvider>());
    }

    #endregion

    #region SelectionModeProviderBase Tests

    [Fact]
    public void SelectionModeProviderBase_reads_attribute()
    {
        var provider = new TestAttributedProvider();

        Assert.Equal("TestAttributed", provider.ModeIdentifier);
    }

    [Fact]
    public void SelectionModeProviderBase_with_explicit_identifier()
    {
        var provider = new TestExplicitIdentifierProvider("ExplicitMode");

        Assert.Equal("ExplicitMode", provider.ModeIdentifier);
    }

    [Fact]
    public void SelectionModeProviderBase_GetDefaultSelectorName_uses_convention()
    {
        var provider = new TestAttributedProvider();
        var convention = new DefaultExperimentNamingConvention();

        var name = provider.GetDefaultSelectorName(typeof(ITestService), convention);

        Assert.NotEmpty(name);
    }

    #endregion

    #region AddSelectionModeProvider Tests

    [Fact]
    public void AddSelectionModeProvider_registers_provider()
    {
        var services = new ServiceCollection();
        services.AddSelectionModeProvider<TestAttributedProvider>();

        var sp = services.BuildServiceProvider();
        var factory = sp.GetService<ISelectionModeProviderFactory>();

        Assert.NotNull(factory);
        Assert.Equal("TestAttributed", factory!.ModeIdentifier);
    }

    [Fact]
    public void AddSelectionModeProvider_with_explicit_mode_registers_provider()
    {
        var services = new ServiceCollection();
        services.AddSelectionModeProvider<TestAttributedProvider>("CustomMode");

        var sp = services.BuildServiceProvider();
        var factory = sp.GetService<ISelectionModeProviderFactory>();

        Assert.NotNull(factory);
        Assert.Equal("CustomMode", factory!.ModeIdentifier);
    }

    #endregion

    #region Test Support Classes

    [SelectionMode("TestAttributed")]
    private sealed class TestAttributedProvider : SelectionModeProviderBase
    {
        public override ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
            => ValueTask.FromResult<string?>(context.DefaultKey);
    }

    private sealed class TestNonAttributedProvider : ISelectionModeProvider
    {
        public string ModeIdentifier => "NonAttributed";

        public ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
            => ValueTask.FromResult<string?>(context.DefaultKey);

        public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
            => convention.FeatureFlagNameFor(serviceType);
    }

    private sealed class TestExplicitIdentifierProvider : SelectionModeProviderBase
    {
        public TestExplicitIdentifierProvider(string modeIdentifier) : base(modeIdentifier) { }

        public override ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
            => ValueTask.FromResult<string?>(context.DefaultKey);
    }

    private sealed class TestSelectionModeProviderFactory(string modeIdentifier) : ISelectionModeProviderFactory
    {
        public string ModeIdentifier => modeIdentifier;

        public ISelectionModeProvider Create(IServiceProvider scopedProvider)
            => new TestProvider(modeIdentifier);

        private sealed class TestProvider(string modeIdentifier) : ISelectionModeProvider
        {
            public string ModeIdentifier => modeIdentifier;

            public ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
                => ValueTask.FromResult<string?>(context.DefaultKey);

            public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
                => convention.FeatureFlagNameFor(serviceType);
        }
    }

    #endregion
}

/// <summary>
/// Tests for SelectionRule model.
/// </summary>
public sealed class SelectionRuleTests
{
    [Fact]
    public void SelectionRule_can_be_created_with_all_properties()
    {
        var predicate = new Func<IServiceProvider, bool>(_ => true);
        var segments = new[] { "premium", "beta" };

        var rule = new Models.SelectionRule
        {
            Mode = Models.SelectionMode.BooleanFeatureFlag,
            SelectorName = "TestFeature",
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(30),
            ActivationPredicate = predicate,
            PercentageAllocation = 0.5,
            UserSegments = segments
        };

        Assert.Equal(Models.SelectionMode.BooleanFeatureFlag, rule.Mode);
        Assert.Equal("TestFeature", rule.SelectorName);
        Assert.NotNull(rule.StartTime);
        Assert.NotNull(rule.EndTime);
        Assert.Same(predicate, rule.ActivationPredicate);
        Assert.Equal(0.5, rule.PercentageAllocation);
        Assert.Equal(segments, rule.UserSegments);
    }

    [Fact]
    public void SelectionRule_FromRegistration_creates_minimal_rule()
    {
        var rule = Models.SelectionRule.FromRegistration(
            Models.SelectionMode.ConfigurationValue,
            "TaxProvider");

        Assert.Equal(Models.SelectionMode.ConfigurationValue, rule.Mode);
        Assert.Equal("TaxProvider", rule.SelectorName);
        Assert.Null(rule.StartTime);
        Assert.Null(rule.EndTime);
        Assert.Null(rule.ActivationPredicate);
        Assert.Null(rule.PercentageAllocation);
        Assert.Null(rule.UserSegments);
    }
}

/// <summary>
/// Tests for ExperimentRegistration model.
/// </summary>
public sealed class ExperimentRegistrationModelTests
{
    [Fact]
    public void ExperimentRegistration_with_all_properties()
    {
        var registration = new Models.ExperimentRegistration
        {
            ServiceType = typeof(IDatabase),
            DefaultKey = "control",
            Trials = new Dictionary<string, Type>
            {
                ["control"] = typeof(LocalDatabase),
                ["variant"] = typeof(CloudDatabase)
            },
            Mode = Models.SelectionMode.BooleanFeatureFlag,
            ModeIdentifier = "BooleanFeatureFlag",
            SelectorName = "TestFeature",
            OnErrorPolicy = Models.OnErrorPolicy.RedirectAndReplayDefault,
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(30),
            ActivationPredicate = _ => true,
            FallbackTrialKey = "control",
            OrderedFallbackKeys = new List<string> { "control", "variant" }
        };

        Assert.Equal(typeof(IDatabase), registration.ServiceType);
        Assert.Equal("control", registration.DefaultKey);
        Assert.Equal(2, registration.Trials.Count);
        Assert.Equal(Models.SelectionMode.BooleanFeatureFlag, registration.Mode);
        Assert.Equal("BooleanFeatureFlag", registration.ModeIdentifier);
        Assert.Equal("TestFeature", registration.SelectorName);
        Assert.Equal(Models.OnErrorPolicy.RedirectAndReplayDefault, registration.OnErrorPolicy);
        Assert.NotNull(registration.StartTime);
        Assert.NotNull(registration.EndTime);
        Assert.NotNull(registration.ActivationPredicate);
        Assert.Equal("control", registration.FallbackTrialKey);
        Assert.Equal(2, registration.OrderedFallbackKeys!.Count);
    }
}

/// <summary>
/// Tests for Experiment and Trial models.
/// </summary>
public sealed class ExperimentAndTrialModelTests
{
    [Fact]
    public void Experiment_can_be_created()
    {
        var selectionRule = new Models.SelectionRule
        {
            Mode = Models.SelectionMode.BooleanFeatureFlag,
            SelectorName = "TestFeature"
        };

        var behaviorRule = new Models.BehaviorRule
        {
            OnErrorPolicy = Models.OnErrorPolicy.Throw
        };

        var trial = new Models.Trial
        {
            ServiceType = typeof(IDatabase),
            ControlKey = "control",
            ControlType = typeof(LocalDatabase),
            Conditions = new Dictionary<string, Type>
            {
                ["variant"] = typeof(CloudDatabase)
            },
            SelectionRule = selectionRule,
            BehaviorRule = behaviorRule
        };

        var experiment = new Models.Experiment
        {
            Name = "test-experiment",
            Trials = new List<Models.Trial> { trial },
            StartTime = DateTimeOffset.UtcNow,
            EndTime = DateTimeOffset.UtcNow.AddDays(30)
        };

        Assert.Equal("test-experiment", experiment.Name);
        Assert.Single(experiment.Trials);
        Assert.NotNull(experiment.StartTime);
        Assert.NotNull(experiment.EndTime);
    }

    [Fact]
    public void Trial_AllImplementations_includes_control_and_conditions()
    {
        var trial = new Models.Trial
        {
            ServiceType = typeof(IDatabase),
            ControlKey = "control",
            ControlType = typeof(LocalDatabase),
            Conditions = new Dictionary<string, Type>
            {
                ["cloud"] = typeof(CloudDatabase)
            },
            SelectionRule = new Models.SelectionRule
            {
                Mode = Models.SelectionMode.BooleanFeatureFlag,
                SelectorName = "TestFeature"
            },
            BehaviorRule = Models.BehaviorRule.Default
        };

        var allImpl = trial.AllImplementations;

        Assert.Equal(2, allImpl.Count);
        Assert.Equal(typeof(LocalDatabase), allImpl["control"]);
        Assert.Equal(typeof(CloudDatabase), allImpl["cloud"]);
    }

    [Fact]
    public void Trial_ToString_returns_summary()
    {
        var trial = new Models.Trial
        {
            ServiceType = typeof(IDatabase),
            ControlKey = "control",
            ControlType = typeof(LocalDatabase),
            Conditions = new Dictionary<string, Type>
            {
                ["cloud"] = typeof(CloudDatabase)
            },
            SelectionRule = new Models.SelectionRule
            {
                Mode = Models.SelectionMode.BooleanFeatureFlag,
                SelectorName = "TestFeature"
            },
            BehaviorRule = Models.BehaviorRule.Default
        };

        var str = trial.ToString();

        Assert.Contains("Trial<IDatabase>", str);
        Assert.Contains("control='control'", str);
        Assert.Contains("cloud", str);
    }

    [Fact]
    public void Experiment_ToString_returns_summary()
    {
        var trial = new Models.Trial
        {
            ServiceType = typeof(IDatabase),
            ControlKey = "control",
            ControlType = typeof(LocalDatabase),
            Conditions = new Dictionary<string, Type>(),
            SelectionRule = new Models.SelectionRule
            {
                Mode = Models.SelectionMode.BooleanFeatureFlag,
                SelectorName = "TestFeature"
            },
            BehaviorRule = Models.BehaviorRule.Default
        };

        var experiment = new Models.Experiment
        {
            Name = "my-experiment",
            Trials = new List<Models.Trial> { trial }
        };

        var str = experiment.ToString();

        Assert.Contains("my-experiment", str);
        Assert.Contains("IDatabase", str);
    }

    [Fact]
    public void BehaviorRule_Default_returns_throw_policy()
    {
        var rule = Models.BehaviorRule.Default;

        Assert.Equal(Models.OnErrorPolicy.Throw, rule.OnErrorPolicy);
        Assert.Null(rule.FallbackConditionKey);
        Assert.Null(rule.OrderedFallbackKeys);
    }

    [Fact]
    public void BehaviorRule_FromRegistration_creates_rule()
    {
        var rule = Models.BehaviorRule.FromRegistration(
            Models.OnErrorPolicy.RedirectAndReplay,
            "fallback",
            null);

        Assert.Equal(Models.OnErrorPolicy.RedirectAndReplay, rule.OnErrorPolicy);
        Assert.Equal("fallback", rule.FallbackConditionKey);
    }

    [Fact]
    public void BehaviorRule_FromRegistration_with_ordered_keys()
    {
        var keys = new List<string> { "first", "second", "third" };

        var rule = Models.BehaviorRule.FromRegistration(
            Models.OnErrorPolicy.RedirectAndReplayOrdered,
            null,
            keys);

        Assert.Equal(Models.OnErrorPolicy.RedirectAndReplayOrdered, rule.OnErrorPolicy);
        Assert.Equal(3, rule.OrderedFallbackKeys!.Count);
    }
}
