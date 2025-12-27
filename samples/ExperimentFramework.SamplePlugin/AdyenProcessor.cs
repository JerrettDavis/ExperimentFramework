namespace Acme.PaymentExperiments;

/// <summary>
/// Adyen payment processor implementation.
/// Demonstrates an alternative payment provider being tested.
/// </summary>
public sealed class AdyenProcessor : IPaymentProcessor
{
    public string Name => "Adyen";
    public string Version => "1.0.0";

    public Task<PaymentResult> ProcessAsync(decimal amount, string currency)
    {
        // Simulated payment processing
        var transactionId = $"adyen_{Guid.NewGuid():N}";

        Console.WriteLine($"[Adyen] Processing {currency} {amount:F2} payment...");

        return Task.FromResult(new PaymentResult(
            Success: true,
            TransactionId: transactionId,
            Message: "Payment processed via Adyen"));
    }
}
