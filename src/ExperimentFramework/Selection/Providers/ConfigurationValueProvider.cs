using ExperimentFramework.Naming;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Selection.Providers;

/// <summary>
/// Selection mode provider that uses IConfiguration for string-based selection.
/// </summary>
/// <remarks>
/// Returns the configuration value directly as the trial key.
/// This is useful for selecting between multiple named implementations.
/// </remarks>
internal sealed class ConfigurationValueProvider : ISelectionModeProvider
{
    /// <inheritdoc />
    public string ModeIdentifier => SelectionModes.ConfigurationValue;

    /// <inheritdoc />
    public ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        var configuration = context.ServiceProvider.GetService<IConfiguration>();
        if (configuration == null)
        {
            return ValueTask.FromResult<string?>(null);
        }

        try
        {
            var value = configuration[context.SelectorName];
            return ValueTask.FromResult(string.IsNullOrEmpty(value) ? null : value);
        }
        catch
        {
            return ValueTask.FromResult<string?>(null);
        }
    }

    /// <inheritdoc />
    public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
        => convention.ConfigurationKeyFor(serviceType);
}

/// <summary>
/// Factory for creating ConfigurationValueProvider instances.
/// </summary>
internal sealed class ConfigurationValueProviderFactory : ISelectionModeProviderFactory
{
    /// <inheritdoc />
    public string ModeIdentifier => SelectionModes.ConfigurationValue;

    /// <inheritdoc />
    public ISelectionModeProvider Create(IServiceProvider scopedProvider)
        => new ConfigurationValueProvider();
}
