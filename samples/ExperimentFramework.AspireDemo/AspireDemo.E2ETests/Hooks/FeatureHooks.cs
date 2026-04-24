using Reqnroll;

namespace AspireDemo.E2ETests.Hooks;

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
        // Feature-level setup. Currently a placeholder — extend as needed.
        return Task.CompletedTask;
    }

    [AfterFeature]
    public static Task AfterFeature(FeatureContext featureContext)
    {
        // Feature-level teardown. Currently a placeholder — extend as needed.
        return Task.CompletedTask;
    }
}
