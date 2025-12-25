using System.Text;

namespace ExperimentFramework.Naming;

/// <summary>
/// Default naming convention for experiment selectors.
/// </summary>
/// <remarks>
/// This implementation provides sensible defaults:
/// <list type="bullet">
/// <item><description>Feature flags use the service type name directly (e.g., <c>"IMyDatabase"</c>).</description></item>
/// <item><description>Configuration keys use the pattern <c>"Experiments:{ServiceType.Name}"</c>.</description></item>
/// </list>
/// </remarks>
public sealed class DefaultExperimentNamingConvention : IExperimentNamingConvention
{
    /// <summary>
    /// Gets the singleton instance of the default naming convention.
    /// </summary>
    public static DefaultExperimentNamingConvention Instance { get; } = new();

    /// <inheritdoc/>
    public string FeatureFlagNameFor(Type serviceType)
        => serviceType.Name;

    /// <inheritdoc/>
    public string VariantFlagNameFor(Type serviceType)
        => serviceType.Name; // Same as boolean for default

    /// <inheritdoc/>
    public string ConfigurationKeyFor(Type serviceType)
        => $"Experiments:{serviceType.Name}";

    /// <inheritdoc/>
    public string OpenFeatureFlagNameFor(Type serviceType)
        => ToKebabCase(serviceType.Name);

    private static string ToKebabCase(string name)
    {
        // Remove leading 'I' if it's an interface name (IMyService -> my-service)
        if (name.Length > 1 && name[0] == 'I' && char.IsUpper(name[1]))
            name = name[1..];

        var builder = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c))
            {
                // Insert hyphen before uppercase if:
                // 1. Not at start, AND
                // 2. Either previous char is lowercase, OR next char is lowercase (end of acronym)
                if (builder.Length > 0)
                {
                    var prevIsLower = i > 0 && char.IsLower(name[i - 1]);
                    var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
                    if (prevIsLower || nextIsLower)
                        builder.Append('-');
                }
                builder.Append(char.ToLowerInvariant(c));
            }
            else
            {
                builder.Append(c);
            }
        }
        return builder.ToString();
    }
}
