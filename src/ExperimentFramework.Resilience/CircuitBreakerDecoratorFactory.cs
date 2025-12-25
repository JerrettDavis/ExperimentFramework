using ExperimentFramework.Decorators;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;

namespace ExperimentFramework.Resilience;

/// <summary>
/// Factory for creating circuit breaker decorators using Polly.
/// </summary>
public sealed class CircuitBreakerDecoratorFactory : IExperimentDecoratorFactory
{
    private readonly CircuitBreakerDecorator _decorator;

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerDecoratorFactory"/> class.
    /// </summary>
    /// <param name="options">The circuit breaker configuration options.</param>
    /// <param name="loggerFactory">Optional logger factory for logging circuit state changes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public CircuitBreakerDecoratorFactory(CircuitBreakerOptions options, ILoggerFactory? loggerFactory = null)
    {
        if (options is null) throw new ArgumentNullException(nameof(options));
        var logger = loggerFactory?.CreateLogger("ExperimentFramework.CircuitBreaker");
        _decorator = new CircuitBreakerDecorator(options, logger);
    }

    /// <inheritdoc/>
    public IExperimentDecorator Create(IServiceProvider serviceProvider)
    {
        // Return singleton instance to share circuit breaker state across all invocations
        return _decorator;
    }

    private sealed class CircuitBreakerDecorator : IExperimentDecorator
    {
        private readonly ResiliencePipeline _pipeline;
        private readonly CircuitBreakerOptions _options;
        private readonly ILogger? _logger;

        public CircuitBreakerDecorator(CircuitBreakerOptions options, ILogger? logger)
        {
            _options = options;
            _logger = logger;

            // Build Polly resilience pipeline with circuit breaker
            var pipelineBuilder = new ResiliencePipelineBuilder();

            // Use configured failure ratio, defaulting to 50% if not specified
            var failureRatio = options.FailureRatioThreshold ?? 0.5;

            pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = failureRatio,
                MinimumThroughput = options.MinimumThroughput,
                SamplingDuration = options.SamplingDuration,
                BreakDuration = options.BreakDuration,
                ShouldHandle = new PredicateBuilder().Handle<Exception>(),
                OnOpened = _ =>
                {
                    logger?.LogWarning(
                        "Circuit breaker opened after failure ratio {FailureRatio:P} exceeded threshold",
                        failureRatio);
                    return default;
                },
                OnClosed = _ =>
                {
                    logger?.LogInformation("Circuit breaker closed - normal operation resumed");
                    return default;
                },
                OnHalfOpened = _ =>
                {
                    logger?.LogInformation("Circuit breaker half-open - testing recovery");
                    return default;
                }
            });

            _pipeline = pipelineBuilder.Build();
        }

        public async ValueTask<object?> InvokeAsync(
            InvocationContext context,
            Func<ValueTask<object?>> next)
        {
            try
            {
                // Execute through Polly circuit breaker
                return await _pipeline.ExecuteAsync(async _ =>
                {
                    return await next();
                }, CancellationToken.None);
            }
            catch (BrokenCircuitException ex)
            {
                _logger?.LogWarning(ex,
                    "Circuit breaker is open for {ServiceType}.{MethodName} trial={TrialKey}",
                    context.ServiceType.Name,
                    context.MethodName,
                    context.TrialKey);

                // Wrap in domain exception for better error messages
                // The error policy in RuntimeExperimentProxy handles retry based on OnErrorPolicy
                throw new CircuitBreakerOpenException(
                    $"Circuit breaker is open for trial '{context.TrialKey}' of {context.ServiceType.Name}.{context.MethodName}",
                    ex);
            }
        }
    }
}

/// <summary>
/// Exception thrown when a circuit breaker is open.
/// </summary>
public sealed class CircuitBreakerOpenException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    /// <param name="message">The error message describing the circuit breaker state.</param>
    /// <param name="innerException">The exception that caused the circuit to open, if any.</param>
    public CircuitBreakerOpenException(string message, Exception? innerException = null)
        : base(message, innerException) { }
}
