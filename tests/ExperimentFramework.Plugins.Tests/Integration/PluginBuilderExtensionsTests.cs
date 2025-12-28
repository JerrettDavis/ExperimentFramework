using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Integration;

namespace ExperimentFramework.Plugins.Tests.Integration;

public class PluginBuilderExtensionsTests
{
    [Fact]
    public void PluginType_CreatesCorrectReference()
    {
        var reference = PluginBuilderExtensions.PluginType("Acme.Plugin", "my-alias");

        Assert.Equal("plugin:Acme.Plugin/my-alias", reference);
    }

    [Fact]
    public void PluginType_NullPluginId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PluginBuilderExtensions.PluginType(null!, "alias"));
    }

    [Fact]
    public void PluginType_NullTypeIdentifier_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            PluginBuilderExtensions.PluginType("Plugin", null!));
    }

    [Fact]
    public void TryParsePluginTypeReference_ValidReference_ReturnsTrue()
    {
        var result = PluginBuilderExtensions.TryParsePluginTypeReference(
            "plugin:Acme.Plugin/my-alias",
            out var pluginId,
            out var typeIdentifier);

        Assert.True(result);
        Assert.Equal("Acme.Plugin", pluginId);
        Assert.Equal("my-alias", typeIdentifier);
    }

    [Fact]
    public void TryParsePluginTypeReference_FullTypeName_ReturnsTrue()
    {
        var result = PluginBuilderExtensions.TryParsePluginTypeReference(
            "plugin:Acme.Plugin/Acme.Plugin.Services.MyService",
            out var pluginId,
            out var typeIdentifier);

        Assert.True(result);
        Assert.Equal("Acme.Plugin", pluginId);
        Assert.Equal("Acme.Plugin.Services.MyService", typeIdentifier);
    }

    [Fact]
    public void TryParsePluginTypeReference_NoPrefix_ReturnsFalse()
    {
        var result = PluginBuilderExtensions.TryParsePluginTypeReference(
            "Acme.Plugin/alias",
            out var pluginId,
            out var typeIdentifier);

        Assert.False(result);
        Assert.Empty(pluginId);
        Assert.Empty(typeIdentifier);
    }

    [Fact]
    public void TryParsePluginTypeReference_NoSlash_ReturnsFalse()
    {
        var result = PluginBuilderExtensions.TryParsePluginTypeReference(
            "plugin:Acme.Plugin",
            out var pluginId,
            out var typeIdentifier);

        Assert.False(result);
    }

    [Fact]
    public void TryParsePluginTypeReference_EmptyPluginId_ReturnsFalse()
    {
        var result = PluginBuilderExtensions.TryParsePluginTypeReference(
            "plugin:/alias",
            out var pluginId,
            out var typeIdentifier);

        Assert.False(result);
    }

    [Fact]
    public void TryParsePluginTypeReference_EmptyTypeIdentifier_ReturnsFalse()
    {
        var result = PluginBuilderExtensions.TryParsePluginTypeReference(
            "plugin:Acme.Plugin/",
            out var pluginId,
            out var typeIdentifier);

        Assert.False(result);
    }

    [Fact]
    public void TryParsePluginTypeReference_Null_ReturnsFalse()
    {
        var result = PluginBuilderExtensions.TryParsePluginTypeReference(
            null!,
            out var pluginId,
            out var typeIdentifier);

        Assert.False(result);
    }

    [Fact]
    public void GetPluginImplementations_NullManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IPluginManager)null!).GetPluginImplementations<IDisposable>().ToList());
    }

    [Fact]
    public void GetPluginImplementations_NoPlugins_ReturnsEmpty()
    {
        var manager = Substitute.For<IPluginManager>();
        manager.GetLoadedPlugins().Returns([]);

        var implementations = manager.GetPluginImplementations<IDisposable>().ToList();

        Assert.Empty(implementations);
    }

    [Fact]
    public void GetPluginImplementations_FindsTypes()
    {
        var manager = Substitute.For<IPluginManager>();
        var context = Substitute.For<IPluginContext>();
        context.GetImplementations<IDisposable>().Returns([typeof(MemoryStream)]);
        manager.GetLoadedPlugins().Returns([context]);

        var implementations = manager.GetPluginImplementations<IDisposable>().ToList();

        Assert.Single(implementations);
        Assert.Equal(typeof(MemoryStream), implementations[0]);
    }

    [Fact]
    public void GetPluginServicesForInterface_NullManager_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ((IPluginManager)null!).GetPluginServicesForInterface("ITest").ToList());
    }

    [Fact]
    public void GetPluginServicesForInterface_NullInterface_ThrowsArgumentNullException()
    {
        var manager = Substitute.For<IPluginManager>();

        Assert.Throws<ArgumentNullException>(() =>
            manager.GetPluginServicesForInterface(null!).ToList());
    }

    [Fact]
    public void GetPluginServicesForInterface_FindsServices()
    {
        var manager = Substitute.For<IPluginManager>();
        var context = Substitute.For<IPluginContext>();
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Services.Returns([
            new PluginServiceRegistration
            {
                Interface = "ITestService",
                Implementations = [new PluginImplementation { Type = "TestImpl", Alias = "test" }]
            }
        ]);
        context.Manifest.Returns(manifest);
        manager.GetLoadedPlugins().Returns([context]);

        var services = manager.GetPluginServicesForInterface("ITestService").ToList();

        Assert.Single(services);
        Assert.Equal("TestImpl", services[0].Implementation.Type);
        Assert.Equal("test", services[0].Implementation.Alias);
    }

    [Fact]
    public void GetPluginServicesForInterface_MatchesSimpleName()
    {
        var manager = Substitute.For<IPluginManager>();
        var context = Substitute.For<IPluginContext>();
        var manifest = Substitute.For<IPluginManifest>();
        manifest.Services.Returns([
            new PluginServiceRegistration
            {
                Interface = "Acme.Services.ITestService",
                Implementations = [new PluginImplementation { Type = "TestImpl" }]
            }
        ]);
        context.Manifest.Returns(manifest);
        manager.GetLoadedPlugins().Returns([context]);

        var services = manager.GetPluginServicesForInterface("ITestService").ToList();

        Assert.Single(services);
    }
}
