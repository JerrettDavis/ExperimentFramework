namespace ExperimentFramework.SampleConsole.Providers;

public sealed class OkTaxProvider : IMyTaxProvider
{
    public decimal CalculateTax(string state, decimal subtotal) => subtotal * 0.045m;
}