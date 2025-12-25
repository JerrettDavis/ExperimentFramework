namespace ExperimentFramework.SampleWebApp.Services;

/// <summary>
/// Service that handles different checkout flow experiences.
/// This demonstrates A/B testing different UX flows.
/// </summary>
public interface ICheckoutFlow
{
    /// <summary>
    /// Gets the checkout steps for this flow.
    /// </summary>
    CheckoutInfo GetCheckoutInfo();
}

public record CheckoutInfo(string FlowName, string[] Steps, int EstimatedTimeSeconds);

/// <summary>
/// Traditional multi-step checkout (Control).
/// </summary>
public class StandardCheckoutFlow : ICheckoutFlow
{
    public CheckoutInfo GetCheckoutInfo()
    {
        return new CheckoutInfo(
            "Standard Checkout",
            ["Cart Review", "Shipping Address", "Payment Method", "Review & Confirm", "Complete"],
            90);
    }
}

/// <summary>
/// Streamlined express checkout (Variant A).
/// </summary>
public class ExpressCheckoutFlow : ICheckoutFlow
{
    public CheckoutInfo GetCheckoutInfo()
    {
        return new CheckoutInfo(
            "Express Checkout",
            ["Review Cart", "Confirm & Pay", "Complete"],
            30);
    }
}

/// <summary>
/// One-click checkout for returning customers (Variant B).
/// </summary>
public class OneClickCheckoutFlow : ICheckoutFlow
{
    public CheckoutInfo GetCheckoutInfo()
    {
        return new CheckoutInfo(
            "One-Click Checkout",
            ["Confirm Purchase", "Complete"],
            10);
    }
}
