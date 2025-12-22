namespace ExperimentFramework.Tests;

// Dedicated interfaces for fluent API testing (no conflict with attribute-based tests)
public interface IFluentService
{
    string Execute();
}

public class FluentServiceV1 : IFluentService
{
    public string Execute() => "FluentServiceV1";
}

public class FluentServiceV2 : IFluentService
{
    public string Execute() => "FluentServiceV2";
}

/// <summary>
/// Demonstrates using the fluent API approach with .UseSourceGenerators()
/// to trigger compile-time proxy generation.
/// </summary>
public static class FluentApiCompositionRoot
{
    /// <summary>
    /// Configures experiments using the fluent API marker (.UseSourceGenerators()).
    /// This method does NOT use the [ExperimentCompositionRoot] attribute.
    /// </summary>
    public static ExperimentFrameworkBuilder ConfigureFluentApiExperiments()
        => ExperimentFrameworkBuilder.Create()
            .Define<IFluentService>(c => c
                .UsingFeatureFlag("UseFluentV2")
                .AddDefaultTrial<FluentServiceV1>("false")
                .AddTrial<FluentServiceV2>("true")
                .OnErrorRedirectAndReplayDefault())
            .UseSourceGenerators(); // Fluent API marker - triggers source generation
}
