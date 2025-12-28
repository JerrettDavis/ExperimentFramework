using System.Runtime.InteropServices;
using System.Security;
using ExperimentFramework.Plugins.Configuration;
using ExperimentFramework.Plugins.Security;

namespace ExperimentFramework.Plugins.Tests.Security;

public class PluginSecurityValidatorTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempFile;

    public PluginSecurityValidatorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PluginSecurityTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _tempFile = Path.Combine(_tempDir, "test.dll");
        File.WriteAllBytes(_tempFile, new byte[] { 0x4D, 0x5A }); // MZ header
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PluginSecurityValidator(null!));
    }

    [Fact]
    public void Constructor_WithValidOptions_Succeeds()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        Assert.NotNull(validator);
    }

    #endregion

    #region ValidatePath Tests

    [Fact]
    public void ValidatePath_NullPath_ThrowsArgumentException()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        Assert.Throws<ArgumentNullException>(() => validator.ValidatePath(null!));
    }

    [Fact]
    public void ValidatePath_EmptyPath_ThrowsArgumentException()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        Assert.Throws<ArgumentException>(() => validator.ValidatePath(""));
        Assert.Throws<ArgumentException>(() => validator.ValidatePath("   "));
    }

    [Fact]
    public void ValidatePath_UncPathWhenNotAllowed_ThrowsSecurityException()
    {
        var options = new PluginConfigurationOptions { AllowUncPaths = false };
        var validator = new PluginSecurityValidator(options);

        // Both Windows-style and Unix-style UNC paths should be blocked
        Assert.Throws<SecurityException>(() => validator.ValidatePath(@"\\server\share\plugin.dll"));
        Assert.Throws<SecurityException>(() => validator.ValidatePath("//server/share/plugin.dll"));
    }

    [Fact]
    public void ValidatePath_UncPathWhenAllowed_DoesNotThrow()
    {
        var options = new PluginConfigurationOptions { AllowUncPaths = true };
        var validator = new PluginSecurityValidator(options);

        // Should not throw - UNC paths are allowed
        validator.ValidatePath(@"\\server\share\plugin.dll");
        validator.ValidatePath("//server/share/plugin.dll");
    }

    [Fact]
    public void ValidatePath_PathTraversal_ThrowsSecurityException()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        Assert.Throws<SecurityException>(() => validator.ValidatePath("../../../plugin.dll"));
        Assert.Throws<SecurityException>(() => validator.ValidatePath("./.."));
        Assert.Throws<SecurityException>(() => validator.ValidatePath("/path/../other/plugin.dll"));
        Assert.Throws<SecurityException>(() => validator.ValidatePath("/path/./secret/../plugin.dll"));
    }

    [Fact]
    public void ValidatePath_NotInAllowedDirectories_ThrowsSecurityException()
    {
        var options = new PluginConfigurationOptions
        {
            AllowedPluginDirectories = [_tempDir]
        };
        var validator = new PluginSecurityValidator(options);

        Assert.Throws<SecurityException>(() => validator.ValidatePath("/other/directory/plugin.dll"));
    }

    [Fact]
    public void ValidatePath_InAllowedDirectory_DoesNotThrow()
    {
        var options = new PluginConfigurationOptions
        {
            AllowedPluginDirectories = [_tempDir]
        };
        var validator = new PluginSecurityValidator(options);

        // Should not throw
        validator.ValidatePath(_tempFile);
    }

    [Fact]
    public void ValidatePath_NoAllowedDirectoriesConfigured_AllowsAnyPath()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        // Should not throw when no allowed directories are configured
        validator.ValidatePath(_tempFile);
    }

    #endregion

    #region ValidateAssemblySignature Tests

    [Fact]
    public void ValidateAssemblySignature_NoSignatureRequired_DoesNotThrow()
    {
        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = false
        };
        var validator = new PluginSecurityValidator(options);

        // Should not throw for unsigned assembly when not required
        validator.ValidateAssemblySignature(_tempFile);
    }

    [Fact]
    public void ValidateAssemblySignature_SignatureRequiredButUnsigned_ThrowsSecurityException()
    {
        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = true
        };
        var validator = new PluginSecurityValidator(options);

        // Should throw for unsigned assembly when required
        Assert.Throws<SecurityException>(() => validator.ValidateAssemblySignature(_tempFile));
    }

    #endregion

    #region ValidatePlugin Tests

    [Fact]
    public void ValidatePlugin_ValidPath_DoesNotThrow()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        // Should not throw for valid path
        validator.ValidatePlugin(_tempFile);
    }

    [Fact]
    public void ValidatePlugin_NonExistentFile_DoesNotValidateSignature()
    {
        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = true
        };
        var validator = new PluginSecurityValidator(options);

        // Should not throw because file doesn't exist (signature check skipped)
        validator.ValidatePlugin("/nonexistent/plugin.dll");
    }

    [Fact]
    public void ValidatePlugin_PathTraversal_ThrowsSecurityException()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        Assert.Throws<SecurityException>(() => validator.ValidatePlugin("../plugin.dll"));
    }

    #endregion

    #region Additional ValidatePath Tests

    [Fact]
    public void ValidatePath_DotDotAtEnd_ThrowsSecurityException()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        Assert.Throws<SecurityException>(() => validator.ValidatePath("/path/to/.."));
    }

    [Fact]
    public void ValidatePath_DotAtEnd_ThrowsSecurityException()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        Assert.Throws<SecurityException>(() => validator.ValidatePath("/path/to/."));
    }

    [Fact]
    public void ValidatePath_JustDotDot_ThrowsSecurityException()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        Assert.Throws<SecurityException>(() => validator.ValidatePath(".."));
    }

    [Fact]
    public void ValidatePath_JustDot_ThrowsSecurityException()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        Assert.Throws<SecurityException>(() => validator.ValidatePath("."));
    }

    [Fact]
    public void ValidatePath_WindowsBackslashTraversal_ThrowsSecurityException()
    {
        var options = new PluginConfigurationOptions();
        var validator = new PluginSecurityValidator(options);

        Assert.Throws<SecurityException>(() => validator.ValidatePath(@"path\..\secret\file.dll"));
    }

    [Fact]
    public void ValidatePath_InSubdirectoryOfAllowed_DoesNotThrow()
    {
        var subDir = Path.Combine(_tempDir, "subdir");
        Directory.CreateDirectory(subDir);
        var subFile = Path.Combine(subDir, "plugin.dll");
        File.WriteAllBytes(subFile, [0x4D, 0x5A]);

        var options = new PluginConfigurationOptions
        {
            AllowedPluginDirectories = [_tempDir]
        };
        var validator = new PluginSecurityValidator(options);

        // Should not throw - subdirectory of allowed directory
        validator.ValidatePath(subFile);
    }

    [Fact]
    public void ValidatePath_MultipleAllowedDirectories_AllowsAny()
    {
        var dir1 = Path.Combine(Path.GetTempPath(), $"PluginSecurityTests1_{Guid.NewGuid():N}");
        var dir2 = Path.Combine(Path.GetTempPath(), $"PluginSecurityTests2_{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);
        var file1 = Path.Combine(dir1, "plugin1.dll");
        var file2 = Path.Combine(dir2, "plugin2.dll");
        File.WriteAllBytes(file1, [0x4D, 0x5A]);
        File.WriteAllBytes(file2, [0x4D, 0x5A]);

        try
        {
            var options = new PluginConfigurationOptions
            {
                AllowedPluginDirectories = [dir1, dir2]
            };
            var validator = new PluginSecurityValidator(options);

            // Both should pass
            validator.ValidatePath(file1);
            validator.ValidatePath(file2);
        }
        finally
        {
            try { Directory.Delete(dir1, true); } catch { }
            try { Directory.Delete(dir2, true); } catch { }
        }
    }

    #endregion

    #region ValidateAssemblySignature Additional Tests

    [Fact]
    public void ValidateAssemblySignature_WithTrustedThumbprintsButNoSignature_ThrowsWhenRequired()
    {
        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = true,
            TrustedPublisherThumbprints = ["ABC123DEF456ABC123DEF456ABC123DEF456ABCD"]
        };
        var validator = new PluginSecurityValidator(options);

        // Should throw because assembly is not signed
        Assert.Throws<SecurityException>(() => validator.ValidateAssemblySignature(_tempFile));
    }

    [Fact]
    public void ValidateAssemblySignature_TrustedThumbprintsOnly_SkipsValidationForUnsigned()
    {
        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = false,
            TrustedPublisherThumbprints = ["ABC123DEF456ABC123DEF456ABC123DEF456ABCD"]
        };
        var validator = new PluginSecurityValidator(options);

        // Should not throw - file is not signed but signing is not required
        // However, the trusted thumbprints list means we need to check
        // For an unsigned file, this should throw when there are trusted thumbprints
        // Actually no - looking at the code, if cert is null and RequireSignedAssemblies is false, it returns
        validator.ValidateAssemblySignature(_tempFile);
    }

    #endregion

    #region Constructor with Logger Tests

    [Fact]
    public void Constructor_WithLogger_Succeeds()
    {
        var options = new PluginConfigurationOptions();
        var logger = NSubstitute.Substitute.For<Microsoft.Extensions.Logging.ILogger<PluginSecurityValidator>>();

        var validator = new PluginSecurityValidator(options, logger);

        Assert.NotNull(validator);
    }

    #endregion
}

