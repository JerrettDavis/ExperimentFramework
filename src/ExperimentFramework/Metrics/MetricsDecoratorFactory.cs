using System.Diagnostics;
using ExperimentFramework.Decorators;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Metrics;

/// <summary>
/// Factory for creating metrics collection decorators.
/// </summary>
public sealed class MetricsDecoratorFactory : IExperimentDecoratorFactory
{
    private readonly IExperimentMetrics _metrics;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsDecoratorFactory"/> class.
    /// </summary>
    /// <param name="metrics">The metrics implementation to use for recording experiment data.</param>
    public MetricsDecoratorFactory(IExperimentMetrics metrics)
    {
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <inheritdoc />
    public IExperimentDecorator Create(IServiceProvider serviceProvider)
    {
        return new MetricsDecorator(_metrics);
    }

    private sealed class MetricsDecorator : IExperimentDecorator
    {
        private readonly IExperimentMetrics _metrics;

        public MetricsDecorator(IExperimentMetrics metrics)
        {
            _metrics = metrics;
        }

        public async ValueTask<object?> InvokeAsync(
            InvocationContext context,
            Func<ValueTask<object?>> next)
        {
            var tags = new[]
            {
                new KeyValuePair<string, object>("service", context.ServiceType.Name),
                new KeyValuePair<string, object>("method", context.MethodName),
                new KeyValuePair<string, object>("trial_key", context.TrialKey)
            };

            // Increment invocation counter
            _metrics.IncrementCounter("experiment_invocations_total", 1, tags);

            var sw = Stopwatch.StartNew();
            try
            {
                var result = await next();

                sw.Stop();

                // Record success metrics
                _metrics.RecordHistogram("experiment_duration_seconds", sw.Elapsed.TotalSeconds, tags);
                _metrics.IncrementCounter("experiment_success_total", 1, tags);

                return result;
            }
            catch (Exception)
            {
                sw.Stop();

                // Record failure metrics
                _metrics.RecordHistogram("experiment_duration_seconds", sw.Elapsed.TotalSeconds, tags);
                _metrics.IncrementCounter("experiment_errors_total", 1, tags);

                throw;
            }
        }
    }
}
