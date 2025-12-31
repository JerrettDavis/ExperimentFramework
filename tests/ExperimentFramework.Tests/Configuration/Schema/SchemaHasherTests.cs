using ExperimentFramework.Configuration.Schema;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration.Schema;

[Feature("Schema hasher computes deterministic, fast hashes for configuration schemas")]
public class SchemaHasherTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Computing hash of empty string returns zero hash")]
    [Fact]
    public Task ComputeHash_empty_string_returns_zero()
        => Given("an empty string", () => string.Empty)
            .When("computing hash", content => SchemaHasher.ComputeHash(content))
            .Then("hash should be zero", hash => hash == "0000000000000000")
            .AssertPassed();

    [Scenario("Computing hash of same content twice produces same result")]
    [Fact]
    public Task ComputeHash_is_deterministic()
        => Given("a schema content string", () => "TYPE:MyConfig\nPROP:Name\nTYPE:string")
            .When("computing hash twice", content =>
            {
                var hash1 = SchemaHasher.ComputeHash(content);
                var hash2 = SchemaHasher.ComputeHash(content);
                return (hash1, hash2);
            })
            .Then("both hashes should match", result => result.hash1 == result.hash2)
            .And("hash should not be empty", result => !string.IsNullOrEmpty(result.hash1))
            .AssertPassed();

    [Scenario("Computing hash of different content produces different results")]
    [Fact]
    public Task ComputeHash_different_content_different_hash()
        => Given("two different schema contents", () =>
            {
                var content1 = "TYPE:ConfigA\nPROP:Name";
                var content2 = "TYPE:ConfigB\nPROP:Name";
                return (content1, content2);
            })
            .When("computing hashes", contents =>
            {
                var hash1 = SchemaHasher.ComputeHash(contents.content1);
                var hash2 = SchemaHasher.ComputeHash(contents.content2);
                return (hash1, hash2);
            })
            .Then("hashes should be different", result => result.hash1 != result.hash2)
            .AssertPassed();

    [Scenario("Hash is 16 hexadecimal characters")]
    [Fact]
    public Task ComputeHash_produces_16_hex_chars()
        => Given("a schema content", () => "TYPE:MyConfig")
            .When("computing hash", content => SchemaHasher.ComputeHash(content))
            .Then("hash length should be 16", hash => hash.Length == 16)
            .And("hash should be valid hex", hash => hash.All(c => "0123456789abcdef".Contains(c)))
            .AssertPassed();

    [Scenario("Computing unified hash from multiple hashes")]
    [Fact]
    public Task ComputeUnifiedHash_combines_multiple_hashes()
        => Given("multiple schema hashes", () => new[] { "abc123", "def456", "789ghi" })
            .When("computing unified hash", hashes => SchemaHasher.ComputeUnifiedHash(hashes))
            .Then("unified hash should not be empty", unifiedHash => !string.IsNullOrEmpty(unifiedHash))
            .And("unified hash length should be 16", unifiedHash => unifiedHash.Length == 16)
            .AssertPassed();

    [Scenario("Unified hash is order-independent")]
    [Fact]
    public Task ComputeUnifiedHash_is_order_independent()
        => Given("hashes in different orders", () =>
            {
                var hashes1 = new[] { "hash1", "hash2", "hash3" };
                var hashes2 = new[] { "hash3", "hash1", "hash2" };
                return (hashes1, hashes2);
            })
            .When("computing unified hashes", data =>
            {
                var unified1 = SchemaHasher.ComputeUnifiedHash(data.hashes1);
                var unified2 = SchemaHasher.ComputeUnifiedHash(data.hashes2);
                return (unified1, unified2);
            })
            .Then("unified hashes should match", result => result.unified1 == result.unified2)
            .AssertPassed();

    [Scenario("Normalizing schema definition produces deterministic output")]
    [Fact]
    public Task NormalizeSchema_is_deterministic()
        => Given("a schema definition", () => new SchemaDefinition
            {
                Types =
                [
                    new SchemaTypeInfo
                    {
                        TypeName = "TestConfig",
                        Namespace = "Test",
                        Properties =
                        [
                            new SchemaPropertyInfo { Name = "PropB", TypeName = "string", IsRequired = true },
                            new SchemaPropertyInfo { Name = "PropA", TypeName = "int", IsRequired = false }
                        ]
                    }
                ]
            })
            .When("normalizing twice", schema =>
            {
                var normalized1 = SchemaHasher.NormalizeSchema(schema);
                var normalized2 = SchemaHasher.NormalizeSchema(schema);
                return (normalized1, normalized2);
            })
            .Then("both normalizations should match", result => result.normalized1 == result.normalized2)
            .AssertPassed();

    [Scenario("Normalized schema sorts properties alphabetically")]
    [Fact]
    public Task NormalizeSchema_sorts_properties()
        => Given("a schema with unsorted properties", () => new SchemaDefinition
            {
                Types =
                [
                    new SchemaTypeInfo
                    {
                        TypeName = "TestConfig",
                        Namespace = "Test",
                        Properties =
                        [
                            new SchemaPropertyInfo { Name = "Zebra", TypeName = "string" },
                            new SchemaPropertyInfo { Name = "Apple", TypeName = "string" },
                            new SchemaPropertyInfo { Name = "Banana", TypeName = "string" }
                        ]
                    }
                ]
            })
            .When("normalizing", schema => SchemaHasher.NormalizeSchema(schema))
            .Then("Apple should appear before Banana", normalized => normalized.IndexOf("Apple") < normalized.IndexOf("Banana"))
            .And("Banana should appear before Zebra", normalized => normalized.IndexOf("Banana") < normalized.IndexOf("Zebra"))
            .AssertPassed();

    [Scenario("Normalized schema sorts types alphabetically")]
    [Fact]
    public Task NormalizeSchema_sorts_types()
        => Given("a schema with multiple types unsorted", () => new SchemaDefinition
            {
                Types =
                [
                    new SchemaTypeInfo { TypeName = "ZConfig", Namespace = "Test", Properties = [] },
                    new SchemaTypeInfo { TypeName = "AConfig", Namespace = "Test", Properties = [] },
                    new SchemaTypeInfo { TypeName = "MConfig", Namespace = "Test", Properties = [] }
                ]
            })
            .When("normalizing", schema => SchemaHasher.NormalizeSchema(schema))
            .Then("AConfig should appear before MConfig", normalized => normalized.IndexOf("AConfig") < normalized.IndexOf("MConfig"))
            .And("MConfig should appear before ZConfig", normalized => normalized.IndexOf("MConfig") < normalized.IndexOf("ZConfig"))
            .AssertPassed();

    [Scenario("Normalized schema includes all property metadata")]
    [Fact]
    public Task NormalizeSchema_includes_all_metadata()
        => Given("a schema with property metadata", () => new SchemaDefinition
            {
                Types =
                [
                    new SchemaTypeInfo
                    {
                        TypeName = "TestConfig",
                        Namespace = "Test.Namespace",
                        Properties =
                        [
                            new SchemaPropertyInfo
                            {
                                Name = "TestProp",
                                TypeName = "string",
                                IsRequired = true,
                                IsNullable = false
                            }
                        ]
                    }
                ]
            })
            .When("normalizing", schema => SchemaHasher.NormalizeSchema(schema))
            .Then("contains type name", normalized => normalized.Contains("TYPE:TestConfig"))
            .And("contains namespace", normalized => normalized.Contains("NAMESPACE:Test.Namespace"))
            .And("contains property name", normalized => normalized.Contains("PROP:TestProp"))
            .And("contains property type", normalized => normalized.Contains("TYPE:string"))
            .And("contains required flag", normalized => normalized.Contains("REQUIRED:True"))
            .And("contains nullable flag", normalized => normalized.Contains("NULLABLE:False"))
            .AssertPassed();
}
