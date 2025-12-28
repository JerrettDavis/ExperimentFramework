using ExperimentFramework.Targeting;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Targeting;

[Feature("SimpleTargetingContext provides dictionary-based context storage")]
public sealed class SimpleTargetingContextTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Context with user ID")]
    [Fact]
    public Task Context_with_user_id()
        => Given("a context with user ID", () => new SimpleTargetingContext("user-123"))
            .Then("user ID is set", ctx => ctx.UserId == "user-123")
            .AssertPassed();

    [Scenario("Context without user ID")]
    [Fact]
    public Task Context_without_user_id()
        => Given("a context without user ID", () => new SimpleTargetingContext())
            .Then("user ID is null", ctx => ctx.UserId == null)
            .AssertPassed();

    [Scenario("WithAttribute adds attribute")]
    [Fact]
    public Task WithAttribute_adds_attribute()
        => Given("a context", () => new SimpleTargetingContext("user-1"))
            .When("adding an attribute", ctx => ctx.WithAttribute("plan", "premium"))
            .Then("attribute is accessible", ctx => ctx.GetAttribute("plan")?.ToString() == "premium")
            .AssertPassed();

    [Scenario("WithAttribute returns same context for chaining")]
    [Fact]
    public Task WithAttribute_returns_same_context()
        => Given("a context", () => new SimpleTargetingContext("user-1"))
            .When("chaining WithAttribute calls", ctx =>
                ctx.WithAttribute("a", 1)
                   .WithAttribute("b", 2)
                   .WithAttribute("c", 3))
            .Then("all attributes are set", ctx =>
                (int?)ctx.GetAttribute("a") == 1 &&
                (int?)ctx.GetAttribute("b") == 2 &&
                (int?)ctx.GetAttribute("c") == 3)
            .AssertPassed();

    [Scenario("GetAttribute returns null for missing attribute")]
    [Fact]
    public Task GetAttribute_returns_null_for_missing()
        => Given("a context", () => new SimpleTargetingContext("user-1"))
            .When("getting a missing attribute", ctx => ctx.GetAttribute("nonexistent"))
            .Then("returns null", result => result == null)
            .AssertPassed();

    [Scenario("HasAttribute returns true when attribute exists")]
    [Fact]
    public Task HasAttribute_returns_true_when_exists()
        => Given("a context with an attribute", () =>
                new SimpleTargetingContext("user-1").WithAttribute("beta", true))
            .When("checking if attribute exists", ctx => ctx.HasAttribute("beta"))
            .Then("returns true", result => result)
            .AssertPassed();

    [Scenario("HasAttribute returns false when attribute missing")]
    [Fact]
    public Task HasAttribute_returns_false_when_missing()
        => Given("a context without attributes", () => new SimpleTargetingContext("user-1"))
            .When("checking for missing attribute", ctx => ctx.HasAttribute("beta"))
            .Then("returns false", result => !result)
            .AssertPassed();

    [Scenario("AttributeNames returns all attribute names")]
    [Fact]
    public Task AttributeNames_returns_all_names()
        => Given("a context with multiple attributes", () =>
                new SimpleTargetingContext("user-1")
                    .WithAttribute("plan", "premium")
                    .WithAttribute("region", "us-east")
                    .WithAttribute("beta", true))
            .When("getting attribute names", ctx => ctx.AttributeNames.ToList())
            .Then("contains all attribute names", names =>
                names.Count == 3 &&
                names.Contains("plan") &&
                names.Contains("region") &&
                names.Contains("beta"))
            .AssertPassed();

    [Scenario("AttributeNames is empty for new context")]
    [Fact]
    public Task AttributeNames_empty_for_new_context()
        => Given("a new context", () => new SimpleTargetingContext("user-1"))
            .Then("attribute names is empty", ctx => !ctx.AttributeNames.Any())
            .AssertPassed();

    [Scenario("Attribute names are case-insensitive")]
    [Fact]
    public Task Attribute_names_case_insensitive()
        => Given("a context with an attribute", () =>
                new SimpleTargetingContext("user-1").WithAttribute("Plan", "premium"))
            .Then("can get with different case", ctx => ctx.GetAttribute("plan")?.ToString() == "premium")
            .And("has attribute with different case", ctx => ctx.HasAttribute("PLAN"))
            .AssertPassed();

    [Scenario("WithAttribute overwrites existing value")]
    [Fact]
    public Task WithAttribute_overwrites_existing()
        => Given("a context with an attribute", () =>
                new SimpleTargetingContext("user-1").WithAttribute("plan", "free"))
            .When("overwriting the attribute", ctx => ctx.WithAttribute("plan", "premium"))
            .Then("returns new value", ctx => ctx.GetAttribute("plan")?.ToString() == "premium")
            .AssertPassed();

    [Scenario("Supports various value types")]
    [Fact]
    public Task Supports_various_value_types()
        => Given("a context with various types", () =>
                new SimpleTargetingContext("user-1")
                    .WithAttribute("string", "value")
                    .WithAttribute("int", 42)
                    .WithAttribute("bool", true)
                    .WithAttribute("double", 3.14)
                    .WithAttribute("list", new List<string> { "a", "b" }))
            .Then("string value is accessible", ctx => ctx.GetAttribute("string")?.ToString() == "value")
            .And("int value is accessible", ctx => (int?)ctx.GetAttribute("int") == 42)
            .And("bool value is accessible", ctx => (bool?)ctx.GetAttribute("bool") == true)
            .And("double value is accessible", ctx => Math.Abs((double)ctx.GetAttribute("double")! - 3.14) < 0.001)
            .And("list value is accessible", ctx => ((List<string>)ctx.GetAttribute("list")!).Count == 2)
            .AssertPassed();
}
