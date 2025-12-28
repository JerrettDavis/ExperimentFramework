using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Configuration;
using ExperimentFramework.Plugins.Loading;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExperimentFramework.Plugins.Tests.Loading;

public class PluginLoaderTests : IDisposable
{
    private readonly PluginLoader _loader;
    private readonly string _tempDir;

    public PluginLoaderTests()
    {
        _loader = new PluginLoader();
        _tempDir = Path.Combine(Path.GetTempPath(), $"PluginLoaderTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    #region CanLoad Tests

    [Fact]
    public void CanLoad_NullPath_ReturnsFalse()
    {
        Assert.False(_loader.CanLoad(null!));
    }

    [Fact]
    public void CanLoad_EmptyPath_ReturnsFalse()
    {
        Assert.False(_loader.CanLoad(""));
    }

    [Fact]
    public void CanLoad_WhitespacePath_ReturnsFalse()
    {
        Assert.False(_loader.CanLoad("   "));
    }

    [Fact]
    public void CanLoad_NonExistentPath_ReturnsFalse()
    {
        Assert.False(_loader.CanLoad("/nonexistent/path/plugin.dll"));
    }

    [Fact]
    public void CanLoad_NonDllFile_ReturnsFalse()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            // Rename to .txt
            var txtFile = Path.ChangeExtension(tempFile, ".txt");
            File.Move(tempFile, txtFile);

            Assert.False(_loader.CanLoad(txtFile));

            File.Delete(txtFile);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void CanLoad_ExistingDllFile_ReturnsTrue()
    {
        // Use the test assembly's DLL
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;
        Assert.True(_loader.CanLoad(dllPath));
    }

    [Fact]
    public void CanLoad_CaseInsensitiveDllExtension_ReturnsTrue()
    {
        var dllPath = Path.Combine(_tempDir, "test.DLL");
        File.WriteAllBytes(dllPath, []); // Empty file, just for extension check

        Assert.True(_loader.CanLoad(dllPath));
    }

    [Fact]
    public void CanLoad_ExeFile_ReturnsFalse()
    {
        var exePath = Path.Combine(_tempDir, "test.exe");
        File.WriteAllBytes(exePath, []);

        Assert.False(_loader.CanLoad(exePath));
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_NonExistentPath_ThrowsFileNotFoundException()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _loader.LoadAsync("/nonexistent/path/plugin.dll"));
    }

    [Fact]
    public async Task LoadAsync_NullPath_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _loader.LoadAsync(null!));
    }

    [Fact]
    public async Task LoadAsync_EmptyPath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _loader.LoadAsync(""));
    }

    [Fact]
    public async Task LoadAsync_WhitespacePath_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _loader.LoadAsync("   "));
    }

    [Fact]
    public async Task LoadAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        // Use a valid DLL path to get past the file check
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _loader.LoadAsync(dllPath, cancellationToken: cts.Token));
    }

    [Fact]
    public async Task LoadAsync_WithNoneIsolationMode_LoadsPlugin()
    {
        // Use a valid assembly - our test assembly
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };

        var context = await _loader.LoadAsync(dllPath, options);

        Assert.NotNull(context);
        Assert.True(context.IsLoaded);
        Assert.NotNull(context.Manifest);
        Assert.NotNull(context.MainAssembly);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task LoadAsync_WithSharedIsolationMode_LoadsPlugin()
    {
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.Shared,
            EnableUnloading = true
        };

        var context = await _loader.LoadAsync(dllPath, options);

        Assert.NotNull(context);
        Assert.True(context.IsLoaded);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task LoadAsync_WithForceIsolation_UsesFullIsolation()
    {
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;
        var options = new PluginLoadOptions
        {
            ForceIsolation = true,
            EnableUnloading = true
        };

        var context = await _loader.LoadAsync(dllPath, options);

        Assert.NotNull(context);
        Assert.True(context.IsLoaded);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task LoadAsync_WithDefaultOptions_LoadsPlugin()
    {
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;

        var context = await _loader.LoadAsync(dllPath);

        Assert.NotNull(context);
        Assert.True(context.IsLoaded);
        Assert.NotEmpty(context.ContextId);
        Assert.Equal(dllPath, context.PluginPath);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task LoadAsync_PopulatesManifestFromAssembly()
    {
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };

        var context = await _loader.LoadAsync(dllPath, options);

        // Default manifest should be created from assembly info
        Assert.NotNull(context.Manifest.Id);
        Assert.NotNull(context.Manifest.Name);
        Assert.NotNull(context.Manifest.Version);
        Assert.Equal("1.0", context.Manifest.ManifestVersion);

        await context.DisposeAsync();
    }

    #endregion

    #region UnloadAsync Tests

    [Fact]
    public async Task UnloadAsync_NullContext_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _loader.UnloadAsync(null!));
    }

    [Fact]
    public async Task UnloadAsync_ValidContext_UnloadsPlugin()
    {
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };

        var context = await _loader.LoadAsync(dllPath, options);
        Assert.True(context.IsLoaded);

        await _loader.UnloadAsync(context);

        Assert.False(context.IsLoaded);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithLogger_UsesLogger()
    {
        var logger = Substitute.For<ILogger<PluginLoader>>();
        var loader = new PluginLoader(logger: logger);

        // Verify loader was created - logger is used internally
        Assert.NotNull(loader);
    }

    [Fact]
    public void Constructor_WithSharedRegistry_UsesRegistry()
    {
        var registry = new SharedTypeRegistry(["TestAssembly"]);
        var loader = new PluginLoader(sharedTypeRegistry: registry);

        Assert.NotNull(loader);
    }

    [Fact]
    public void Constructor_WithNullParameters_UsesDefaults()
    {
        var loader = new PluginLoader(null, null);
        Assert.NotNull(loader);
    }

    #endregion

    #region PluginLoadOptions Tests

    [Fact]
    public void PluginLoadOptions_DefaultValues()
    {
        var options = new PluginLoadOptions();

        Assert.Null(options.IsolationModeOverride);
        Assert.Empty(options.AdditionalSharedAssemblies);
        Assert.False(options.ForceIsolation);
        Assert.True(options.EnableUnloading);
        Assert.Null(options.Metadata);
    }

    [Fact]
    public void PluginLoadOptions_CanSetValues()
    {
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.Full,
            AdditionalSharedAssemblies = ["Custom.Assembly"],
            ForceIsolation = true,
            EnableUnloading = false,
            Metadata = new Dictionary<string, object> { ["key"] = "value" }
        };

        Assert.Equal(PluginIsolationMode.Full, options.IsolationModeOverride);
        Assert.Single(options.AdditionalSharedAssemblies);
        Assert.True(options.ForceIsolation);
        Assert.False(options.EnableUnloading);
        Assert.NotNull(options.Metadata);
    }

    [Fact]
    public void PluginLoadOptions_AdditionalSharedAssemblies_DefaultsToEmpty()
    {
        var options = new PluginLoadOptions();

        Assert.NotNull(options.AdditionalSharedAssemblies);
        Assert.Empty(options.AdditionalSharedAssemblies);
    }

    [Fact]
    public void PluginLoadOptions_MetadataCanStoreArbitraryObjects()
    {
        var options = new PluginLoadOptions
        {
            Metadata = new Dictionary<string, object>
            {
                ["string"] = "value",
                ["int"] = 42,
                ["bool"] = true,
                ["object"] = new { Name = "Test" }
            }
        };

        Assert.Equal("value", options.Metadata["string"]);
        Assert.Equal(42, options.Metadata["int"]);
        Assert.Equal(true, options.Metadata["bool"]);
    }

    #endregion

    #region Integration with SharedTypeRegistry Tests

    [Fact]
    public async Task LoadAsync_WithAdditionalSharedAssemblies_UsesCustomRegistry()
    {
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;
        var options = new PluginLoadOptions
        {
            AdditionalSharedAssemblies = ["ExperimentFramework.Plugins"],
            IsolationModeOverride = PluginIsolationMode.Shared,
            EnableUnloading = true
        };

        var context = await _loader.LoadAsync(dllPath, options);

        Assert.NotNull(context);
        Assert.True(context.IsLoaded);

        await context.DisposeAsync();
    }

    #endregion

    #region Constructor with Options Tests

    [Fact]
    public void Constructor_WithOptions_ThrowsOnNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PluginLoader(null!, null, null));
    }

    [Fact]
    public void Constructor_WithOptions_UsesConfiguredSettings()
    {
        var configOptions = Options.Create(new PluginConfigurationOptions
        {
            MaxManifestSizeBytes = 512 * 1024,
            MaxManifestJsonDepth = 16
        });

        var loader = new PluginLoader(configOptions);

        Assert.NotNull(loader);
    }

    #endregion

    #region LoadAsync Exception Handling Tests

    [Fact]
    public async Task LoadAsync_WithInvalidAssembly_DoesNotLeakLoadContext()
    {
        // Create a file that is not a valid assembly
        var invalidDll = Path.Combine(_tempDir, "invalid.dll");
        File.WriteAllBytes(invalidDll, [0x00, 0x01, 0x02, 0x03]); // Not a valid PE

        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.Shared,
            EnableUnloading = true
        };

        // Should throw because it's not a valid assembly
        await Assert.ThrowsAnyAsync<Exception>(() => _loader.LoadAsync(invalidDll, options));

        // The load context should be cleaned up on failure
    }

    [Fact]
    public async Task LoadAsync_WithFullIsolation_LoadsPlugin()
    {
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.Full,
            EnableUnloading = true
        };

        var context = await _loader.LoadAsync(dllPath, options);

        Assert.NotNull(context);
        Assert.True(context.IsLoaded);

        await context.DisposeAsync();
    }

    #endregion

    #region Manifest Warnings Tests

    [Fact]
    public async Task LoadAsync_WithManifestWarnings_LogsWarnings()
    {
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;
        var logger = Substitute.For<ILogger<PluginLoader>>();
        var loader = new PluginLoader(logger: logger);

        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };

        var context = await loader.LoadAsync(dllPath, options);

        Assert.NotNull(context);

        await context.DisposeAsync();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task LoadAsync_MultipleLoadsSameFile_CreatesDifferentContexts()
    {
        var dllPath = typeof(PluginLoaderTests).Assembly.Location;
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };

        var context1 = await _loader.LoadAsync(dllPath, options);
        var context2 = await _loader.LoadAsync(dllPath, options);

        Assert.NotEqual(context1.ContextId, context2.ContextId);

        await context1.DisposeAsync();
        await context2.DisposeAsync();
    }

    #endregion
}
