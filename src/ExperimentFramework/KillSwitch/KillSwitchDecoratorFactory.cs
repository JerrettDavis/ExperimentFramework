using ExperimentFramework.Decorators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ExperimentFramework.KillSwitch;

/// <summary>
/// Factory for creating kill switch decorators.
/// </summary>
public sealed class KillSwitchDecoratorFactory : IExperimentDecoratorFactory
{
    private readonly IKillSwitchProvider _killSwitch;
    private readonly ILoggerFactory? _loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="KillSwitchDecoratorFactory"/> class.
    /// </summary>
    /// <param name="killSwitch">The kill switch provider to use.</param>
    /// <param name="loggerFactory">Optional logger factory for logging kill switch events.</param>
    public KillSwitchDecoratorFactory(IKillSwitchProvider killSwitch, ILoggerFactory? loggerFactory = null)
    {
        _killSwitch = killSwitch ?? throw new ArgumentNullException(nameof(killSwitch));
        _loggerFactory = loggerFactory;
    }

    /// <inheritdoc />
    public IExperimentDecorator Create(IServiceProvider serviceProvider)
    {
        var logger = _loggerFactory?.CreateLogger("ExperimentFramework.KillSwitch");
        return new KillSwitchDecorator(_killSwitch, logger);
    }

    private sealed class KillSwitchDecorator : IExperimentDecorator
    {
        private readonly IKillSwitchProvider _killSwitch;
        private readonly ILogger? _logger;

        public KillSwitchDecorator(IKillSwitchProvider killSwitch, ILogger? logger)
        {
            _killSwitch = killSwitch;
            _logger = logger;
        }

        public async ValueTask<object?> InvokeAsync(
            InvocationContext context,
            Func<ValueTask<object?>> next)
        {
            // Check if entire experiment is disabled
            if (_killSwitch.IsExperimentDisabled(context.ServiceType))
            {
                _logger?.LogWarning(
                    "Experiment disabled by kill switch: {ServiceType}.{MethodName}",
                    context.ServiceType.Name,
                    context.MethodName);

                throw new ExperimentDisabledException(
                    $"Experiment for {context.ServiceType.Name} is disabled by kill switch");
            }

            // Check if specific trial is disabled
            if (_killSwitch.IsTrialDisabled(context.ServiceType, context.TrialKey))
            {
                _logger?.LogWarning(
                    "Trial disabled by kill switch: {ServiceType}.{MethodName} trial={TrialKey}",
                    context.ServiceType.Name,
                    context.MethodName,
                    context.TrialKey);

                throw new TrialDisabledException(
                    $"Trial '{context.TrialKey}' for {context.ServiceType.Name} is disabled by kill switch");
            }

            return await next();
        }
    }
}

/// <summary>
/// Exception thrown when an experiment is disabled by the kill switch.
/// </summary>
public sealed class ExperimentDisabledException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExperimentDisabledException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public ExperimentDisabledException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when a trial is disabled by the kill switch.
/// </summary>
public sealed class TrialDisabledException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TrialDisabledException"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    public TrialDisabledException(string message) : base(message) { }
}
