using ExperimentFramework.Decorators;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.DataPlane.Decorators;

/// <summary>
/// Factory for creating exposure logging decorators.
/// </summary>
public sealed class ExposureLoggingDecoratorFactory : IExperimentDecoratorFactory
{
    /// <inheritdoc />
    public IExperimentDecorator Create(IServiceProvider sp)
    {
        return ActivatorUtilities.CreateInstance<ExposureLoggingDecorator>(sp);
    }
}
