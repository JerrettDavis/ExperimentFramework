namespace ExperimentFramework.Benchmarks;

/// <summary>
/// Composition root for benchmark experiments to trigger source generation.
/// </summary>
public static class BenchmarkCompositionRoot
{
    [ExperimentCompositionRoot]
    public static ExperimentFrameworkBuilder ConfigureBenchmarkExperiments()
    {
        return ExperimentFrameworkBuilder.Create()
            .Define<ISimpleService>(c => c
                .UsingFeatureFlag("UseV2Service")
                .AddDefaultTrial<SimpleServiceV1>("false")
                .AddTrial<SimpleServiceV2>("true"))
            .Define<IGenericService<string>>(c => c
                .UsingFeatureFlag("UseV2GenericService")
                .AddDefaultTrial<GenericServiceV1<string>>("false")
                .AddTrial<GenericServiceV2<string>>("true"))
            .Define<IDatabase>(c => c
                .UsingFeatureFlag("UseCloudDb")
                .AddDefaultTrial<InMemoryDatabase>("false")
                .AddTrial<CloudDatabase>("true"))
            .Define<ICache>(c => c
                .UsingFeatureFlag("UseAdvancedCache")
                .AddDefaultTrial<SimpleCache>("false")
                .AddTrial<AdvancedCache>("true"));
    }
}
