using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Exceptions;

namespace ExperimentFramework.Tests.Configuration;

public class TypeResolverTests
{
    [Fact]
    public void Resolve_WithFullyQualifiedName_ReturnsType()
    {
        // Arrange
        var resolver = new TypeResolver();
        var fullName = typeof(List<string>).AssemblyQualifiedName!;

        // Act
        var result = resolver.Resolve(fullName);

        // Assert
        Assert.Equal(typeof(List<string>), result);
    }

    [Fact]
    public void Resolve_WithSimpleName_ReturnsType()
    {
        // Arrange
        var resolver = new TypeResolver();

        // Act
        var result = resolver.Resolve("String");

        // Assert
        Assert.Equal(typeof(string), result);
    }

    [Fact]
    public void Resolve_WithAlias_ReturnsAliasedType()
    {
        // Arrange
        var resolver = new TypeResolver();
        resolver.RegisterAlias("MyList", typeof(List<int>));

        // Act
        var result = resolver.Resolve("MyList");

        // Assert
        Assert.Equal(typeof(List<int>), result);
    }

    [Fact]
    public void Resolve_WithUnknownType_ThrowsTypeResolutionException()
    {
        // Arrange
        var resolver = new TypeResolver();

        // Act & Assert
        Assert.Throws<TypeResolutionException>(() => resolver.Resolve("NonExistentType12345"));
    }

    [Fact]
    public void TryResolve_WithValidType_ReturnsTrue()
    {
        // Arrange
        var resolver = new TypeResolver();

        // Act
        var success = resolver.TryResolve("String", out var type);

        // Assert
        Assert.True(success);
        Assert.Equal(typeof(string), type);
    }

    [Fact]
    public void TryResolve_WithInvalidType_ReturnsFalse()
    {
        // Arrange
        var resolver = new TypeResolver();

        // Act
        var success = resolver.TryResolve("NonExistentType12345", out var type);

        // Assert
        Assert.False(success);
        Assert.Null(type);
    }

    [Fact]
    public void TryResolve_WithNullOrEmpty_ReturnsFalse()
    {
        // Arrange
        var resolver = new TypeResolver();

        // Act & Assert
        Assert.False(resolver.TryResolve(null!, out _));
        Assert.False(resolver.TryResolve("", out _));
        Assert.False(resolver.TryResolve("   ", out _));
    }

    [Fact]
    public void RegisterAlias_OverwritesPreviousAlias()
    {
        // Arrange
        var resolver = new TypeResolver();
        resolver.RegisterAlias("MyType", typeof(int));

        // Act
        resolver.RegisterAlias("MyType", typeof(string));
        var result = resolver.Resolve("MyType");

        // Assert
        Assert.Equal(typeof(string), result);
    }

    [Fact]
    public void Constructor_WithTypeAliases_RegistersAliases()
    {
        // Arrange
        var aliases = new Dictionary<string, Type>
        {
            ["IntList"] = typeof(List<int>),
            ["StringDict"] = typeof(Dictionary<string, string>)
        };

        // Act
        var resolver = new TypeResolver(null, aliases);

        // Assert
        Assert.Equal(typeof(List<int>), resolver.Resolve("IntList"));
        Assert.Equal(typeof(Dictionary<string, string>), resolver.Resolve("StringDict"));
    }
}
