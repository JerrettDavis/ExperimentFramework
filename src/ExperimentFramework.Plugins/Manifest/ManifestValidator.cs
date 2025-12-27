using ExperimentFramework.Plugins.Abstractions;

namespace ExperimentFramework.Plugins.Manifest;

/// <summary>
/// Validates plugin manifests.
/// </summary>
public sealed class ManifestValidator
{
    /// <summary>
    /// Validates a plugin manifest.
    /// </summary>
    /// <param name="manifest">The manifest to validate.</param>
    /// <returns>The validation result.</returns>
    public ManifestValidationResult Validate(IPluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var errors = new List<string>();
        var warnings = new List<string>();

        // Required fields
        if (string.IsNullOrWhiteSpace(manifest.Id))
        {
            errors.Add("Plugin ID is required.");
        }
        else if (!IsValidPluginId(manifest.Id))
        {
            errors.Add($"Plugin ID '{manifest.Id}' is invalid. Must contain only alphanumeric characters, dots, hyphens, and underscores.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            errors.Add("Plugin name is required.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            errors.Add("Plugin version is required.");
        }
        else if (!IsValidSemVer(manifest.Version))
        {
            warnings.Add($"Plugin version '{manifest.Version}' does not follow semantic versioning.");
        }

        // Manifest version
        if (string.IsNullOrWhiteSpace(manifest.ManifestVersion))
        {
            warnings.Add("Manifest version not specified, assuming 1.0.");
        }
        else if (manifest.ManifestVersion != "1.0")
        {
            warnings.Add($"Unknown manifest version '{manifest.ManifestVersion}'. Some features may not be supported.");
        }

        // Service registrations
        foreach (var service in manifest.Services)
        {
            if (string.IsNullOrWhiteSpace(service.Interface))
            {
                errors.Add("Service registration missing interface type.");
            }

            foreach (var impl in service.Implementations)
            {
                if (string.IsNullOrWhiteSpace(impl.Type))
                {
                    errors.Add($"Implementation for {service.Interface} missing type.");
                }

                if (!string.IsNullOrWhiteSpace(impl.Alias) && !IsValidAlias(impl.Alias))
                {
                    errors.Add($"Invalid alias '{impl.Alias}'. Must contain only alphanumeric characters, hyphens, and underscores.");
                }
            }

            // Check for duplicate aliases
            var aliases = service.Implementations
                .Where(i => !string.IsNullOrWhiteSpace(i.Alias))
                .Select(i => i.Alias!)
                .ToList();

            var duplicates = aliases
                .GroupBy(a => a, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key);

            foreach (var dup in duplicates)
            {
                errors.Add($"Duplicate alias '{dup}' in service {service.Interface}.");
            }
        }

        // Isolation configuration
        if (manifest.Isolation.Mode == PluginIsolationMode.Full &&
            manifest.Isolation.SharedAssemblies.Count > 0)
        {
            warnings.Add("SharedAssemblies specified with Full isolation mode will be ignored.");
        }

        return new ManifestValidationResult(errors, warnings);
    }

    private static bool IsValidPluginId(string id)
    {
        return id.All(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_');
    }

    private static bool IsValidAlias(string alias)
    {
        return alias.All(c => char.IsLetterOrDigit(c) || c is '-' or '_');
    }

    private static bool IsValidSemVer(string version)
    {
        var parts = version.Split('.');
        if (parts.Length is < 2 or > 4)
        {
            return false;
        }

        return parts.Take(3).All(p => int.TryParse(p.Split('-')[0], out _));
    }
}

/// <summary>
/// Result of manifest validation.
/// </summary>
public sealed record ManifestValidationResult(
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    /// <summary>
    /// Gets whether the manifest is valid (no errors).
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// A successful validation with no errors or warnings.
    /// </summary>
    public static ManifestValidationResult Success { get; } = new([], []);
}
