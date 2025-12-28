using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Manifest;

namespace ExperimentFramework.Plugins.Tests.Manifest;

public class PluginManifestAttributeTests
{
    [Fact]
    public void Constructor_SetsId()
    {
        var attribute = new PluginManifestAttribute("Test.Plugin");

        Assert.Equal("Test.Plugin", attribute.Id);
    }

    [Fact]
    public void Name_DefaultsToNull()
    {
        var attribute = new PluginManifestAttribute("Test.Plugin");

        Assert.Null(attribute.Name);
    }

    [Fact]
    public void Name_CanBeSet()
    {
        var attribute = new PluginManifestAttribute("Test.Plugin")
        {
            Name = "Test Plugin Display Name"
        };

        Assert.Equal("Test Plugin Display Name", attribute.Name);
    }

    [Fact]
    public void Version_DefaultsToNull()
    {
        var attribute = new PluginManifestAttribute("Test.Plugin");

        Assert.Null(attribute.Version);
    }

    [Fact]
    public void Version_CanBeSet()
    {
        var attribute = new PluginManifestAttribute("Test.Plugin")
        {
            Version = "2.0.0"
        };

        Assert.Equal("2.0.0", attribute.Version);
    }

    [Fact]
    public void Description_DefaultsToNull()
    {
        var attribute = new PluginManifestAttribute("Test.Plugin");

        Assert.Null(attribute.Description);
    }

    [Fact]
    public void Description_CanBeSet()
    {
        var attribute = new PluginManifestAttribute("Test.Plugin")
        {
            Description = "A test plugin for unit testing"
        };

        Assert.Equal("A test plugin for unit testing", attribute.Description);
    }

    [Fact]
    public void SupportsHotReload_DefaultsToTrue()
    {
        var attribute = new PluginManifestAttribute("Test.Plugin");

        Assert.True(attribute.SupportsHotReload);
    }

    [Fact]
    public void SupportsHotReload_CanBeSetToFalse()
    {
        var attribute = new PluginManifestAttribute("Test.Plugin")
        {
            SupportsHotReload = false
        };

        Assert.False(attribute.SupportsHotReload);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        var attribute = new PluginManifestAttribute("My.Plugin")
        {
            Name = "My Plugin",
            Version = "3.1.4",
            Description = "Plugin description",
            SupportsHotReload = false
        };

        Assert.Equal("My.Plugin", attribute.Id);
        Assert.Equal("My Plugin", attribute.Name);
        Assert.Equal("3.1.4", attribute.Version);
        Assert.Equal("Plugin description", attribute.Description);
        Assert.False(attribute.SupportsHotReload);
    }
}

public class PluginIsolationAttributeTests
{
    [Fact]
    public void Mode_DefaultsToShared()
    {
        var attribute = new PluginIsolationAttribute();

        Assert.Equal(PluginIsolationMode.Shared, attribute.Mode);
    }

    [Fact]
    public void Mode_CanBeSetToFull()
    {
        var attribute = new PluginIsolationAttribute
        {
            Mode = PluginIsolationMode.Full
        };

        Assert.Equal(PluginIsolationMode.Full, attribute.Mode);
    }

    [Fact]
    public void Mode_CanBeSetToNone()
    {
        var attribute = new PluginIsolationAttribute
        {
            Mode = PluginIsolationMode.None
        };

        Assert.Equal(PluginIsolationMode.None, attribute.Mode);
    }

    [Fact]
    public void SharedAssemblies_DefaultsToEmptyArray()
    {
        var attribute = new PluginIsolationAttribute();

        Assert.NotNull(attribute.SharedAssemblies);
        Assert.Empty(attribute.SharedAssemblies);
    }

