namespace Acme.PaymentExperiments;

/// <summary>
/// Sample interface for payment processing experiments.
/// This interface would normally be defined in a shared contracts assembly.
/// </summary>
public interface IPaymentProcessor
{
    /// <summary>
    /// Gets the name of the payment processor.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the version of the processor implementation.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Processes a payment.
    /// </summary>
    /// <param name="amount">The payment amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <returns>The payment result.</returns>
    Task<PaymentResult> ProcessAsync(decimal amount, string currency);
}

/// <summary>
/// Result of a payment processing operation.
/// </summary>
public record PaymentResult(bool Success, string TransactionId, string Message);
