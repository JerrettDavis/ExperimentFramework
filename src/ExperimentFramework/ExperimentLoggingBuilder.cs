using ExperimentFramework.Decorators;

namespace ExperimentFramework;

/// <summary>
/// Fluent builder for configuring built-in logging decorators.
/// </summary>
/// <remarks>
/// <para>
/// This builder provides a simple, declarative way to enable common logging-related decorators
/// without requiring users to manually register decorator factories.
/// </para>
/// <para>
/// It is intentionally limited in scope and only produces decorator factories related
/// to logging concerns.
/// </para>
/// </remarks>
public sealed class ExperimentLoggingBuilder
{
    private bool _benchmarks;
    private bool _errorLogging;

    /// <summary>
    /// Enables benchmark logging for experiment invocations.
    /// </summary>
    /// <returns>The current logging builder instance.</returns>
    /// <remarks>
    /// When enabled, each experiment invocation will emit a log entry containing
    /// execution duration and trial metadata.
    /// </remarks>
    public ExperimentLoggingBuilder AddBenchmarks()
    {
        _benchmarks = true;
        return this;
    }

    /// <summary>
    /// Enables error logging for experiment invocations.
    /// </summary>
    /// <returns>The current logging builder instance.</returns>
    /// <remarks>
    /// When enabled, exceptions thrown by trials will be logged with contextual
    /// information before being rethrown.
    /// </remarks>
    public ExperimentLoggingBuilder AddErrorLogging()
    {
        _errorLogging = true;
        return this;
    }

    /// <summary>
    /// Builds the decorator factories represented by the current configuration.
    /// </summary>
    /// <returns>
    /// A list of <see cref="IExperimentDecoratorFactory"/> instances to be added
    /// to the global decorator pipeline.
    /// </returns>
    /// <remarks>
    /// This method is internal and is intended to be invoked only by
    /// <see cref="ExperimentFrameworkBuilder"/>.
    /// </remarks>
    internal IReadOnlyList<IExperimentDecoratorFactory> Build()
    {
        var list = new List<IExperimentDecoratorFactory>();

        if (_benchmarks)
            list.Add(new BenchmarkDecoratorFactory());

        if (_errorLogging)
            list.Add(new ErrorLoggingDecoratorFactory());

        return list;
    }
}