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
internal sealed class DefaultExperimentNamingConvention : IExperimentNamingConvention
{
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

        var builder = new System.Text.StringBuilder();
        foreach (var c in name)
        {
            if (char.IsUpper(c))
            {
                if (builder.Length > 0)
                    builder.Append('-');
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
