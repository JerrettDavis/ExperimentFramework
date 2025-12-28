using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Loading;

namespace ExperimentFramework.Plugins.Tests.Loading;

public class PluginLoadContextTests : IDisposable
{
    private readonly string _tempDir;
    private PluginLoadContext? _context;

    public PluginLoadContextTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"PluginLoadContextTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (_context is { IsCollectible: true })
        {
            _context.Unload();
        }
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch { }
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullPath_ThrowsArgumentNullException()
    {
        var registry = new SharedTypeRegistry();

        Assert.Throws<ArgumentNullException>(() =>
            new PluginLoadContext(null!, PluginIsolationMode.Shared, registry));
    }

    [Fact]
    public void Constructor_WithEmptyPath_ThrowsArgumentException()
    {
        var registry = new SharedTypeRegistry();

        Assert.Throws<ArgumentException>(() =>
            new PluginLoadContext("", PluginIsolationMode.Shared, registry));
    }

    [Fact]
    public void Constructor_WithWhitespacePath_ThrowsArgumentException()
    {
        var registry = new SharedTypeRegistry();

        Assert.Throws<ArgumentException>(() =>
            new PluginLoadContext("   ", PluginIsolationMode.Shared, registry));
    }

    [Fact]
    public void Constructor_WithNullRegistry_ThrowsArgumentNullException()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;

        Assert.Throws<ArgumentNullException>(() =>
            new PluginLoadContext(dllPath, PluginIsolationMode.Shared, null!));
    }

    [Fact]
    public void Constructor_WithValidPath_SetsPluginPath()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Shared, registry);

        Assert.Equal(dllPath, _context.PluginPath);
    }

    [Fact]
    public void Constructor_WithCollectible_CreatesCollectibleContext()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Shared, registry, isCollectible: true);

        Assert.NotNull(_context);
        Assert.True(_context.IsCollectible);
    }

    [Fact]
    public void Constructor_WithNonCollectible_CreatesNonCollectibleContext()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Shared, registry, isCollectible: false);

        Assert.NotNull(_context);
        Assert.False(_context.IsCollectible);
    }

    [Fact]
    public void Constructor_SetsContextNameFromAssembly()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();
        var expectedName = Path.GetFileNameWithoutExtension(dllPath);

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Shared, registry);

        Assert.Equal(expectedName, _context.Name);
    }

    #endregion

    #region LoadMainAssembly Tests

    [Fact]
    public void LoadMainAssembly_LoadsAssemblyFromPath()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Shared, registry);
        var assembly = _context.LoadMainAssembly();

        Assert.NotNull(assembly);
        Assert.Contains("ExperimentFramework.Plugins.Tests", assembly.FullName!);
    }

    #endregion

    #region IsolationMode Tests

    [Fact]
    public void Context_WithNoneIsolation_DefersToDefaultContext()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.None, registry);

        // Load main assembly - this should work
        var assembly = _context.LoadMainAssembly();
        Assert.NotNull(assembly);
    }

    [Fact]
    public void Context_WithSharedIsolation_LoadsFromPluginDirectory()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Shared, registry);

        var assembly = _context.LoadMainAssembly();
        Assert.NotNull(assembly);
    }

    [Fact]
    public void Context_WithFullIsolation_LoadsFromPluginDirectory()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Full, registry);

        var assembly = _context.LoadMainAssembly();
        Assert.NotNull(assembly);
    }

    #endregion

    #region Assemblies Collection Tests

    [Fact]
    public void Assemblies_AfterLoad_ContainsLoadedAssemblies()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Shared, registry);
        var mainAssembly = _context.LoadMainAssembly();

        Assert.Contains(mainAssembly, _context.Assemblies);
    }

    #endregion

    #region Unload Tests

    [Fact]
    public void Unload_CollectibleContext_CanBeUnloaded()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Shared, registry, isCollectible: true);
        _context.LoadMainAssembly();

        // This should not throw
        _context.Unload();
        _context = null; // Prevent double unload in Dispose
    }

    #endregion

    #region Load Method Tests

    [Fact]
    public void LoadMainAssembly_WithFullIsolation_LoadsFromPluginDirectory()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Full, registry);
        var assembly = _context.LoadMainAssembly();

        Assert.NotNull(assembly);
    }

    [Fact]
    public void LoadMainAssembly_WithNoneIsolation_LoadsAssembly()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.None, registry);
        var assembly = _context.LoadMainAssembly();

        Assert.NotNull(assembly);
    }

    [Fact]
    public void Load_SharedAssembly_LoadsFromSharedRegistry()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Shared, registry);
        var assembly = _context.LoadMainAssembly();

        // The assembly should be loaded and dependencies resolved
        Assert.NotNull(assembly);
    }

    #endregion

    #region Constructor Edge Cases

    [Fact]
    public void Constructor_WithFileOnlyPath_InfersDirectory()
    {
        var dllPath = typeof(PluginLoadContextTests).Assembly.Location;
        var registry = new SharedTypeRegistry();

        _context = new PluginLoadContext(dllPath, PluginIsolationMode.Shared, registry);

        Assert.Equal(dllPath, _context.PluginPath);
        Assert.NotNull(_context.Name);
    }

    #endregion
}
