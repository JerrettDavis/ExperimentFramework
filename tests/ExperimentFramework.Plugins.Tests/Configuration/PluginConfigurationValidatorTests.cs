using ExperimentFramework.Plugins.Configuration;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Plugins.Tests.Configuration;

public class PluginConfigurationValidatorTests
{
    private readonly PluginConfigurationValidator _validator = new();

    #region Discovery Paths Tests

    [Fact]
    public void Validate_EmptyDiscoveryPath_ReturnsFailure()
    {
        var options = new PluginConfigurationOptions
        {
            DiscoveryPaths = ["./valid", "", "   "]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Discovery paths cannot contain empty or whitespace values", result.FailureMessage);
    }

    [Fact]
    public void Validate_ValidDiscoveryPaths_ReturnsSuccess()
    {
        var options = new PluginConfigurationOptions
        {
            DiscoveryPaths = ["./plugins", "/app/plugins", "C:\\plugins"]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    #endregion

    #region Allowed Plugin Directories Tests

    [Fact]
    public void Validate_EmptyAllowedDirectory_ReturnsFailure()
    {
        var options = new PluginConfigurationOptions
        {
            AllowedPluginDirectories = ["/valid", ""]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Allowed plugin directories cannot contain empty or whitespace values", result.FailureMessage);
    }

    [Fact]
    public void Validate_ValidAllowedDirectories_ReturnsSuccess()
    {
        var options = new PluginConfigurationOptions
        {
            AllowedPluginDirectories = ["/app/plugins"]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    #endregion

    #region Thumbprint Validation Tests

    [Fact]
    public void Validate_EmptyThumbprint_ReturnsFailure()
    {
        var options = new PluginConfigurationOptions
        {
            TrustedPublisherThumbprints = ["", "   "]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Trusted publisher thumbprints cannot contain empty or whitespace values", result.FailureMessage);
    }

    [Fact]
    public void Validate_InvalidThumbprintLength_ReturnsFailure()
    {
        var options = new PluginConfigurationOptions
        {
            TrustedPublisherThumbprints = ["TOOSHORT"]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Invalid certificate thumbprint format", result.FailureMessage);
    }

    [Fact]
    public void Validate_InvalidThumbprintCharacters_ReturnsFailure()
    {
        var options = new PluginConfigurationOptions
        {
            // 40 chars but contains invalid characters (G, Z, !)
            TrustedPublisherThumbprints = ["GGGGGGGGGGGGGGGGGGGG!!!!!!!!!!!!!!!!!!!!"]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Invalid certificate thumbprint format", result.FailureMessage);
    }

    [Fact]
    public void Validate_ValidThumbprint_ReturnsSuccess()
    {
        var options = new PluginConfigurationOptions
        {
            // Valid 40 hex character thumbprint
            TrustedPublisherThumbprints = ["A1B2C3D4E5F6A1B2C3D4E5F6A1B2C3D4E5F6A1B2"]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_LowercaseThumbprint_ReturnsSuccess()
    {
        var options = new PluginConfigurationOptions
        {
            TrustedPublisherThumbprints = ["a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2"]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    #endregion

    #region Size Limits Tests

    [Fact]
    public void Validate_ZeroManifestSize_ReturnsFailure()
    {
        var options = new PluginConfigurationOptions
        {
            MaxManifestSizeBytes = 0
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxManifestSizeBytes must be positive", result.FailureMessage);
    }

    [Fact]
    public void Validate_NegativeManifestSize_ReturnsFailure()
    {
        var options = new PluginConfigurationOptions
        {
            MaxManifestSizeBytes = -100
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxManifestSizeBytes must be positive", result.FailureMessage);
    }

    [Fact]
    public void Validate_ZeroJsonDepth_ReturnsFailure()
    {
        var options = new PluginConfigurationOptions
        {
            MaxManifestJsonDepth = 0
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxManifestJsonDepth must be positive", result.FailureMessage);
    }

    [Fact]
    public void Validate_NegativeJsonDepth_ReturnsFailure()
    {
        var options = new PluginConfigurationOptions
        {
            MaxManifestJsonDepth = -1
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("MaxManifestJsonDepth must be positive", result.FailureMessage);
    }

    [Fact]
    public void Validate_ValidSizeLimits_ReturnsSuccess()
    {
        var options = new PluginConfigurationOptions
        {
            MaxManifestSizeBytes = 1024 * 1024,
            MaxManifestJsonDepth = 32
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    #endregion

    #region Hot Reload Settings Tests

    [Fact]
    public void Validate_NegativeDebounceWithHotReload_ReturnsFailure()
    {
        var options = new PluginConfigurationOptions
        {
            EnableHotReload = true,
            HotReloadDebounceMs = -1
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("HotReloadDebounceMs cannot be negative", result.FailureMessage);
    }

    [Fact]
    public void Validate_NegativeDebounceWithoutHotReload_ReturnsSuccess()
    {
        var options = new PluginConfigurationOptions
        {
            EnableHotReload = false,
            HotReloadDebounceMs = -1
        };

        var result = _validator.Validate(null, options);

        // Should succeed because hot reload is disabled
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ValidHotReloadSettings_ReturnsSuccess()
    {
        var options = new PluginConfigurationOptions
        {
            EnableHotReload = true,
            HotReloadDebounceMs = 500
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    #endregion

    #region Shared Assemblies Tests

    [Fact]
    public void Validate_EmptySharedAssembly_ReturnsFailure()
    {
        var options = new PluginConfigurationOptions
        {
            DefaultSharedAssemblies = ["ValidAssembly", "", "   "]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains("Default shared assemblies cannot contain empty or whitespace values", result.FailureMessage);
    }

    [Fact]
    public void Validate_ValidSharedAssemblies_ReturnsSuccess()
    {
        var options = new PluginConfigurationOptions
        {
            DefaultSharedAssemblies = ["ExperimentFramework", "Microsoft.Extensions.DependencyInjection"]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    #endregion

    #region Default Options Tests

    [Fact]
    public void Validate_DefaultOptions_ReturnsSuccess()
    {
        var options = new PluginConfigurationOptions();

        var result = _validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    #endregion

    #region Multiple Errors Tests

    [Fact]
    public void Validate_MultipleErrors_ReturnsAllErrors()
    {
        var options = new PluginConfigurationOptions
        {
            DiscoveryPaths = [""],
            AllowedPluginDirectories = [""],
            TrustedPublisherThumbprints = ["invalid"],
            MaxManifestSizeBytes = 0,
            DefaultSharedAssemblies = [""]
        };

        var result = _validator.Validate(null, options);

        Assert.True(result.Failed);
        // Should contain multiple errors
        Assert.Contains("Discovery paths cannot contain empty", result.FailureMessage);
        Assert.Contains("Allowed plugin directories cannot contain empty", result.FailureMessage);
        Assert.Contains("Invalid certificate thumbprint", result.FailureMessage);
        Assert.Contains("MaxManifestSizeBytes must be positive", result.FailureMessage);
        Assert.Contains("Default shared assemblies cannot contain empty", result.FailureMessage);
    }

    #endregion
}
