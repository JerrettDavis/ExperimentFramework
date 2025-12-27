using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Exceptions;
using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Integration;

namespace ExperimentFramework.Plugins.Tests.Integration;

public class PluginTypeResolverTests
{
    private readonly ITypeResolver _innerResolver;
    private readonly IPluginManager _pluginManager;
    private readonly PluginTypeResolver _resolver;

    public PluginTypeResolverTests()
    {
        _innerResolver = Substitute.For<ITypeResolver>();
        _pluginManager = Substitute.For<IPluginManager>();
        _pluginManager.GetLoadedPlugins().Returns([]);

        _resolver = new PluginTypeResolver(_innerResolver, _pluginManager);
    }

    [Fact]
    public void Constructor_ThrowsOnNullInnerResolver()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PluginTypeResolver(null!, _pluginManager));
    }

    [Fact]
    public void Constructor_ThrowsOnNullPluginManager()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PluginTypeResolver(_innerResolver, null!));
    }

    [Fact]
    public void TryResolve_NullTypeName_ReturnsFalse()
    {
        var result = _resolver.TryResolve(null!, out var type);

        Assert.False(result);
        Assert.Null(type);
    }

    [Fact]
    public void TryResolve_EmptyTypeName_ReturnsFalse()
    {
        var result = _resolver.TryResolve("", out var type);

        Assert.False(result);
        Assert.Null(type);
    }

    [Fact]
    public void TryResolve_PluginPrefix_CallsPluginManager()
    {
        _pluginManager.ResolveType("plugin:Test/alias").Returns(typeof(string));

        var result = _resolver.TryResolve("plugin:Test/alias", out var type);

        Assert.True(result);
        Assert.Equal(typeof(string), type);
        _pluginManager.Received(1).ResolveType("plugin:Test/alias");
    }

    [Fact]
    public void TryResolve_PluginPrefix_NotFound_ReturnsFalse()
    {
        _pluginManager.ResolveType("plugin:Test/alias").Returns((Type?)null);

        var result = _resolver.TryResolve("plugin:Test/alias", out var type);

        Assert.False(result);
        Assert.Null(type);
    }

    [Fact]
    public void TryResolve_NonPlugin_DelegatestoInnerResolver()
    {
        _innerResolver.TryResolve("SomeType", out Arg.Any<Type?>())
            .Returns(x =>
            {
                x[1] = typeof(int);
                return true;
            });

        var result = _resolver.TryResolve("SomeType", out var type);

        Assert.True(result);
        Assert.Equal(typeof(int), type);
    }

    [Fact]
    public void TryResolve_FallsBackToPluginSearch()
    {
        _innerResolver.TryResolve("SomeType", out Arg.Any<Type?>())
            .Returns(false);

        var mockContext = Substitute.For<IPluginContext>();
        mockContext.GetTypeByAlias("SomeType").Returns((Type?)null);
        mockContext.GetType("SomeType").Returns(typeof(double));

        _pluginManager.GetLoadedPlugins().Returns([mockContext]);

        var result = _resolver.TryResolve("SomeType", out var type);

        Assert.True(result);
        Assert.Equal(typeof(double), type);
    }

    [Fact]
    public void TryResolve_FallsBackToAlias()
    {
        _innerResolver.TryResolve("alias", out Arg.Any<Type?>())
            .Returns(false);

        var mockContext = Substitute.For<IPluginContext>();
        mockContext.GetTypeByAlias("alias").Returns(typeof(float));

        _pluginManager.GetLoadedPlugins().Returns([mockContext]);

        var result = _resolver.TryResolve("alias", out var type);

        Assert.True(result);
        Assert.Equal(typeof(float), type);
    }

    [Fact]
    public void Resolve_ThrowsWhenNotFound()
    {
        _innerResolver.TryResolve("Unknown", out Arg.Any<Type?>())
            .Returns(false);
        _pluginManager.GetLoadedPlugins().Returns([]);

        Assert.Throws<TypeResolutionException>(() => _resolver.Resolve("Unknown"));
    }

    [Fact]
    public void Resolve_ReturnsType()
    {
        _innerResolver.TryResolve("System.String", out Arg.Any<Type?>())
            .Returns(x =>
            {
                x[1] = typeof(string);
                return true;
            });

        var type = _resolver.Resolve("System.String");

        Assert.Equal(typeof(string), type);
    }

    [Fact]
    public void RegisterAlias_DelegatesToInnerResolver()
    {
        _resolver.RegisterAlias("myAlias", typeof(string));

        _innerResolver.Received(1).RegisterAlias("myAlias", typeof(string));
    }
}
