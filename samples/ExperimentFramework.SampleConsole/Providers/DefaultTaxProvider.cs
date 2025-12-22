namespace ExperimentFramework.SampleConsole.Providers;

public sealed class DefaultTaxProvider : IMyTaxProvider
{
    public decimal CalculateTax(string state, decimal subtotal) => subtotal * 0.05m;
}