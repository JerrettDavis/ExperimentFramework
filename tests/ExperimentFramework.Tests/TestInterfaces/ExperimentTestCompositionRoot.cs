namespace ExperimentFramework.Tests.TestInterfaces;

/// <summary>
/// Composition root for test experiments - triggers source generation.
/// </summary>
public static class ExperimentTestCompositionRoot
{
    [ExperimentCompositionRoot]
    public static ExperimentFrameworkBuilder ConfigureTestExperiments()
    {
        return ExperimentFrameworkBuilder.Create()
            // Async service tests
            .Define<IAsyncService>(c => c
                .UsingFeatureFlag("UseV2AsyncService")
                .AddDefaultTrial<AsyncServiceV1>("false")
                .AddTrial<AsyncServiceV2>("true"))

            // Generic repository tests
            .Define<IGenericRepository<TestEntity>>(c => c
                .UsingFeatureFlag("UseV2Repository")
                .AddDefaultTrial<GenericRepositoryV1<TestEntity>>("false")
                .AddTrial<GenericRepositoryV2<TestEntity>>("true"))

            // Nested generic service tests
            .Define<INestedGenericService>(c => c
                .UsingFeatureFlag("UseV2NestedGeneric")
                .AddDefaultTrial<NestedGenericServiceV1>("false")
                .AddTrial<NestedGenericServiceV2>("true"))

            // Common test service (used by ErrorPolicyTests, etc.)
            .Define<ITestService>(c => c
                .UsingFeatureFlag("UseFailingService")
                .AddDefaultTrial<StableService>("false")
                .AddTrial<FailingService>("true")
                .OnErrorRedirectAndReplayDefault())

            // Database tests (used by SelectionModeTests)
            .Define<IDatabase>(c => c
                .UsingFeatureFlag("UseCloudDb")
                .AddDefaultTrial<LocalDatabase>("false")
                .AddTrial<CloudDatabase>("true"))

            // Tax provider tests (used by SelectionModeTests, IntegrationTests)
            .Define<ITaxProvider>(c => c
                .UsingConfigurationKey("TaxProvider")
                .AddDefaultTrial<DefaultTaxProvider>("")
                .AddTrial<OkTaxProvider>("OK")
                .AddTrial<TxTaxProvider>("TX"))

            // Variant service tests (IntegrationTests, SelectionModeTests)
            .Define<IVariantService>(c => c
                .UsingCustomMode("StickyRouting", "UserVariant")
                .AddDefaultTrial<ControlVariant>("control")
                .AddTrial<VariantA>("variant-a")
                .AddTrial<VariantB>("variant-b"))

            // Simple service tests
            .Define<IMyService>(c => c
                .UsingFeatureFlag("UseV2Service")
                .AddDefaultTrial<MyServiceV1>("false")
                .AddTrial<MyServiceV2>("true"))

            // IOtherService for ExperimentFrameworkBuilderTests
            .Define<IOtherService>(c => c
                .UsingConfigurationKey("Experiments:TestConfig")
                .AddDefaultTrial<ServiceC>("")
                .AddTrial<ServiceD>("test-value"))

            // IVariantTestService for VariantFeatureManagerTests
            .Define<IVariantTestService>(c => c
                .UsingCustomMode("VariantFeatureFlag", "MyVariantFeature")
                .AddDefaultTrial<ControlService>("control")
                .AddTrial<VariantAService>("variant-a")
                .AddTrial<VariantBService>("variant-b"))

            // IRedirectSpecificService for RedirectAndReplay error policy tests
            .Define<IRedirectSpecificService>(c => c
                .UsingFeatureFlag("UsePrimaryService")
                .AddDefaultTrial<PrimaryService>("true")
                .AddTrial<SecondaryService>("false")
                .AddTrial<NoopFallbackService>("noop")
                .OnErrorRedirectAndReplay("noop"))

            // IRedirectOrderedService for RedirectAndReplayOrdered error policy tests
            .Define<IRedirectOrderedService>(c => c
                .UsingFeatureFlag("UseCloudService")
                .AddDefaultTrial<CloudService>("true")
                .AddTrial<LocalCacheService>("cache")
                .AddTrial<InMemoryCacheService>("memory")
                .AddTrial<StaticDataService>("static")
                .AddTrial<AlwaysFailsService1>("fail1")
                .AddTrial<AlwaysFailsService2>("fail2")
                .AddTrial<AlwaysFailsService3>("fail3")
                .OnErrorRedirectAndReplayOrdered("cache", "memory", "static"));
    }
}
