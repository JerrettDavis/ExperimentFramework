using ExperimentFramework.Targeting;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Targeting;

[Feature("TargetingOptions configures targeting behavior")]
public sealed class TargetingOptionsTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Default MatchedKey is 'true'")]
    [Fact]
    public Task Default_matched_key_is_true()
        => Given("a new TargetingOptions", () => new TargetingOptions())
            .Then("MatchedKey is 'true'", opts => opts.MatchedKey == "true")
            .AssertPassed();

    [Scenario("Default UnmatchedKey is null")]
    [Fact]
    public Task Default_unmatched_key_is_null()
        => Given("a new TargetingOptions", () => new TargetingOptions())
            .Then("UnmatchedKey is null", opts => opts.UnmatchedKey == null)
            .AssertPassed();

    [Scenario("Default DefaultRule is null")]
    [Fact]
    public Task Default_rule_is_null()
        => Given("a new TargetingOptions", () => new TargetingOptions())
            .Then("DefaultRule is null", opts => opts.DefaultRule == null)
            .AssertPassed();

    [Scenario("Can set MatchedKey")]
    [Fact]
    public Task Can_set_matched_key()
        => Given("a new TargetingOptions", () => new TargetingOptions())
            .When("setting MatchedKey", opts =>
            {
                opts.MatchedKey = "enabled";
                return opts;
            })
            .Then("MatchedKey is set", opts => opts.MatchedKey == "enabled")
            .AssertPassed();

    [Scenario("Can set UnmatchedKey")]
    [Fact]
    public Task Can_set_unmatched_key()
        => Given("a new TargetingOptions", () => new TargetingOptions())
            .When("setting UnmatchedKey", opts =>
            {
                opts.UnmatchedKey = "disabled";
                return opts;
            })
            .Then("UnmatchedKey is set", opts => opts.UnmatchedKey == "disabled")
            .AssertPassed();

    [Scenario("Can set DefaultRule")]
    [Fact]
    public Task Can_set_default_rule()
        => Given("a new TargetingOptions", () => new TargetingOptions())
            .When("setting DefaultRule", opts =>
            {
                opts.DefaultRule = TargetingRules.Always();
                return opts;
            })
            .Then("DefaultRule is set", opts => opts.DefaultRule != null)
            .AssertPassed();

    [Scenario("Can configure all options together")]
    [Fact]
    public Task Can_configure_all_options()
        => Given("a configured TargetingOptions", () => new TargetingOptions
            {
                DefaultRule = TargetingRules.HasAttribute("beta"),
                MatchedKey = "beta-variant",
                UnmatchedKey = "control-variant"
            })
            .Then("all values are set correctly", opts =>
                opts.DefaultRule != null &&
                opts.MatchedKey == "beta-variant" &&
                opts.UnmatchedKey == "control-variant")
            .AssertPassed();
}

[Feature("InMemoryTargetingConfiguration stores selector rules")]
public sealed class InMemoryTargetingConfigurationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("GetRulesFor returns null for unknown selector")]
    [Fact]
    public Task Returns_null_for_unknown_selector()
        => Given("an empty configuration", () => new InMemoryTargetingConfiguration())
            .When("getting rules for unknown selector", cfg => cfg.GetRulesFor("unknown-selector"))
            .Then("returns null", result => result == null)
            .AssertPassed();

    [Scenario("AddRule stores rules for selector")]
    [Fact]
    public Task AddRule_stores_rules()
        => Given("a configuration", () => new InMemoryTargetingConfiguration())
            .When("adding a rule", cfg =>
            {
                cfg.AddRule("my-selector", TargetingRules.Always(), "matched-key");
                return cfg.GetRulesFor("my-selector");
            })
            .Then("rule is stored", rules => rules != null && rules.Count == 1)
            .AssertPassed();

    [Scenario("AddRule returns configuration for chaining")]
    [Fact]
    public Task AddRule_returns_for_chaining()
        => Given("a configuration", () => new InMemoryTargetingConfiguration())
            .When("chaining AddRule calls", cfg =>
                cfg.AddRule("selector-1", TargetingRules.Always(), "key-1")
                   .AddRule("selector-2", TargetingRules.Never(), "key-2")
                   .AddRule("selector-1", TargetingRules.Never(), "key-3"))
            .Then("all rules are stored", cfg =>
                cfg.GetRulesFor("selector-1")?.Count == 2 &&
                cfg.GetRulesFor("selector-2")?.Count == 1)
            .AssertPassed();

    [Scenario("Multiple rules for same selector are evaluated in order")]
    [Fact]
    public Task Multiple_rules_are_ordered()
        => Given("a configuration with multiple rules for one selector", () =>
            {
                var cfg = new InMemoryTargetingConfiguration();
                cfg.AddRule("test", TargetingRules.HasAttribute("premium"), "premium-key");
                cfg.AddRule("test", TargetingRules.HasAttribute("standard"), "standard-key");
                cfg.AddRule("test", TargetingRules.Always(), "default-key");
                return cfg;
            })
            .When("getting rules", cfg => cfg.GetRulesFor("test"))
            .Then("rules are in order", rules =>
                rules != null &&
                rules.Count == 3 &&
                rules[0].Key == "premium-key" &&
                rules[1].Key == "standard-key" &&
                rules[2].Key == "default-key")
            .AssertPassed();

    [Scenario("Selector names are case-insensitive")]
    [Fact]
    public Task Selector_names_case_insensitive()
        => Given("a configuration with a rule", () =>
            {
                var cfg = new InMemoryTargetingConfiguration();
                cfg.AddRule("MySelector", TargetingRules.Always(), "key");
                return cfg;
            })
            .When("getting rules with different case", cfg => cfg.GetRulesFor("myselector"))
            .Then("rules are found", rules => rules != null && rules.Count == 1)
            .AssertPassed();

    [Scenario("Can retrieve rules with original key")]
    [Fact]
    public Task Can_retrieve_with_mixed_case()
        => Given("a configuration with lowercase selector", () =>
            {
                var cfg = new InMemoryTargetingConfiguration();
                cfg.AddRule("lowercase", TargetingRules.Never(), "result");
                return cfg;
            })
            .When("getting rules with uppercase", cfg => cfg.GetRulesFor("LOWERCASE"))
            .Then("rules are found", rules => rules != null && rules.Count == 1 && rules[0].Key == "result")
            .AssertPassed();

    [Scenario("Rule tuple contains correct rule and key")]
    [Fact]
    public Task Rule_tuple_has_correct_values()
        => Given("a configuration with a specific rule", () =>
            {
                var cfg = new InMemoryTargetingConfiguration();
                var rule = TargetingRules.Users("user-1");
                cfg.AddRule("test-selector", rule, "specific-key");
                return cfg;
            })
            .When("getting the rule", cfg => cfg.GetRulesFor("test-selector"))
            .Then("tuple has correct key", rules => rules![0].Key == "specific-key")
            .And("tuple has a rule", rules => rules![0].Rule != null)
            .AssertPassed();
}
