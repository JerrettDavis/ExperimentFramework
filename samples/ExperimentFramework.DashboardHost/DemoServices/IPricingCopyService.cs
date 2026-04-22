namespace ExperimentFramework.DashboardHost.DemoServices;

public interface IPricingCopyService { string Describe(); }

public sealed class PricingCopyOriginal : IPricingCopyService
{
    public string Describe() => "Original";
}

public sealed class PricingCopyBenefitsLed : IPricingCopyService
{
    public string Describe() => "Benefits-Led";
}
