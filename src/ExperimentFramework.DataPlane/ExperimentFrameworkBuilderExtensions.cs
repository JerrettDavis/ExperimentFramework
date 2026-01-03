using ExperimentFramework.DataPlane.Decorators;

namespace ExperimentFramework.DataPlane;

/// <summary>
/// Extension methods for ExperimentFrameworkBuilder to add data plane support.
/// </summary>
public static class ExperimentFrameworkBuilderExtensions
{
    /// <summary>
    /// Adds exposure logging to the data backplane.
    /// </summary>
    /// <param name="builder">The experiment framework builder.</param>
    /// <returns>The builder for chaining.</returns>
    /// <remarks>
    /// Exposure events will be emitted to the configured IDataBackplane when trials are invoked.
    /// Configure the data backplane and options via service collection before calling AddExperimentFramework.
    /// </remarks>
    public static ExperimentFrameworkBuilder WithExposureLogging(
        this ExperimentFrameworkBuilder builder)
    {
        builder.AddDecoratorFactory(new ExposureLoggingDecoratorFactory());
        return builder;
    }
}
