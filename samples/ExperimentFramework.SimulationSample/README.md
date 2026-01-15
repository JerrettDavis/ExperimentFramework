# ExperimentFramework.Simulation Sample

This sample demonstrates comprehensive simulation and shadow-mode testing patterns with the ExperimentFramework.Simulation package.

## Overview

The sample shows how to safely test both **output-based** (read operations) and **action-based** (write operations) experiments using isolation and integration models.

## Key Concepts

### 1. Output-Based Simulations (Read Operations)

**Use Case**: Testing implementations that return data without side effects

- ✅ Safe to run against real implementations
- ✅ Compares returned values
- ✅ No database modifications
- ✅ Ideal for initial parity validation

```csharp
// Read operations return data for comparison
var scenarios = new[]
{
    new Scenario<IDatabase, List<Customer>>(
        "GetAllCustomers",
        async db => await db.GetAllCustomersAsync())
};
```

### 2. Action-Based Simulations - Isolation Model

**Use Case**: Testing write operations WITHOUT affecting real systems

- ✅ Uses mock/simulated implementations
- ✅ No real database side effects
- ✅ Safe to run repeatedly
- ✅ Perfect for development and CI/CD
- ✅ Prevents cascading to dependent systems

```csharp
// Register MOCK implementations
services.AddKeyedScoped<IDatabase>("control", (sp, key) => new MockDatabase());
services.AddKeyedScoped<IDatabase>("new", (sp, key) => new MockDatabase());

// Write operations execute against mocks
var scenarios = new[]
{
    new Scenario<IDatabase, int>(
        "CreateCustomer",
        async db => await db.CreateCustomerAsync(customer))
};
```

### 3. Action-Based Simulations - Integration Model

**Use Case**: Shadow writing to real systems for final validation

- ⚠️ Uses real implementations
- ⚠️ Real database side effects occur
- ⚠️ Requires cleanup strategy
- ✅ Validates real-world behavior
- ✅ Useful before production deployment

```csharp
// Register REAL implementations for shadow writes
services.AddKeyedScoped<IDatabase>("control", (sp, key) => new RealDatabase());
services.AddKeyedScoped<IDatabase>("new", (sp, key) => new NewDatabase());

// Both implementations will execute real writes
var report = await sim.RunAsync(scenarios);
```

### 4. Dependency Control and Isolation

**Use Case**: Preventing unintended cascading down dependency trees

**Problem**: When testing a service that depends on other services, you might not want to cascade writes through the entire dependency graph.

**Solution**: Use dependency injection to control which implementations are used

```csharp
// Register dependencies with controlled implementations
services.AddKeyedScoped<ICustomerDatabase>("test", (sp, key) => new MockCustomerDatabase());
services.AddKeyedScoped<IOrderDatabase>("test", (sp, key) => new MockOrderDatabase());
services.AddKeyedScoped<IPaymentGateway>("test", (sp, key) => new MockPaymentGateway());

// Now your service uses mocked dependencies, preventing cascade
```

## Running the Sample

```bash
cd samples/ExperimentFramework.SimulationSample
dotnet run
```

## Scenarios Demonstrated

### Scenario 1: Output-Based Simulation
- Tests read operations (GetAllCustomers)
- Compares results between implementations
- Safe to run with real databases

### Scenario 2: Action-Based - Isolation Model
- Tests write operations (CreateCustomer)
- Uses mock implementations
- No real side effects
- Demonstrates safe testing approach

### Scenario 3: Action-Based - Integration Model
- Tests write operations with real shadow writes
- Both implementations write to databases
- Shows real-world validation approach
- Requires cleanup consideration

### Scenario 4: Mixed Operations with Dependency Control
- Combines read and write operations
- Demonstrates dependency isolation
- Prevents cascading to other systems
- Shows phased testing approach

## Best Practices

### When to Use Isolation Model (Mocks)

1. **Development and CI/CD**: Run simulations frequently without cleanup
2. **Early testing**: Validate logic before touching real systems
3. **Testing with dependencies**: Prevent cascading writes
4. **Rapid iteration**: Make changes and test immediately

### When to Use Integration Model (Real)

1. **Pre-production validation**: Final check before deployment
2. **Performance testing**: Measure real database performance
3. **Integration testing**: Validate with actual infrastructure
4. **Limited scope**: When you have cleanup strategies in place

### Dependency Isolation Strategies

1. **Mock external services**: Replace APIs, databases, message queues
2. **Use test doubles**: Implement lightweight in-memory alternatives
3. **Scope your tests**: Test one layer at a time
4. **Control service registration**: Use keyed services or factories

## Safety Checklist

Before running shadow writes to production:

- [ ] Understand which systems will be affected
- [ ] Have a cleanup/rollback strategy
- [ ] Consider data volume impact
- [ ] Review compliance and security implications
- [ ] Test in lower environments first
- [ ] Monitor for performance impact
- [ ] Have alerting in place

## Key Takeaways

1. **Choose your model**: Isolation for safety, integration for reality
2. **Control dependencies**: Use DI to prevent unwanted cascading
3. **Start isolated**: Test with mocks first, real systems later
4. **Be explicit**: Make it clear which implementations are real vs mock
5. **Document side effects**: Make it obvious when real writes occur
6. **Have cleanup plans**: For integration model, know how to undo changes

## Additional Resources

- [Simulation & Shadow Mode Documentation](../../docs/simulation-and-shadow-mode.md)
- [ExperimentFramework Documentation](../../README.md)
