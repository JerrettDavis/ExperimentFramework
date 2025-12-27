using System.Security;
using System.Security.Cryptography.X509Certificates;
using ExperimentFramework.Plugins.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ExperimentFramework.Plugins.Security;

/// <summary>
/// Validates plugin security requirements including path restrictions and assembly signatures.
/// </summary>
public sealed class PluginSecurityValidator
{
    private readonly PluginConfigurationOptions _options;
    private readonly ILogger<PluginSecurityValidator> _logger;
    private readonly List<string> _normalizedAllowedPaths;

    /// <summary>
    /// Creates a new plugin security validator.
    /// </summary>
    /// <param name="options">The plugin configuration options.</param>
    /// <param name="logger">Optional logger.</param>
    public PluginSecurityValidator(
        PluginConfigurationOptions options,
        ILogger<PluginSecurityValidator>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options;
        _logger = logger ?? NullLogger<PluginSecurityValidator>.Instance;

        // Pre-normalize allowed paths for faster comparison
        _normalizedAllowedPaths = options.AllowedPluginDirectories
            .Select(NormalizePath)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
    }

    /// <summary>
    /// Validates that a plugin path meets all security requirements.
    /// </summary>
    /// <param name="pluginPath">The path to the plugin assembly.</param>
    /// <exception cref="SecurityException">Thrown if the path fails validation.</exception>
    public void ValidatePath(string pluginPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginPath);

        // Check for UNC paths on the original input (before normalization)
        // This ensures cross-platform detection since Path.GetFullPath may normalize away UNC patterns on non-Windows
        if (IsUncPath(pluginPath) && !_options.AllowUncPaths)
        {
            _logger.LogWarning("Blocked UNC path for plugin: {Path}", pluginPath);
            throw new SecurityException(
                $"UNC paths are not allowed for plugin loading. Path: {pluginPath}");
        }

        var fullPath = Path.GetFullPath(pluginPath);

        // Check for path traversal attempts
        if (ContainsPathTraversal(pluginPath))
        {
            _logger.LogWarning("Blocked path traversal attempt: {Path}", pluginPath);
            throw new SecurityException(
                $"Path traversal sequences are not allowed. Path: {pluginPath}");
        }

        // Check against allowed directories whitelist
        if (_normalizedAllowedPaths.Count > 0)
        {
            var normalizedPath = NormalizePath(fullPath);
            var isAllowed = _normalizedAllowedPaths.Any(allowed =>
                normalizedPath.StartsWith(allowed, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                _logger.LogWarning(
                    "Plugin path not in allowed directories: {Path}. Allowed: {Allowed}",
                    fullPath,
                    string.Join(", ", _options.AllowedPluginDirectories));
                throw new SecurityException(
                    $"Plugin path is not in an allowed directory. Path: {pluginPath}");
            }
        }

        _logger.LogDebug("Plugin path validated: {Path}", fullPath);
    }

    /// <summary>
    /// Validates that an assembly meets signature requirements.
    /// </summary>
    /// <param name="assemblyPath">The path to the assembly.</param>
    /// <exception cref="SecurityException">Thrown if the assembly fails signature validation.</exception>
    public void ValidateAssemblySignature(string assemblyPath)
    {
        if (!_options.RequireSignedAssemblies && _options.TrustedPublisherThumbprints.Count == 0)
        {
            return; // No signature validation required
        }

        X509Certificate2? cert = null;

        try
        {
            // Extract certificate from signed file
            // Note: Using the older API as LoadSignerCertificate is not available
#pragma warning disable SYSLIB0057 // X509Certificate.CreateFromSignedFile is obsolete
            var baseCert = X509Certificate.CreateFromSignedFile(assemblyPath);
#pragma warning restore SYSLIB0057

            if (baseCert is not null)
            {
                cert = new X509Certificate2(baseCert);
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not extract certificate from {Path}", assemblyPath);
        }

        if (cert is null)
        {
            if (_options.RequireSignedAssemblies)
            {
                _logger.LogWarning("Unsigned assembly blocked: {Path}", assemblyPath);
                throw new SecurityException(
                    $"Assembly is not signed with Authenticode. Path: {assemblyPath}");
            }
            return;
        }

        using (cert)
        {
            // Check against trusted thumbprints if configured
            if (_options.TrustedPublisherThumbprints.Count > 0)
            {
                var thumbprint = cert.Thumbprint;

                var isTrusted = _options.TrustedPublisherThumbprints
                    .Any(t => t.Equals(thumbprint, StringComparison.OrdinalIgnoreCase));

                if (!isTrusted)
                {
                    _logger.LogWarning(
                        "Assembly signed by untrusted publisher. Path: {Path}, Thumbprint: {Thumbprint}",
                        assemblyPath,
                        thumbprint);
                    throw new SecurityException(
                        $"Assembly is not signed by a trusted publisher. Thumbprint: {thumbprint}");
                }

                _logger.LogDebug(
                    "Assembly signature verified. Path: {Path}, Thumbprint: {Thumbprint}",
                    assemblyPath,
                    thumbprint);
            }
        }
    }

    /// <summary>
    /// Performs all security validations on a plugin path.
    /// </summary>
    /// <param name="pluginPath">The path to the plugin assembly.</param>
    /// <exception cref="SecurityException">Thrown if any validation fails.</exception>
    public void ValidatePlugin(string pluginPath)
    {
        ValidatePath(pluginPath);

        if (File.Exists(pluginPath))
        {
            ValidateAssemblySignature(pluginPath);
        }
    }

    private static bool IsUncPath(string path)
    {
        return path.StartsWith(@"\\", StringComparison.Ordinal) ||
               path.StartsWith("//", StringComparison.Ordinal);
    }

    private static bool ContainsPathTraversal(string path)
    {
        // Check for common path traversal patterns
        var normalized = path.Replace('\\', '/');
        return normalized.Contains("/../") ||
               normalized.Contains("/./") ||
               normalized.StartsWith("../") ||
               normalized.StartsWith("./..") ||
               normalized.EndsWith("/..") ||
               normalized.EndsWith("/.") ||
               normalized == ".." ||
               normalized == ".";
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        try
        {
            var fullPath = Path.GetFullPath(path);
            // Ensure trailing separator for directory comparison
            if (!fullPath.EndsWith(Path.DirectorySeparatorChar))
            {
                fullPath += Path.DirectorySeparatorChar;
            }
            return fullPath;
        }
        catch
        {
            return string.Empty;
        }
    }
}
