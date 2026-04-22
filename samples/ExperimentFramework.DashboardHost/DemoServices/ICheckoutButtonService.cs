namespace ExperimentFramework.DashboardHost.DemoServices;

public interface ICheckoutButtonService { string Describe(); }

public sealed class CheckoutButtonControl : ICheckoutButtonService
{
    public string Describe() => "Control";
}

public sealed class CheckoutButtonVariantA : ICheckoutButtonService
{
    public string Describe() => "Variant A";
}
