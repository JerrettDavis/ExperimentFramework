namespace ExperimentFramework.SimulationSample;

/// <summary>
/// Represents a customer entity
/// </summary>
public record Customer(int Id, string Name, string Email, decimal Balance);

/// <summary>
/// Interface for customer database operations
/// </summary>
public interface ICustomerDatabase
{
    // Read operations (output-based)
    ValueTask<Customer?> GetCustomerAsync(int customerId);
    ValueTask<List<Customer>> GetAllCustomersAsync();
    
    // Write operations (action-based)
    ValueTask<int> CreateCustomerAsync(Customer customer);
    ValueTask UpdateCustomerAsync(Customer customer);
    ValueTask DeleteCustomerAsync(int customerId);
}

/// <summary>
/// Real database implementation - makes actual database calls
/// </summary>
public class RealCustomerDatabase : ICustomerDatabase
{
    private readonly Dictionary<int, Customer> _store = new();
    private int _nextId = 1;

    public ValueTask<Customer?> GetCustomerAsync(int customerId)
    {
        Console.WriteLine($"[REAL DB] Getting customer {customerId}");
        _store.TryGetValue(customerId, out var customer);
        return new ValueTask<Customer?>(customer);
    }

    public ValueTask<List<Customer>> GetAllCustomersAsync()
    {
        Console.WriteLine($"[REAL DB] Getting all customers");
        return new ValueTask<List<Customer>>(_store.Values.ToList());
    }

    public ValueTask<int> CreateCustomerAsync(Customer customer)
    {
        var id = _nextId++;
        var customerWithId = customer with { Id = id };
        _store[id] = customerWithId;
        Console.WriteLine($"[REAL DB] Created customer {id}: {customer.Name}");
        return new ValueTask<int>(id);
    }

    public ValueTask UpdateCustomerAsync(Customer customer)
    {
        _store[customer.Id] = customer;
        Console.WriteLine($"[REAL DB] Updated customer {customer.Id}");
        return ValueTask.CompletedTask;
    }

    public ValueTask DeleteCustomerAsync(int customerId)
    {
        _store.Remove(customerId);
        Console.WriteLine($"[REAL DB] Deleted customer {customerId}");
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Mock/Simulated database implementation - does NOT make real database calls
/// Safe for testing write operations without side effects
/// </summary>
public class MockCustomerDatabase : ICustomerDatabase
{
    private readonly Dictionary<int, Customer> _inMemoryStore = new();
    private int _nextId = 1;

    public ValueTask<Customer?> GetCustomerAsync(int customerId)
    {
        Console.WriteLine($"[MOCK DB] Getting customer {customerId}");
        _inMemoryStore.TryGetValue(customerId, out var customer);
        return new ValueTask<Customer?>(customer);
    }

    public ValueTask<List<Customer>> GetAllCustomersAsync()
    {
        Console.WriteLine($"[MOCK DB] Getting all customers");
        return new ValueTask<List<Customer>>(_inMemoryStore.Values.ToList());
    }

    public ValueTask<int> CreateCustomerAsync(Customer customer)
    {
        var id = _nextId++;
        var customerWithId = customer with { Id = id };
        _inMemoryStore[id] = customerWithId;
        Console.WriteLine($"[MOCK DB] Simulated create customer {id}: {customer.Name}");
        return new ValueTask<int>(id);
    }

    public ValueTask UpdateCustomerAsync(Customer customer)
    {
        _inMemoryStore[customer.Id] = customer;
        Console.WriteLine($"[MOCK DB] Simulated update customer {customer.Id}");
        return ValueTask.CompletedTask;
    }

    public ValueTask DeleteCustomerAsync(int customerId)
    {
        _inMemoryStore.Remove(customerId);
        Console.WriteLine($"[MOCK DB] Simulated delete customer {customerId}");
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// New database implementation being tested
/// </summary>
public class NewCustomerDatabase : ICustomerDatabase
{
    private readonly Dictionary<int, Customer> _store = new();
    private int _nextId = 1;

    public ValueTask<Customer?> GetCustomerAsync(int customerId)
    {
        Console.WriteLine($"[NEW DB] Getting customer {customerId}");
        _store.TryGetValue(customerId, out var customer);
        return new ValueTask<Customer?>(customer);
    }

    public ValueTask<List<Customer>> GetAllCustomersAsync()
    {
        Console.WriteLine($"[NEW DB] Getting all customers");
        return new ValueTask<List<Customer>>(_store.Values.ToList());
    }

    public ValueTask<int> CreateCustomerAsync(Customer customer)
    {
        var id = _nextId++;
        var customerWithId = customer with { Id = id };
        _store[id] = customerWithId;
        Console.WriteLine($"[NEW DB] Created customer {id}: {customer.Name}");
        return new ValueTask<int>(id);
    }

    public ValueTask UpdateCustomerAsync(Customer customer)
    {
        _store[customer.Id] = customer;
        Console.WriteLine($"[NEW DB] Updated customer {customer.Id}");
        return ValueTask.CompletedTask;
    }

    public ValueTask DeleteCustomerAsync(int customerId)
    {
        _store.Remove(customerId);
        Console.WriteLine($"[NEW DB] Deleted customer {customerId}");
        return ValueTask.CompletedTask;
    }
}
