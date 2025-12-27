using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Loading;

namespace ExperimentFramework.Plugins.Tests.Loading;

public class PluginLoaderTests
{
    private readonly PluginLoader _loader;

    public PluginLoaderTests()
    {
        _loader = new PluginLoader();
    }

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

    // Note: Actual plugin loading tests would require a real plugin assembly.
    // Those are covered in integration tests with a sample plugin project.
}
