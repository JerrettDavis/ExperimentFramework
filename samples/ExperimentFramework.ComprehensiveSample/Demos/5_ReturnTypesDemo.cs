using ExperimentFramework.ComprehensiveSample.Services.ReturnTypes;

namespace ExperimentFramework.ComprehensiveSample.Demos;

/// <summary>
/// Demonstrates all 5 supported return types:
/// - void (synchronous, no return value)
/// - Task (asynchronous, no return value)
/// - Task&gt;T&lt; (asynchronous with return value)
/// - ValueTask (asynchronous, no return value, allocation-optimized)
/// - ValueTask&gt;T&lt; (asynchronous with return value, allocation-optimized)
/// </summary>
public class ReturnTypesDemo(
    IVoidService voidService,
    ITaskService taskService,
    ITaskTService taskTService,
    IValueTaskService valueTaskService,
    IValueTaskTService valueTaskTService
)
{
    public async Task RunAsync()
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("DEMO 5: ALL RETURN TYPES");
        Console.WriteLine(new string('=', 80));

        Console.WriteLine("\nExperimentFramework supports all 5 return types:");

        // 1. void
        Console.WriteLine("\n5.1 void (synchronous, no return value):");
        voidService.Execute();
        Console.WriteLine("  ✅ Synchronous execution completed");

        // 2. Task
        Console.WriteLine("\n5.2 Task (asynchronous, no return value):");
        await taskService.ExecuteAsync();
        Console.WriteLine("  ✅ Asynchronous execution completed");

        // 3. Task<T>
        Console.WriteLine("\n5.3 Task<T> (asynchronous with return value):");
        var result1 = await taskTService.GetResultAsync();
        Console.WriteLine($"  ✅ Result: {result1}");

        // 4. ValueTask
        Console.WriteLine("\n5.4 ValueTask (allocation-optimized async, no return value):");
        await valueTaskService.ExecuteAsync();
        Console.WriteLine("  ✅ ValueTask execution completed");

        // 5. ValueTask<T>
        Console.WriteLine("\n5.5 ValueTask<T> (allocation-optimized async with return value):");
        var result2 = await valueTaskTService.GetResultAsync();
        Console.WriteLine($"  ✅ Result: {result2}");

        Console.WriteLine("\n  → All 5 return types work seamlessly with experiments");
        Console.WriteLine("  → Framework handles async/sync execution automatically");
        Console.WriteLine("  → ValueTask optimizes allocations for high-performance scenarios");
    }
}
