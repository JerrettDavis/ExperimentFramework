using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Decorators;

internal sealed class BenchmarkDecoratorFactory : IExperimentDecoratorFactory
{
    /// <summary>
    /// Creates a benchmark decorator that logs elapsed time for experiment calls.
    /// </summary>
    /// <param name="sp">The service provider used to resolve dependencies.</param>
    /// <returns>An <see cref="IExperimentDecorator"/> instance.</returns>
    /// <remarks>
    /// This factory resolves an <see cref="ILoggerFactory"/> (if available) and constructs a decorator that logs a benchmark
    /// line for each invocation, including service name, method name, trial key, and elapsed milliseconds.
    /// </remarks>
    public IExperimentDecorator Create(IServiceProvider sp)
        => new BenchmarkDecorator(sp.GetService<ILoggerFactory>());

    /// <summary>
    /// Decorator that measures and logs invocation duration.
    /// </summary>
    /// <param name="loggerFactory">The logger factory used to create the benchmark logger, if available.</param>
    private sealed class BenchmarkDecorator(ILoggerFactory? loggerFactory) : IExperimentDecorator
    {
        private readonly ILogger? _log = loggerFactory?.CreateLogger("ExperimentFramework.Benchmarks");

        /// <summary>
        /// Measures and logs the duration of the invocation.
        /// </summary>
        /// <param name="ctx">The invocation context for the current call.</param>
        /// <param name="next">The continuation representing the remainder of the pipeline.</param>
        /// <returns>
        /// The terminal invocation result, or <see langword="null"/> for void-like calls.
        /// </returns>
        /// <remarks>
        /// Logging occurs in a <c>finally</c> block to ensure benchmark telemetry is emitted even when the call throws.
        /// </remarks>
        public async ValueTask<object?> InvokeAsync(InvocationContext ctx, Func<ValueTask<object?>> next)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                return await next().ConfigureAwait(false);
            }
            finally
            {
                sw.Stop();
                _log?.LogInformation(
                    "Experiment call: {Service}.{Method} trial={Trial} elapsedMs={ElapsedMs}",
                    ctx.ServiceType.Name,
                    ctx.MethodName,
                    ctx.TrialKey,
                    sw.Elapsed.TotalMilliseconds);
            }
        }
    }
}