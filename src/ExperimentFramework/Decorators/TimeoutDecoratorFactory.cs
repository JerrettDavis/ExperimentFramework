using ExperimentFramework.Models;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Decorators;

/// <summary>
/// Factory for creating timeout enforcement decorators.
/// </summary>
public sealed class TimeoutDecoratorFactory : IExperimentDecoratorFactory
{
    private readonly TimeoutPolicy _policy;
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeoutDecoratorFactory"/> class.
    /// </summary>
    /// <param name="policy">The timeout policy to enforce.</param>
    /// <param name="loggerFactory">Optional logger factory for logging timeout events.</param>
    public TimeoutDecoratorFactory(TimeoutPolicy policy, ILoggerFactory? loggerFactory = null)
    {
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IExperimentDecorator Create(IServiceProvider serviceProvider)
    {
        var logger = _loggerFactory?.CreateLogger("ExperimentFramework.Timeout");
        return new TimeoutDecorator(_policy, logger);
    }

    private sealed class TimeoutDecorator(TimeoutPolicy policy, ILogger? logger) : IExperimentDecorator
    {
        public async ValueTask<object?> InvokeAsync(
            InvocationContext context,
            Func<ValueTask<object?>> next)
        {
            try
            {
                // Use Task.WaitAsync to enforce timeout without race conditions
                var task = next().AsTask();
                return await task.WaitAsync(policy.Timeout);
            }
            catch (TimeoutException ex)
            {
                // Wrap with detailed context information
                var timeoutEx = new TimeoutException(
                    $"Trial '{context.TrialKey}' for {context.ServiceType.Name}.{context.MethodName} " +
                    $"exceeded timeout of {policy.Timeout.TotalMilliseconds}ms",
                    ex);

                logger?.LogWarning(timeoutEx,
                    "Trial timeout: {ServiceType}.{MethodName} trial={TrialKey} timeout={TimeoutMs}ms",
                    context.ServiceType.Name,
                    context.MethodName,
                    context.TrialKey,
                    policy.Timeout.TotalMilliseconds);

                throw timeoutEx;
            }
        }
    }
}
