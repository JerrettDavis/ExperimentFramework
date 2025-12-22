namespace ExperimentFramework.SampleConsole.Providers;

public sealed class TxTaxProvider : IMyTaxProvider
{
    public decimal CalculateTax(string state, decimal subtotal) => subtotal * 0.0625m;
}