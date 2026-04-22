namespace ExperimentFramework.DashboardHost.DemoServices;

public interface IHomepageLayoutService { string Describe(); }

public sealed class HomepageLayoutControl : IHomepageLayoutService
{
    public string Describe() => "Current";
}

public sealed class HomepageLayoutFallHero : IHomepageLayoutService
{
    public string Describe() => "Fall Hero";
}