    [Fact]
    public void SharedAssemblies_CanBeSet()
    {
        var attribute = new PluginIsolationAttribute
        {
            SharedAssemblies = ["Assembly1", "Assembly2", "Assembly3"]
        };

        Assert.Equal(3, attribute.SharedAssemblies.Length);
        Assert.Contains("Assembly1", attribute.SharedAssemblies);
        Assert.Contains("Assembly2", attribute.SharedAssemblies);
        Assert.Contains("Assembly3", attribute.SharedAssemblies);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        var attribute = new PluginIsolationAttribute
        {
            Mode = PluginIsolationMode.Full,
            SharedAssemblies = ["SharedLib"]
        };

        Assert.Equal(PluginIsolationMode.Full, attribute.Mode);
        Assert.Single(attribute.SharedAssemblies);
    }
}

public class PluginServiceAttributeTests
{
    [Fact]
    public void InterfaceName_IsRequired()
    {
        var attribute = new PluginServiceAttribute
        {
            InterfaceName = "IPaymentProcessor",
            Implementations = ["StripeProcessor:stripe"]
        };

        Assert.Equal("IPaymentProcessor", attribute.InterfaceName);
    }

    [Fact]
    public void Implementations_IsRequired()
    {
        var attribute = new PluginServiceAttribute
        {
            InterfaceName = "IService",
            Implementations = ["Impl1:alias1", "Impl2:alias2"]
        };

        Assert.Equal(2, attribute.Implementations.Length);
    }

    [Fact]
    public void Implementations_WithTypeAndAlias_ParsesCorrectly()
    {
        var attribute = new PluginServiceAttribute
        {
            InterfaceName = "IProcessor",
            Implementations = ["MyNamespace.MyProcessor:my-processor"]
        };

        Assert.Single(attribute.Implementations);
        Assert.Equal("MyNamespace.MyProcessor:my-processor", attribute.Implementations[0]);
    }

    [Fact]
    public void Implementations_WithOnlyType_IsValid()
    {
        var attribute = new PluginServiceAttribute
        {
            InterfaceName = "IService",
            Implementations = ["SimpleImplementation"]
        };

        Assert.Single(attribute.Implementations);
        Assert.Equal("SimpleImplementation", attribute.Implementations[0]);
    }

    [Fact]
    public void MultipleImplementations_AllStored()
    {
        var attribute = new PluginServiceAttribute
        {
            InterfaceName = "IPaymentProcessor",
            Implementations = [
                "StripeProcessor:stripe",
                "PayPalProcessor:paypal",
                "SquareProcessor:square"
            ]
        };

        Assert.Equal(3, attribute.Implementations.Length);
    }
}

public class PluginImplementationAttributeTests
{
    [Fact]
    public void Alias_DefaultsToNull()
    {
        var attribute = new PluginImplementationAttribute();

        Assert.Null(attribute.Alias);
    }

    [Fact]
    public void Alias_CanBeSet()
    {
        var attribute = new PluginImplementationAttribute
        {
            Alias = "my-custom-alias"
        };

        Assert.Equal("my-custom-alias", attribute.Alias);
    }

    [Fact]
    public void ServiceInterface_DefaultsToNull()
    {
        var attribute = new PluginImplementationAttribute();

        Assert.Null(attribute.ServiceInterface);
    }

    [Fact]
    public void ServiceInterface_CanBeSet()
    {
        var attribute = new PluginImplementationAttribute
        {
            ServiceInterface = typeof(IDisposable)
        };

        Assert.Equal(typeof(IDisposable), attribute.ServiceInterface);
    }

    [Fact]
    public void Exclude_DefaultsToFalse()
    {
        var attribute = new PluginImplementationAttribute();

        Assert.False(attribute.Exclude);
    }

    [Fact]
    public void Exclude_CanBeSetToTrue()
    {
        var attribute = new PluginImplementationAttribute
        {
            Exclude = true
        };

        Assert.True(attribute.Exclude);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        var attribute = new PluginImplementationAttribute
        {
            Alias = "custom",
            ServiceInterface = typeof(IComparable),
            Exclude = false
        };

        Assert.Equal("custom", attribute.Alias);
        Assert.Equal(typeof(IComparable), attribute.ServiceInterface);
        Assert.False(attribute.Exclude);
    }
}