/// <summary>
/// Tests for PluginSecurityValidator that require a signed executable.
/// These tests use dotnet.exe which is Authenticode signed by Microsoft.
/// </summary>
public class PluginSecurityValidatorSignedAssemblyTests
{
    private static readonly string? DotnetExePath = FindDotnetExe();

    private static string? FindDotnetExe()
    {
        // Try common locations for dotnet.exe
        var paths = new[]
        {
            @"C:\Program Files\dotnet\dotnet.exe",
            @"C:\Program Files (x86)\dotnet\dotnet.exe",
            Environment.GetEnvironmentVariable("DOTNET_ROOT") is string root
                ? Path.Combine(root, "dotnet.exe")
                : null
        };

        foreach (var path in paths.Where(p => p != null))
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private static bool HasSignedDotnetExe => DotnetExePath != null &&
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    // Known Microsoft .NET certificate thumbprint (may change with updates)
    // This is the thumbprint for the ".NET" certificate from Microsoft
    private const string MicrosoftDotNetThumbprint = "860AB2B78578D8EF61F692CF81AE4B1198CCBC94";

    [SkippableFact]
    public void ValidateAssemblySignature_SignedAssemblyWithMatchingThumbprint_DoesNotThrow()
    {
        Skip.IfNot(HasSignedDotnetExe, "Signed dotnet.exe not available on this platform");

        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = true,
            TrustedPublisherThumbprints = [MicrosoftDotNetThumbprint]
        };
        var validator = new PluginSecurityValidator(options);

        // Should not throw - dotnet.exe is signed with the trusted thumbprint
        validator.ValidateAssemblySignature(DotnetExePath!);
    }

