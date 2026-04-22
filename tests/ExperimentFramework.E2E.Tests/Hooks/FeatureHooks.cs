using Reqnroll;

namespace ExperimentFramework.E2E.Tests.Hooks;

[Binding]
public class FeatureHooks
{
    private readonly FeatureContext _featureContext;

    public FeatureHooks(FeatureContext featureContext)
    {
        _featureContext = featureContext;
    }

    [BeforeFeature]
    public static Task BeforeFeature(FeatureContext featureContext)
    {
        // Feature-level setup. Currently a placeholder — extend as needed
        // (e.g., seed shared feature-scoped test data, start a test server, etc.).
        return Task.CompletedTask;
    }

    [AfterFeature]
    public static Task AfterFeature(FeatureContext featureContext)
    {
        // Feature-level teardown. Currently a placeholder — extend as needed
        // (e.g., clean up shared feature-scoped test data, stop a test server, etc.).
        return Task.CompletedTask;
    }
}
