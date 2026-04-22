namespace ExperimentFramework.DashboardHost.DemoServices;

public interface ISearchRankerService { string Describe(); }

public sealed class SearchBaseline : ISearchRankerService
{
    public string Describe() => "Baseline";
}

public sealed class SearchMlV1 : ISearchRankerService
{
    public string Describe() => "ML v1";
}

public sealed class SearchMlV2 : ISearchRankerService
{
    public string Describe() => "ML v2";
}
