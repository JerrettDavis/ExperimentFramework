namespace Acme.PaymentExperiments;

/// <summary>
/// Experimental Stripe v2 API payment processor.
/// Demonstrates a new implementation being tested via the plugin system.
/// </summary>
public sealed class StripeV2Processor : IPaymentProcessor
{
    public string Name => "Stripe v2 API";
    public string Version => "2.0.0";

    public Task<PaymentResult> ProcessAsync(decimal amount, string currency)
    {
        // Simulated payment processing
        var transactionId = $"stripe_v2_{Guid.NewGuid():N}";

        Console.WriteLine($"[StripeV2] Processing {currency} {amount:F2} payment...");

        return Task.FromResult(new PaymentResult(
            Success: true,
            TransactionId: transactionId,
            Message: $"Payment processed via Stripe v2 API"));
    }
}
