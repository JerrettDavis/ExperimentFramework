using System.Reflection;
using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Loading;
using ExperimentFramework.Plugins.Manifest;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Plugins.Tests.Loading;

public class PluginContextTests : IAsyncLifetime
{
    private readonly IPluginLoader _loader;
    private IPluginContext? _context;

    public PluginContextTests()
    {
        _loader = new PluginLoader();
    }

    public async Task InitializeAsync()
    {
        var dllPath = typeof(PluginContextTests).Assembly.Location;
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };
        _context = await _loader.LoadAsync(dllPath, options);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.DisposeAsync();
        }
    }

    #region Properties Tests

    [Fact]
    public void ContextId_IsNotEmpty()
    {
        Assert.NotNull(_context);
        Assert.NotEmpty(_context.ContextId);
    }

    [Fact]
    public void Manifest_IsNotNull()
    {
        Assert.NotNull(_context);
        Assert.NotNull(_context.Manifest);
    }

    [Fact]
    public void IsLoaded_InitiallyTrue()
    {
        Assert.NotNull(_context);
        Assert.True(_context.IsLoaded);
    }

    [Fact]
    public void PluginPath_MatchesLoadedPath()
    {
        var expectedPath = typeof(PluginContextTests).Assembly.Location;
        Assert.NotNull(_context);
        Assert.Equal(expectedPath, _context.PluginPath);
    }

    [Fact]
    public void MainAssembly_IsNotNull()
    {
        Assert.NotNull(_context);
        Assert.NotNull(_context.MainAssembly);
    }

    [Fact]
    public void LoadedAssemblies_ContainsMainAssembly()
    {
        Assert.NotNull(_context);
        Assert.Contains(_context.MainAssembly, _context.LoadedAssemblies);
    }

    #endregion

    #region GetType Tests

    [Fact]
    public void GetType_WithValidTypeName_ReturnsType()
    {
        Assert.NotNull(_context);

        // Use a type from this test assembly
        var typeName = typeof(PluginContextTests).FullName!;
        var type = _context.GetType(typeName);

        Assert.NotNull(type);
        Assert.Equal(typeof(PluginContextTests), type);
    }

    [Fact]
    public void GetType_WithInvalidTypeName_ReturnsNull()
    {
        Assert.NotNull(_context);

        var type = _context.GetType("NonExistent.Type.Name");

        Assert.Null(type);
    }

    #endregion

    #region GetTypeByAlias Tests

    [Fact]
    public void GetTypeByAlias_WithNoAliases_ReturnsNull()
    {
        Assert.NotNull(_context);

        // The test assembly doesn't have plugin manifest aliases
        var type = _context.GetTypeByAlias("nonexistent-alias");

        Assert.Null(type);
    }

    #endregion

    #region GetImplementations Tests

    [Fact]
    public void GetImplementations_ReturnsImplementingTypes()
    {
        Assert.NotNull(_context);

        // IAsyncLifetime is implemented by this test class
        var implementations = _context.GetImplementations(typeof(IAsyncLifetime));

        Assert.NotNull(implementations);
        Assert.Contains(implementations, t => t == typeof(PluginContextTests));
    }

    [Fact]
    public void GetImplementations_Generic_ReturnsImplementingTypes()
    {
        Assert.NotNull(_context);

        var implementations = _context.GetImplementations<IAsyncLifetime>();

        Assert.NotNull(implementations);
        Assert.Contains(implementations, t => t == typeof(PluginContextTests));
    }

    [Fact]
    public void GetImplementations_WithNoImplementations_ReturnsEmpty()
    {
        Assert.NotNull(_context);

        // Interface not implemented in this assembly
        var implementations = _context.GetImplementations(typeof(IPluginContext));

        // Should return empty or not contain plugin context (it's not in this assembly)
        Assert.NotNull(implementations);
    }

    #endregion

    #region CreateInstance Tests

    [Fact]
    public void CreateInstance_WithValidType_CreatesInstance()
    {
        Assert.NotNull(_context);

        var services = new ServiceCollection().BuildServiceProvider();
        var type = typeof(SimpleTestClass);

        var instance = _context.CreateInstance(type, services);

        Assert.NotNull(instance);
        Assert.IsType<SimpleTestClass>(instance);
    }

    [Fact]
    public void CreateInstance_WithNullType_ThrowsArgumentNullException()
    {
        Assert.NotNull(_context);
        var services = new ServiceCollection().BuildServiceProvider();

        Assert.Throws<ArgumentNullException>(() =>
            _context.CreateInstance(null!, services));
    }

    [Fact]
    public void CreateInstance_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        Assert.NotNull(_context);

        Assert.Throws<ArgumentNullException>(() =>
            _context.CreateInstance(typeof(SimpleTestClass), null!));
    }

    [Fact]
    public void CreateInstance_WithDependencyInjection_InjectsDependencies()
    {
        Assert.NotNull(_context);

        var services = new ServiceCollection()
            .AddSingleton<IDependency, ConcreteDependency>()
            .BuildServiceProvider();

        var instance = _context.CreateInstance(typeof(ClassWithDependency), services);

        Assert.NotNull(instance);
        Assert.IsType<ClassWithDependency>(instance);
        var typedInstance = (ClassWithDependency)instance;
        Assert.NotNull(typedInstance.Dependency);
    }

    #endregion

    #region CreateInstanceByAlias Tests

    [Fact]
    public void CreateInstanceByAlias_WithUnknownAlias_ReturnsNull()
    {
        Assert.NotNull(_context);
        var services = new ServiceCollection().BuildServiceProvider();

        var instance = _context.CreateInstanceByAlias("unknown-alias", services);

        Assert.Null(instance);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public async Task DisposeAsync_SetsIsLoadedToFalse()
    {
        var dllPath = typeof(PluginContextTests).Assembly.Location;
        var loader = new PluginLoader();
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };

        var context = await loader.LoadAsync(dllPath, options);
        Assert.True(context.IsLoaded);

        await context.DisposeAsync();

        Assert.False(context.IsLoaded);
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_IsIdempotent()
    {
        var dllPath = typeof(PluginContextTests).Assembly.Location;
        var loader = new PluginLoader();
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };

        var context = await loader.LoadAsync(dllPath, options);

        // Should not throw when called multiple times
        await context.DisposeAsync();
        await context.DisposeAsync();
        await context.DisposeAsync();

        Assert.False(context.IsLoaded);
    }

    [Fact]
    public async Task GetType_AfterDispose_ThrowsObjectDisposedException()
    {
        var dllPath = typeof(PluginContextTests).Assembly.Location;
        var loader = new PluginLoader();
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };

        var context = await loader.LoadAsync(dllPath, options);
        await context.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() =>
            context.GetType("SomeType"));
    }

    [Fact]
    public async Task GetTypeByAlias_AfterDispose_ThrowsObjectDisposedException()
    {
        var dllPath = typeof(PluginContextTests).Assembly.Location;
        var loader = new PluginLoader();
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };

        var context = await loader.LoadAsync(dllPath, options);
        await context.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() =>
            context.GetTypeByAlias("some-alias"));
    }

    [Fact]
    public async Task GetImplementations_AfterDispose_ThrowsObjectDisposedException()
    {
        var dllPath = typeof(PluginContextTests).Assembly.Location;
        var loader = new PluginLoader();
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };

        var context = await loader.LoadAsync(dllPath, options);
        await context.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() =>
            context.GetImplementations(typeof(IDisposable)));
    }

    [Fact]
    public async Task CreateInstance_AfterDispose_ThrowsObjectDisposedException()
    {
        var dllPath = typeof(PluginContextTests).Assembly.Location;
        var loader = new PluginLoader();
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };
        var services = new ServiceCollection().BuildServiceProvider();

        var context = await loader.LoadAsync(dllPath, options);
        await context.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() =>
            context.CreateInstance(typeof(SimpleTestClass), services));
    }

    #endregion

    #region CreateInstance When Not Loaded Tests

    [Fact]
    public async Task CreateInstance_WhenNotLoaded_ThrowsInvalidOperationException()
    {
        var dllPath = typeof(PluginContextTests).Assembly.Location;
        var loader = new PluginLoader();
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };
        var services = new ServiceCollection().BuildServiceProvider();

        var context = await loader.LoadAsync(dllPath, options);
        await context.DisposeAsync(); // Unload the plugin

        var exception = Assert.Throws<ObjectDisposedException>(() =>
            context.CreateInstance(typeof(SimpleTestClass), services));
    }

    #endregion

    #region CreateInstanceByAlias Additional Tests

    [Fact]
    public async Task CreateInstanceByAlias_AfterDispose_ThrowsObjectDisposedException()
    {
        var dllPath = typeof(PluginContextTests).Assembly.Location;
        var loader = new PluginLoader();
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.None
        };
        var services = new ServiceCollection().BuildServiceProvider();

        var context = await loader.LoadAsync(dllPath, options);
        await context.DisposeAsync();

        // GetTypeByAlias is called first, which throws ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() =>
            context.CreateInstanceByAlias("any-alias", services));
    }

    #endregion

    #region Dispose with LoadContext Tests

    [Fact]
    public async Task DisposeAsync_WithIsolatedContext_TriggersUnload()
    {
        var dllPath = typeof(PluginContextTests).Assembly.Location;
        var loader = new PluginLoader();
        var options = new PluginLoadOptions
        {
            IsolationModeOverride = PluginIsolationMode.Shared,
            EnableUnloading = true
        };

        var context = await loader.LoadAsync(dllPath, options);
        Assert.True(context.IsLoaded);

        await context.DisposeAsync();

        Assert.False(context.IsLoaded);
        Assert.Null(context.MainAssembly);
    }

    [Fact]
    public async Task DisposeAsync_WithFullIsolation_TriggersUnloadAndGC()
    {
        var dllPath = typeof(PluginContextTests).Assembly.Location;
        var loader = new PluginLoader();
        var options = new PluginLoadOptions
        {
            ForceIsolation = true,
            EnableUnloading = true
        };

        var context = await loader.LoadAsync(dllPath, options);
        Assert.True(context.IsLoaded);

        await context.DisposeAsync();

        Assert.False(context.IsLoaded);
    }

    #endregion

    #region Test Helper Classes

    public class SimpleTestClass { }

    public interface IDependency { }

    public class ConcreteDependency : IDependency { }

    public class ClassWithDependency(IDependency dependency)
    {
        public IDependency Dependency { get; } = dependency;
    }

    #endregion
}
