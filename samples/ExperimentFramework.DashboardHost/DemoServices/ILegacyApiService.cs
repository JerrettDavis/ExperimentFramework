namespace ExperimentFramework.DashboardHost.DemoServices;

public interface ILegacyApiService { string Describe(); }

public sealed class LegacyApiV1 : ILegacyApiService
{
    public string Describe() => "V1 API";
}

public sealed class LegacyApiV2 : ILegacyApiService
{
    public string Describe() => "V2 API";
}
