# ExperimentFramework.Analyzers

This analyzer package provides compile-time diagnostics for common experiment configuration mistakes in ExperimentFramework.

## Diagnostics

### EF0001: Control type does not implement service type
**Severity**: Error

The type specified in `AddControl<TImpl>()` must implement the service interface being experimented on.

**Example of violation**:
```csharp
.Trial<IPaymentService>(t => t
    .AddControl<NotAPaymentService>()) // Error: NotAPaymentService doesn't implement IPaymentService
```

**Fix**: Change the type argument to a class that implements `IPaymentService`.

**Known Limitations**: The analyzer currently works best when analyzing direct method calls. Detection within complex lambda expressions may have limitations.

---

### EF0002: Condition type does not implement service type
**Severity**: Error

The type specified in `AddCondition<TImpl>()` or `AddVariant<TImpl>()` must implement the service interface being experimented on.

**Example of violation**:
```csharp
.Trial<IPaymentService>(t => t
    .AddControl<StripePayment>()
    .AddCondition<NotAPaymentService>("paypal")) // Error: NotAPaymentService doesn't implement IPaymentService
```

**Fix**: Change the type argument to a class that implements `IPaymentService`.

**Known Limitations**: The analyzer currently works best when analyzing direct method calls. Detection within complex lambda expressions may have limitations.

---

### EF0003: Duplicate condition key in trial
**Severity**: Warning

Each condition key must be unique within a trial. Duplicate keys will result in the last registration overwriting earlier ones.

**Example of violation**:
```csharp
.Trial<IPaymentService>(t => t
    .AddControl<StripePayment>()
    .AddCondition<PayPalPayment>("paypal")
    .AddCondition<BraintreePayment>("paypal")) // Warning: Key "paypal" is duplicated
```

**Fix**: Rename one of the duplicate keys to make them unique.

**Code Fix Available**: The analyzer provides a code fix to automatically rename the duplicate key.

**Known Limitations**: Duplicate key detection works within the same statement but may not detect duplicates across different code paths.

---

### EF0004: Trial declared but not registered
**Severity**: Warning

A trial is configured but the ExperimentFrameworkBuilder is never added to the service collection.

**Example of violation**:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    var builder = ExperimentFrameworkBuilder.Create()
        .Trial<IPaymentService>(t => t.AddControl<StripePayment>());
    
    // Missing: builder.AddTo(services) or services.AddExperimentFramework(builder)
}
```

**Fix**: Call `.AddTo(services)` on the builder or use `services.AddExperimentFramework(builder)`.

**Status**: Diagnostic defined but not yet implemented in this release.

---

### EF0005: Potential lifetime capture mismatch
**Severity**: Warning

When a singleton service depends on a scoped service through an experiment proxy, it can lead to captive dependencies where scoped services are effectively promoted to singleton lifetime.

**Example of violation**:
```csharp
services.AddSingleton<IPaymentService>(/* experiment proxy */);
services.AddScoped<StripePayment>(); // Warning: Singleton may capture scoped
```

**Fix**: Ensure all trial implementations have lifetimes compatible with the service registration. Either:
- Register the service as Scoped if any implementations are Scoped
- Register all implementations as Singleton if the service is Singleton

**Status**: Diagnostic defined but not yet implemented in this release.

---

## Code Fixes

The analyzer package includes code fix providers for the following diagnostics:

1. **EF0003 - Duplicate Key**: Automatically renames duplicate condition keys to make them unique (experimental)
2. **EF0001/EF0002 - Type Mismatch**: Suggests types in the current project that implement the service interface (experimental)

---

## Usage

The analyzer is automatically included when you reference the `ExperimentFramework` or `ExperimentFramework.Generators` package. No additional configuration is required.

To see diagnostics in Visual Studio or Visual Studio Code, simply build your project or save a file containing experiment configuration code.

---

## Current Limitations

1. **Lambda Expression Analysis**: The EF0001 and EF0002 analyzers work best with direct method calls. Analysis of method calls within lambda expressions (e.g., `.Trial<T>(t => t.AddControl<...>())`) is limited in this initial release.

2. **Cross-File Analysis**: The EF0003 analyzer detects duplicates within a single statement but may not detect duplicates across different files or code paths.

3. **EF0004 and EF0005**: These diagnostics are defined but not fully implemented in this release. They will be completed in a future update.

## Future Enhancements

- Improved lambda expression analysis for EF0001 and EF0002
- Full implementation of EF0004 (trial registration check)
- Full implementation of EF0005 (lifetime mismatch detection)
- Additional diagnostics for common configuration errors
- More sophisticated code fix providers