    [SkippableFact]
    public void ValidateAssemblySignature_SignedAssemblyWithNonMatchingThumbprint_ThrowsSecurityException()
    {
        Skip.IfNot(HasSignedDotnetExe, "Signed dotnet.exe not available on this platform");

        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = true,
            TrustedPublisherThumbprints = ["0000000000000000000000000000000000000000"] // Wrong thumbprint
        };
        var validator = new PluginSecurityValidator(options);

        // Should throw - thumbprint doesn't match
        var ex = Assert.Throws<SecurityException>(() =>
            validator.ValidateAssemblySignature(DotnetExePath!));

        Assert.Contains("not signed by a trusted publisher", ex.Message);
        Assert.Contains(MicrosoftDotNetThumbprint, ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [SkippableFact]
    public void ValidateAssemblySignature_SignedAssemblyWithRequiredButNoThumbprints_DoesNotThrow()
    {
        Skip.IfNot(HasSignedDotnetExe, "Signed dotnet.exe not available on this platform");

        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = true,
            TrustedPublisherThumbprints = [] // No specific thumbprints - any signed assembly is OK
        };
        var validator = new PluginSecurityValidator(options);

        // Should not throw - file is signed and no specific thumbprints required
        validator.ValidateAssemblySignature(DotnetExePath!);
    }

