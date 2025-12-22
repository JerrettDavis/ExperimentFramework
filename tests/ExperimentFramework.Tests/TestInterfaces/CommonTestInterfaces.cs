namespace ExperimentFramework.Tests.TestInterfaces;

/// <summary>
/// Common test interfaces and implementations used across multiple test files.
/// </summary>

// Simple test service for basic scenarios (ErrorPolicyTests, ExperimentFrameworkBuilderTests)
public interface ITestService
{
    string Execute();
}

public class StableService : ITestService
{
    public string Execute() => "StableService";
}

public class FailingService : ITestService
{
    public string Execute() => throw new InvalidOperationException("FailingService always fails");
}

public class UnstableService : ITestService
{
    private int _callCount = 0;

    public string Execute()
    {
        _callCount++;
        if (_callCount <= 1)
            throw new InvalidOperationException("UnstableService fails on first call");
        return "UnstableService";
    }
}

public class AlsoFailingService : ITestService
{
    public string Execute() => throw new InvalidOperationException("AlsoFailingService always fails");
}

// Additional test service implementations
public class ServiceA : ITestService
{
    public string Execute() => "ServiceA";
}

public class ServiceB : ITestService
{
    public string Execute() => "ServiceB";
}

// Database test interfaces (SelectionModeTests, IntegrationTests)
public interface IDatabase
{
    string GetName();
}

public class LocalDatabase : IDatabase
{
    public string GetName() => "LocalDatabase";
}

public class CloudDatabase : IDatabase
{
    public string GetName() => "CloudDatabase";
}

public class ControlDatabase : IDatabase
{
    public string GetName() => "ControlDatabase";
}

public class ExperimentalDatabase : IDatabase
{
    public string GetName() => "ExperimentalDatabase";
}

// Tax provider test interfaces (SelectionModeTests, IntegrationTests)
public interface ITaxProvider
{
    decimal Calculate(decimal amount);
    decimal CalculateTax(decimal amount); // Alias for Calculate
}

public class DefaultTaxProvider : ITaxProvider
{
    public decimal Calculate(decimal amount) => 0m;
    public decimal CalculateTax(decimal amount) => Calculate(amount);
}

public class OkTaxProvider : ITaxProvider
{
    public decimal Calculate(decimal amount) => amount * 0.045m;
    public decimal CalculateTax(decimal amount) => Calculate(amount);
}

public class TxTaxProvider : ITaxProvider
{
    public decimal Calculate(decimal amount) => amount * 0.0625m;
    public decimal CalculateTax(decimal amount) => Calculate(amount);
}

// Variant test interfaces (SelectionModeTests, IntegrationTests)
public interface IVariantService
{
    string GetName();
}

public class ControlVariant : IVariantService
{
    public string GetName() => "ControlVariant";
}

public class VariantA : IVariantService
{
    public string GetName() => "VariantA";
}

public class VariantB : IVariantService
{
    public string GetName() => "VariantB";
}

// Additional variant implementations for SelectionModeTests
public class ControlImpl : IVariantService
{
    public string GetName() => "ControlImpl";
}

// Generic service for testing
public interface IGenericService<T>
{
    T GetValue();
}

public class GenericServiceV1<T> : IGenericService<T> where T : new()
{
    public T GetValue() => new T();
}

public class GenericServiceV2<T> : IGenericService<T> where T : new()
{
    public T GetValue() => new T();
}

// Additional simple interfaces
public interface IMyService
{
    string Execute();
}

public class MyServiceV1 : IMyService
{
    public string Execute() => "v1";
}

public class MyServiceV2 : IMyService
{
    public string Execute() => "v2";
}

public interface IMyTestService
{
    int Calculate();
}

public class MyTestServiceImpl : IMyTestService
{
    public int Calculate() => 42;
}

// IOtherService for ExperimentFrameworkBuilderTests
public interface IOtherService
{
    string Execute();
}

public class ServiceC : IOtherService
{
    public string Execute() => "ServiceC";
}

public class ServiceD : IOtherService
{
    public string Execute() => "ServiceD";
}

// IVariantTestService for VariantFeatureManagerTests
public interface IVariantTestService
{
    string GetName();
}

public class ControlService : IVariantTestService
{
    public string GetName() => "ControlService";
}

public class VariantAService : IVariantTestService
{
    public string GetName() => "VariantAService";
}

public class VariantBService : IVariantTestService
{
    public string GetName() => "VariantBService";
}