public class GeneratePluginManifestAttributeTests
{
    [Fact]
    public void Id_DefaultsToNull()
    {
        var attribute = new GeneratePluginManifestAttribute();

        Assert.Null(attribute.Id);
    }

    [Fact]
    public void Id_CanBeSet()
    {
        var attribute = new GeneratePluginManifestAttribute
        {
            Id = "Custom.Plugin.Id"
        };

        Assert.Equal("Custom.Plugin.Id", attribute.Id);
    }

    [Fact]
    public void Name_DefaultsToNull()
    {
        var attribute = new GeneratePluginManifestAttribute();

        Assert.Null(attribute.Name);
    }

    [Fact]
    public void Name_CanBeSet()
    {
        var attribute = new GeneratePluginManifestAttribute
        {
            Name = "Custom Plugin Name"
        };

        Assert.Equal("Custom Plugin Name", attribute.Name);
    }

    [Fact]
    public void Description_DefaultsToNull()
    {
        var attribute = new GeneratePluginManifestAttribute();

        Assert.Null(attribute.Description);
    }

    [Fact]
    public void Description_CanBeSet()
    {
        var attribute = new GeneratePluginManifestAttribute
        {
            Description = "A plugin that does things"
        };

        Assert.Equal("A plugin that does things", attribute.Description);
    }

    [Fact]
    public void IsolationMode_DefaultsToShared()
    {
        var attribute = new GeneratePluginManifestAttribute();

        Assert.Equal(PluginIsolationMode.Shared, attribute.IsolationMode);
    }

    [Fact]
    public void IsolationMode_CanBeSetToFull()
    {
        var attribute = new GeneratePluginManifestAttribute
        {
            IsolationMode = PluginIsolationMode.Full
        };

        Assert.Equal(PluginIsolationMode.Full, attribute.IsolationMode);
    }

    [Fact]
    public void IsolationMode_CanBeSetToNone()
    {
        var attribute = new GeneratePluginManifestAttribute
        {
            IsolationMode = PluginIsolationMode.None
        };

        Assert.Equal(PluginIsolationMode.None, attribute.IsolationMode);
    }

    [Fact]
    public void SharedAssemblies_DefaultsToNull()
    {
        var attribute = new GeneratePluginManifestAttribute();

        Assert.Null(attribute.SharedAssemblies);
    }

    [Fact]
    public void SharedAssemblies_CanBeSet()
    {
        var attribute = new GeneratePluginManifestAttribute
        {
            SharedAssemblies = ["Lib1", "Lib2"]
        };

        Assert.NotNull(attribute.SharedAssemblies);
        Assert.Equal(2, attribute.SharedAssemblies.Length);
    }

    [Fact]
    public void SupportsHotReload_DefaultsToTrue()
    {
        var attribute = new GeneratePluginManifestAttribute();

        Assert.True(attribute.SupportsHotReload);
    }

    [Fact]
    public void SupportsHotReload_CanBeSetToFalse()
    {
        var attribute = new GeneratePluginManifestAttribute
        {
            SupportsHotReload = false
        };

        Assert.False(attribute.SupportsHotReload);
    }

    [Fact]
    public void AllProperties_CanBeSetTogether()
    {
        var attribute = new GeneratePluginManifestAttribute
        {
            Id = "My.Plugin",
            Name = "My Plugin",
            Description = "Description here",
            IsolationMode = PluginIsolationMode.Full,
            SharedAssemblies = ["SharedLib"],
            SupportsHotReload = false
        };

        Assert.Equal("My.Plugin", attribute.Id);
        Assert.Equal("My Plugin", attribute.Name);
        Assert.Equal("Description here", attribute.Description);
        Assert.Equal(PluginIsolationMode.Full, attribute.IsolationMode);
        Assert.Single(attribute.SharedAssemblies!);
        Assert.False(attribute.SupportsHotReload);
    }
}