    [SkippableFact]
    public void ValidateAssemblySignature_SignedAssemblyWithOnlyThumbprintsConfigured_DoesNotThrow()
    {
        Skip.IfNot(HasSignedDotnetExe, "Signed dotnet.exe not available on this platform");

        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = false, // Signing not required
            TrustedPublisherThumbprints = [MicrosoftDotNetThumbprint] // But if signed, must match
        };
        var validator = new PluginSecurityValidator(options);

        // Should not throw - thumbprint matches
        validator.ValidateAssemblySignature(DotnetExePath!);
    }

    [SkippableFact]
    public void ValidateAssemblySignature_SignedAssemblyWithCaseInsensitiveThumbprint_DoesNotThrow()
    {
        Skip.IfNot(HasSignedDotnetExe, "Signed dotnet.exe not available on this platform");

        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = true,
            TrustedPublisherThumbprints = [MicrosoftDotNetThumbprint.ToLowerInvariant()] // lowercase
        };
        var validator = new PluginSecurityValidator(options);

        // Should not throw - thumbprint comparison is case-insensitive
        validator.ValidateAssemblySignature(DotnetExePath!);
    }

    [SkippableFact]
    public void ValidateAssemblySignature_WithMultipleThumbprintsIncludingMatch_DoesNotThrow()
    {
        Skip.IfNot(HasSignedDotnetExe, "Signed dotnet.exe not available on this platform");

        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = true,
            TrustedPublisherThumbprints =
            [
                "1111111111111111111111111111111111111111",
                "2222222222222222222222222222222222222222",
                MicrosoftDotNetThumbprint, // This one matches
                "3333333333333333333333333333333333333333"
            ]
        };
        var validator = new PluginSecurityValidator(options);

        // Should not throw - one of the thumbprints matches
        validator.ValidateAssemblySignature(DotnetExePath!);
    }

    [SkippableFact]
    public void ValidateAssemblySignature_WithMultipleThumbprintsNoneMatching_ThrowsSecurityException()
    {
        Skip.IfNot(HasSignedDotnetExe, "Signed dotnet.exe not available on this platform");

        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = true,
            TrustedPublisherThumbprints =
            [
                "1111111111111111111111111111111111111111",
                "2222222222222222222222222222222222222222",
                "3333333333333333333333333333333333333333"
            ]
        };
        var validator = new PluginSecurityValidator(options);

        // Should throw - none of the thumbprints match
        Assert.Throws<SecurityException>(() =>
            validator.ValidateAssemblySignature(DotnetExePath!));
    }

    [SkippableFact]
    public void ValidatePlugin_SignedAssemblyWithMatchingThumbprint_DoesNotThrow()
    {
        Skip.IfNot(HasSignedDotnetExe, "Signed dotnet.exe not available on this platform");

        var options = new PluginConfigurationOptions
        {
            RequireSignedAssemblies = true,
            TrustedPublisherThumbprints = [MicrosoftDotNetThumbprint]
        };
        var validator = new PluginSecurityValidator(options);

        // Should not throw - full validation passes
        validator.ValidatePlugin(DotnetExePath!);
    }
}
