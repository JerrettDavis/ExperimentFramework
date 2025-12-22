namespace ExperimentFramework.SampleConsole.Providers;

public interface IMyTaxProvider
{
    decimal CalculateTax(string state, decimal subtotal);
}