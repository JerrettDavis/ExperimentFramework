using Microsoft.Extensions.Options;

namespace ExperimentFramework.Plugins.Configuration;

/// <summary>
/// Validates plugin configuration at startup.
/// </summary>
public sealed class PluginConfigurationValidator : IValidateOptions<PluginConfigurationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PluginConfigurationOptions options)
    {
        var errors = new List<string>();

        // Validate discovery paths - only check for empty values, not existence
        // (paths may be created at runtime or might be glob patterns)
        foreach (var path in options.DiscoveryPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                errors.Add("Discovery paths cannot contain empty or whitespace values.");
            }
        }

        // Validate allowed plugin directories - only check for empty values
        // (directories may be created at runtime during deployment)
        foreach (var path in options.AllowedPluginDirectories)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                errors.Add("Allowed plugin directories cannot contain empty or whitespace values.");
            }
        }

        // Validate security configuration consistency
        if (options.RequireSignedAssemblies && options.TrustedPublisherThumbprints.Count == 0)
        {
            // This is a warning scenario - signed assemblies are required but no thumbprints specified
            // All signed assemblies will be trusted. This might be intentional.
        }

        // Validate thumbprint format (SHA1 thumbprints are 40 hex chars)
        foreach (var thumbprint in options.TrustedPublisherThumbprints)
        {
            if (string.IsNullOrWhiteSpace(thumbprint))
            {
                errors.Add("Trusted publisher thumbprints cannot contain empty or whitespace values.");
                continue;
            }

            if (thumbprint.Length != 40 || !thumbprint.All(c => char.IsAsciiHexDigit(c)))
            {
                errors.Add($"Invalid certificate thumbprint format: {thumbprint}. Expected 40 hexadecimal characters.");
            }
        }

        // Validate size limits
        if (options.MaxManifestSizeBytes <= 0)
        {
            errors.Add($"MaxManifestSizeBytes must be positive. Got: {options.MaxManifestSizeBytes}");
        }

        if (options.MaxManifestJsonDepth <= 0)
        {
            errors.Add($"MaxManifestJsonDepth must be positive. Got: {options.MaxManifestJsonDepth}");
        }

        // Validate hot reload settings
        if (options.EnableHotReload && options.HotReloadDebounceMs < 0)
        {
            errors.Add($"HotReloadDebounceMs cannot be negative. Got: {options.HotReloadDebounceMs}");
        }

        // Validate shared assemblies
        foreach (var assembly in options.DefaultSharedAssemblies)
        {
            if (string.IsNullOrWhiteSpace(assembly))
            {
                errors.Add("Default shared assemblies cannot contain empty or whitespace values.");
            }
        }

        return errors.Count > 0
            ? ValidateOptionsResult.Fail(errors)
            : ValidateOptionsResult.Success;
    }
}
