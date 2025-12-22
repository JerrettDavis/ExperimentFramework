using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.Decorators;

internal sealed class ErrorLoggingDecoratorFactory : IExperimentDecoratorFactory
{
    /// <summary>
    /// Creates an error-logging decorator that logs exceptions thrown by experiment calls.
    /// </summary>
    /// <param name="sp">The service provider used to resolve dependencies.</param>
    /// <returns>An <see cref="IExperimentDecorator"/> instance.</returns>
    /// <remarks>
    /// This factory resolves an <see cref="ILoggerFactory"/> (if available) and constructs a decorator that logs errors
    /// with service name, method name, and trial key, then rethrows.
    /// </remarks>
    public IExperimentDecorator Create(IServiceProvider sp)
        => new ErrorLoggingDecorator(sp.GetService<ILoggerFactory>());

    /// <summary>
    /// Decorator that logs exceptions and rethrows.
    /// </summary>
    /// <param name="loggerFactory">The logger factory used to create the error logger, if available.</param>
    private sealed class ErrorLoggingDecorator(ILoggerFactory? loggerFactory) : IExperimentDecorator
    {
        private readonly ILogger? _log = loggerFactory?.CreateLogger("ExperimentFramework.Errors");

        /// <summary>
        /// Invokes the next pipeline stage and logs any exception that occurs.
        /// </summary>
        /// <param name="ctx">The invocation context for the current call.</param>
        /// <param name="next">The continuation representing the remainder of the pipeline.</param>
        /// <returns>
        /// The terminal invocation result, or <see langword="null"/> for void-like calls.
        /// </returns>
        /// <exception cref="Exception">
        /// Rethrows any exception thrown by <paramref name="next"/> after logging.
        /// </exception>
        public async ValueTask<object?> InvokeAsync(InvocationContext ctx, Func<ValueTask<object?>> next)
        {
            try
            {
                return await next().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log?.LogError(
                    ex,
                    "Experiment call failed: {Service}.{Method} trial={Trial}",
                    ctx.ServiceType.Name,
                    ctx.MethodName,
                    ctx.TrialKey);

                throw;
            }
        }
    }
}