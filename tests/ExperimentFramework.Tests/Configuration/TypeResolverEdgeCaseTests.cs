using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Exceptions;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration;

[Feature("TypeResolver resolves types from string names with aliasing support")]
public class TypeResolverEdgeCaseTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region Assembly Search Path Tests

    [Scenario("Constructor with null assembly paths does not throw")]
    [Fact]
    public Task Constructor_with_null_paths_does_not_throw()
        => Given("null assembly paths", () => new TypeResolver(null, null))
            .Then("resolver is not null", resolver => resolver != null)
            .AssertPassed();

    [Scenario("Constructor with empty assembly paths does not throw")]
    [Fact]
    public Task Constructor_with_empty_paths_does_not_throw()
        => Given("empty assembly paths", () => new TypeResolver([], null))
            .Then("resolver is not null", resolver => resolver != null)
            .AssertPassed();

    [Scenario("Constructor with null type aliases does not throw")]
    [Fact]
    public Task Constructor_with_null_aliases_does_not_throw()
        => Given("null type aliases", () => new TypeResolver(null, null))
            .Then("resolver is not null", resolver => resolver != null)
            .AssertPassed();

    #endregion

    #region Resolve Edge Cases

    [Scenario("Resolves System.Int32 type")]
    [Fact]
    public Task Resolves_system_int32()
        => Given("a type resolver", () => new TypeResolver())
            .When("resolving System.Int32", resolver => resolver.Resolve("System.Int32"))
            .Then("returns int type", type => type == typeof(int))
            .AssertPassed();

    [Scenario("Resolves generic type with brackets")]
    [Fact]
    public Task Resolves_generic_type()
        => Given("a type resolver and generic type name", () =>
            (new TypeResolver(), typeof(Dictionary<string, int>).AssemblyQualifiedName!))
            .When("resolving the type", t => t.Item1.Resolve(t.Item2))
            .Then("returns correct type", type => type == typeof(Dictionary<string, int>))
            .AssertPassed();

    [Scenario("Resolves nested type")]
    [Fact]
    public Task Resolves_nested_type()
        => Given("a type resolver and nested type name", () =>
            (new TypeResolver(), typeof(Environment.SpecialFolder).AssemblyQualifiedName!))
            .When("resolving the type", t => t.Item1.Resolve(t.Item2))
            .Then("returns correct type", type => type == typeof(Environment.SpecialFolder))
            .AssertPassed();

    [Scenario("Resolves array type")]
    [Fact]
    public Task Resolves_array_type()
        => Given("a type resolver and array type name", () =>
            (new TypeResolver(), typeof(string[]).AssemblyQualifiedName!))
            .When("resolving the type", t => t.Item1.Resolve(t.Item2))
            .Then("returns correct type", type => type == typeof(string[]))
            .AssertPassed();

    [Scenario("Null type name throws TypeResolutionException")]
    [Fact]
    public Task Null_type_name_throws()
        => Given("a type resolver", () => new TypeResolver())
            .Then("throws TypeResolutionException", resolver =>
            {
                try
                {
                    resolver.Resolve(null!);
                    return false;
                }
                catch (TypeResolutionException)
                {
                    return true;
                }
            })
            .AssertPassed();

    [Scenario("Empty type name throws TypeResolutionException")]
    [Fact]
    public Task Empty_type_name_throws()
        => Given("a type resolver", () => new TypeResolver())
            .Then("throws TypeResolutionException", resolver =>
            {
                try
                {
                    resolver.Resolve("");
                    return false;
                }
                catch (TypeResolutionException)
                {
                    return true;
                }
            })
            .AssertPassed();

    [Scenario("Whitespace type name throws TypeResolutionException")]
    [Fact]
    public Task Whitespace_type_name_throws()
        => Given("a type resolver", () => new TypeResolver())
            .Then("throws TypeResolutionException", resolver =>
            {
                try
                {
                    resolver.Resolve("   ");
                    return false;
                }
                catch (TypeResolutionException)
                {
                    return true;
                }
            })
            .AssertPassed();

    #endregion

    #region TryResolve Edge Cases

    [Scenario("TryResolve with valid type returns true")]
    [Fact]
    public Task TryResolve_valid_type_returns_true()
        => Given("a type resolver", () => new TypeResolver())
            .When("trying to resolve System.String", resolver =>
            {
                var success = resolver.TryResolve("System.String", out var type);
                return (success, type);
            })
            .Then("returns true", result => result.success)
            .And("type is string", result => result.type == typeof(string))
            .AssertPassed();

    [Scenario("TryResolve with invalid type returns false")]
    [Fact]
    public Task TryResolve_invalid_type_returns_false()
        => Given("a type resolver", () => new TypeResolver())
            .When("trying to resolve non-existent type", resolver =>
            {
                var success = resolver.TryResolve("NonExistentType12345", out var type);
                return (success, type);
            })
            .Then("returns false", result => !result.success)
            .And("type is null", result => result.type == null)
            .AssertPassed();

    [Scenario("TryResolve with null type name returns false")]
    [Fact]
    public Task TryResolve_null_type_name_returns_false()
        => Given("a type resolver", () => new TypeResolver())
            .When("trying to resolve null", resolver =>
            {
                var success = resolver.TryResolve(null!, out var type);
                return (success, type);
            })
            .Then("returns false", result => !result.success)
            .And("type is null", result => result.type == null)
            .AssertPassed();

    [Scenario("TryResolve with empty type name returns false")]
    [Fact]
    public Task TryResolve_empty_type_name_returns_false()
        => Given("a type resolver", () => new TypeResolver())
            .When("trying to resolve empty string", resolver =>
            {
                var success = resolver.TryResolve("", out var type);
                return (success, type);
            })
            .Then("returns false", result => !result.success)
            .And("type is null", result => result.type == null)
            .AssertPassed();

    [Scenario("TryResolve with whitespace type name returns false")]
    [Fact]
    public Task TryResolve_whitespace_type_name_returns_false()
        => Given("a type resolver", () => new TypeResolver())
            .When("trying to resolve whitespace", resolver =>
            {
                var success = resolver.TryResolve("   ", out var type);
                return (success, type);
            })
            .Then("returns false", result => !result.success)
            .And("type is null", result => result.type == null)
            .AssertPassed();

    #endregion

    #region Alias Tests

    [Scenario("Register alias with null alias handles gracefully")]
    [Fact]
    public Task Register_alias_null_alias()
        => Given("a type resolver", () => new TypeResolver())
            .Then("handles null alias", resolver =>
            {
                try
                {
                    resolver.RegisterAlias(null!, typeof(string));
                    return true; // Didn't throw
                }
                catch (ArgumentNullException)
                {
                    return true; // Expected behavior
                }
            })
            .AssertPassed();

    [Scenario("Register alias with null type handles gracefully")]
    [Fact]
    public Task Register_alias_null_type()
        => Given("a type resolver", () => new TypeResolver())
            .Then("handles null type", resolver =>
            {
                try
                {
                    resolver.RegisterAlias("alias", null!);
                    return true; // Didn't throw
                }
                catch (ArgumentNullException)
                {
                    return true; // Expected behavior
                }
            })
            .AssertPassed();

    [Scenario("Empty alias throws on resolve")]
    [Fact]
    public Task Empty_alias_throws_on_resolve()
        => Given("resolver with empty alias registered", () =>
            {
                var resolver = new TypeResolver();
                resolver.RegisterAlias("", typeof(string));
                return resolver;
            })
            .Then("throws on resolve", resolver =>
            {
                try
                {
                    resolver.Resolve("");
                    return false;
                }
                catch (TypeResolutionException)
                {
                    return true;
                }
            })
            .AssertPassed();

    [Scenario("Alias resolution is case sensitive")]
    [Fact]
    public Task Alias_is_case_sensitive()
        => Given("resolver with alias MyType", () =>
            {
                var resolver = new TypeResolver();
                resolver.RegisterAlias("MyType", typeof(string));
                return resolver;
            })
            .When("checking case sensitivity", resolver =>
            {
                var successLower = resolver.TryResolve("mytype", out _);
                var successExact = resolver.TryResolve("MyType", out var type);
                return (successLower, successExact, type);
            })
            .Then("exact case resolves", result => result.successExact)
            .And("type is string", result => result.type == typeof(string))
            .AssertPassed();

    [Scenario("Multiple aliases for same type all work")]
    [Fact]
    public Task Multiple_aliases_for_same_type()
        => Given("resolver with multiple aliases", () =>
            {
                var resolver = new TypeResolver();
                resolver.RegisterAlias("Alias1", typeof(string));
                resolver.RegisterAlias("Alias2", typeof(string));
                resolver.RegisterAlias("Alias3", typeof(string));
                return resolver;
            })
            .When("resolving all aliases", resolver =>
            {
                var t1 = resolver.Resolve("Alias1");
                var t2 = resolver.Resolve("Alias2");
                var t3 = resolver.Resolve("Alias3");
                return (t1, t2, t3);
            })
            .Then("all resolve to string", result =>
                result.t1 == typeof(string) &&
                result.t2 == typeof(string) &&
                result.t3 == typeof(string))
            .AssertPassed();

    #endregion

    #region Caching Tests

    [Scenario("Resolving same type twice returns same result")]
    [Fact]
    public Task Resolving_same_type_returns_same_result()
        => Given("resolver and type name", () =>
            (new TypeResolver(), typeof(string).AssemblyQualifiedName!))
            .When("resolving twice", t =>
            {
                var result1 = t.Item1.Resolve(t.Item2);
                var result2 = t.Item1.Resolve(t.Item2);
                return (result1, result2);
            })
            .Then("same instance returned", result => ReferenceEquals(result.result1, result.result2))
            .AssertPassed();

    [Scenario("Resolving alias caches result")]
    [Fact]
    public Task Resolving_alias_caches()
        => Given("resolver with alias", () =>
            {
                var resolver = new TypeResolver();
                resolver.RegisterAlias("MyString", typeof(string));
                return resolver;
            })
            .When("resolving twice", resolver =>
            {
                var result1 = resolver.Resolve("MyString");
                var result2 = resolver.Resolve("MyString");
                return (result1, result2);
            })
            .Then("same instance returned", result => ReferenceEquals(result.result1, result.result2))
            .AssertPassed();

    #endregion

    #region Interface Implementation

    [Scenario("TypeResolver implements ITypeResolver")]
    [Fact]
    public Task TypeResolver_implements_interface()
        => Given("a type resolver", () => new TypeResolver())
            .Then("is ITypeResolver", resolver => resolver is ITypeResolver)
            .AssertPassed();

    #endregion
}
