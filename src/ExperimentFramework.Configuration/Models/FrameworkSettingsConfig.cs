namespace ExperimentFramework.Configuration.Models;

/// <summary>
/// Global framework settings.
/// </summary>
public sealed class FrameworkSettingsConfig
{
    /// <summary>
    /// Proxy generation strategy: "sourceGenerators" or "dispatchProxy".
    /// Default is "sourceGenerators".
    /// </summary>
    public string ProxyStrategy { get; set; } = "sourceGenerators";

    /// <summary>
    /// Naming convention: "default" or a fully qualified type name
    /// implementing <see cref="ExperimentFramework.Naming.IExperimentNamingConvention"/>.
    /// </summary>
    public string NamingConvention { get; set; } = "default";
}
