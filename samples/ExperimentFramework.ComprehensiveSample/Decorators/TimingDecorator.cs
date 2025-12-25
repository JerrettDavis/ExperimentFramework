using System.Diagnostics;
using ExperimentFramework.Decorators;

namespace ExperimentFramework.ComprehensiveSample.Decorators;

/// <summary>
/// Custom decorator that measures execution time of experiment trials
/// </summary>
public class TimingDecorator : IExperimentDecorator
{
    public int Order => 1; // Execute first in the pipeline

    public async ValueTask<object?> InvokeAsync(
        InvocationContext context,
        Func<ValueTask<object?>> next)
    {
        var sw = Stopwatch.StartNew();
        Console.WriteLine($"    [TimingDecorator] Starting {context.MethodName}...");

        try
        {
            var result = await next();
            sw.Stop();
            Console.WriteLine($"    [TimingDecorator] Completed in {sw.ElapsedMilliseconds}ms");
            return result;
        }
        catch
        {
            sw.Stop();
            Console.WriteLine($"    [TimingDecorator] Failed after {sw.ElapsedMilliseconds}ms");
            throw;
        }
    }
}

/// <summary>
/// Factory for creating timing decorators
/// </summary>
public class TimingDecoratorFactory : IExperimentDecoratorFactory
{
    public IExperimentDecorator Create(IServiceProvider services)
        => new TimingDecorator();
}
