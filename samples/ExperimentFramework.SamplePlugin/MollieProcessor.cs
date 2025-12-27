namespace Acme.PaymentExperiments;

/// <summary>
/// Mollie payment processor implementation.
/// Demonstrates another payment provider variant.
/// </summary>
public sealed class MollieProcessor : IPaymentProcessor
{
    public string Name => "Mollie";
    public string Version => "1.0.0";

    public Task<PaymentResult> ProcessAsync(decimal amount, string currency)
    {
        // Simulated payment processing
        var transactionId = $"mollie_{Guid.NewGuid():N}";

        Console.WriteLine($"[Mollie] Processing {currency} {amount:F2} payment...");

        return Task.FromResult(new PaymentResult(
            Success: true,
            TransactionId: transactionId,
            Message: "Payment processed via Mollie"));
    }
}
