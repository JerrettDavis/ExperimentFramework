using ExperimentFramework.Targeting;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Targeting;

[Feature("Targeting rules evaluate contexts for matches")]
public sealed class TargetingRulesTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Always rule matches any context")]
    [Fact]
    public Task Always_rule_matches()
        => Given("an Always rule", TargetingRules.Always)
            .When("evaluating with any context", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("Never rule never matches")]
    [Fact]
    public Task Never_rule_never_matches()
        => Given("a Never rule", TargetingRules.Never)
            .When("evaluating with any context", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("Users rule matches specified user IDs")]
    [Fact]
    public Task Users_rule_matches_specified_ids()
        => Given("a Users rule for specific IDs", () => TargetingRules.Users("user-1", "user-2"))
            .When("evaluating with matching user", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("Users rule does not match unspecified user IDs")]
    [Fact]
    public Task Users_rule_does_not_match_other_ids()
        => Given("a Users rule for specific IDs", () => TargetingRules.Users("user-1", "user-2"))
            .When("evaluating with non-matching user", rule => rule.Evaluate(CreateContext("user-3")))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("Users rule is case-insensitive")]
    [Fact]
    public Task Users_rule_is_case_insensitive()
        => Given("a Users rule", () => TargetingRules.Users("User-1"))
            .When("evaluating with different case", rule => rule.Evaluate(CreateContext("USER-1")))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("AttributeEquals matches when attribute equals value")]
    [Fact]
    public Task AttributeEquals_matches_equal_value()
        => Given("an AttributeEquals rule", () => TargetingRules.AttributeEquals("plan", "premium"))
            .When("evaluating with matching attribute", rule =>
                rule.Evaluate(CreateContext("user-1", ("plan", "premium"))))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("AttributeEquals does not match different value")]
    [Fact]
    public Task AttributeEquals_does_not_match_different_value()
        => Given("an AttributeEquals rule", () => TargetingRules.AttributeEquals("plan", "premium"))
            .When("evaluating with different attribute value", rule =>
                rule.Evaluate(CreateContext("user-1", ("plan", "free"))))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("AttributeEquals does not match missing attribute")]
    [Fact]
    public Task AttributeEquals_does_not_match_missing_attribute()
        => Given("an AttributeEquals rule", () => TargetingRules.AttributeEquals("plan", "premium"))
            .When("evaluating without the attribute", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("AttributeIn matches when attribute is in set")]
    [Fact]
    public Task AttributeIn_matches_value_in_set()
        => Given("an AttributeIn rule", () => TargetingRules.AttributeIn("plan", "premium", "enterprise"))
            .When("evaluating with matching attribute", rule =>
                rule.Evaluate(CreateContext("user-1", ("plan", "enterprise"))))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("AttributeIn does not match value outside set")]
    [Fact]
    public Task AttributeIn_does_not_match_value_outside_set()
        => Given("an AttributeIn rule", () => TargetingRules.AttributeIn("plan", "premium", "enterprise"))
            .When("evaluating with non-matching attribute", rule =>
                rule.Evaluate(CreateContext("user-1", ("plan", "free"))))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("HasAttribute matches when attribute exists")]
    [Fact]
    public Task HasAttribute_matches_when_exists()
        => Given("a HasAttribute rule", () => TargetingRules.HasAttribute("beta"))
            .When("evaluating with the attribute", rule =>
                rule.Evaluate(CreateContext("user-1", ("beta", true))))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("HasAttribute does not match when attribute missing")]
    [Fact]
    public Task HasAttribute_does_not_match_when_missing()
        => Given("a HasAttribute rule", () => TargetingRules.HasAttribute("beta"))
            .When("evaluating without the attribute", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("All rule matches when all sub-rules match")]
    [Fact]
    public Task All_rule_matches_when_all_match()
        => Given("an All rule", () => TargetingRules.All(
                TargetingRules.HasAttribute("beta"),
                TargetingRules.AttributeEquals("plan", "premium")))
            .When("evaluating with matching context", rule =>
                rule.Evaluate(CreateContext("user-1", ("beta", true), ("plan", "premium"))))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("All rule does not match when any sub-rule fails")]
    [Fact]
    public Task All_rule_does_not_match_when_any_fails()
        => Given("an All rule", () => TargetingRules.All(
                TargetingRules.HasAttribute("beta"),
                TargetingRules.AttributeEquals("plan", "premium")))
            .When("evaluating with partially matching context", rule =>
                rule.Evaluate(CreateContext("user-1", ("beta", true), ("plan", "free"))))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("Any rule matches when at least one sub-rule matches")]
    [Fact]
    public Task Any_rule_matches_when_one_matches()
        => Given("an Any rule", () => TargetingRules.Any(
                TargetingRules.HasAttribute("beta"),
                TargetingRules.AttributeEquals("plan", "premium")))
            .When("evaluating with one matching rule", rule =>
                rule.Evaluate(CreateContext("user-1", ("plan", "premium"))))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("Any rule does not match when no sub-rules match")]
    [Fact]
    public Task Any_rule_does_not_match_when_none_match()
        => Given("an Any rule", () => TargetingRules.Any(
                TargetingRules.HasAttribute("beta"),
                TargetingRules.AttributeEquals("plan", "premium")))
            .When("evaluating with no matching rules", rule =>
                rule.Evaluate(CreateContext("user-1", ("plan", "free"))))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("Not rule inverts the result")]
    [Fact]
    public Task Not_rule_inverts_result()
        => Given("a Not rule", () => TargetingRules.Not(TargetingRules.HasAttribute("blocked")))
            .When("evaluating without the attribute", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("Percentage rule provides consistent allocation")]
    [Fact]
    public Task Percentage_rule_is_consistent()
        => Given("a Percentage rule at 50%", () => TargetingRules.Percentage(50, "test-seed"))
            .When("evaluating the same user multiple times", rule =>
            {
                var context = CreateContext("user-123");
                return Enumerable.Range(0, 10)
                    .Select(_ => rule.Evaluate(context))
                    .ToList();
            })
            .Then("all results are identical", results => results.Distinct().Count() == 1)
            .AssertPassed();

    [Scenario("Percentage rule with 0% matches no users")]
    [Fact]
    public Task Percentage_rule_zero_matches_none()
        => Given("a Percentage rule at 0%", () => TargetingRules.Percentage(0, "test-seed"))
            .When("evaluating 100 users", rule =>
                Enumerable.Range(1, 100)
                    .Count(i => rule.Evaluate(CreateContext($"user-{i}"))))
            .Then("no users match", count => count == 0)
            .AssertPassed();

    [Scenario("Percentage rule with 100% matches all users")]
    [Fact]
    public Task Percentage_rule_hundred_matches_all()
        => Given("a Percentage rule at 100%", () => TargetingRules.Percentage(100, "test-seed"))
            .When("evaluating 100 users", rule =>
                Enumerable.Range(1, 100)
                    .Count(i => rule.Evaluate(CreateContext($"user-{i}"))))
            .Then("all users match", count => count == 100)
            .AssertPassed();

    [Scenario("Percentage rule does not match null user")]
    [Fact]
    public Task Percentage_rule_does_not_match_null_user()
        => Given("a Percentage rule at 100%", () => TargetingRules.Percentage(100, "test-seed"))
            .When("evaluating context with null user", rule =>
                rule.Evaluate(new SimpleTargetingContext(null)))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("Percentage rule uses default seed when null")]
    [Fact]
    public Task Percentage_rule_uses_default_seed()
        => Given("a Percentage rule without seed", () => TargetingRules.Percentage(50))
            .When("evaluating the same user multiple times", rule =>
            {
                var context = CreateContext("test-user-42");
                return Enumerable.Range(0, 10)
                    .Select(_ => rule.Evaluate(context))
                    .ToList();
            })
            .Then("all results are consistent", results => results.Distinct().Count() == 1)
            .AssertPassed();

    [Scenario("Users rule does not match null user")]
    [Fact]
    public Task Users_rule_does_not_match_null_user()
        => Given("a Users rule", () => TargetingRules.Users("user-1"))
            .When("evaluating context with null user", rule =>
                rule.Evaluate(new SimpleTargetingContext(null)))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("AttributeIn does not match missing attribute")]
    [Fact]
    public Task AttributeIn_does_not_match_missing_attribute()
        => Given("an AttributeIn rule", () => TargetingRules.AttributeIn("plan", "premium", "enterprise"))
            .When("evaluating without the attribute", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("Not rule inverts matching result")]
    [Fact]
    public Task Not_rule_inverts_matching_result()
        => Given("a Not rule with HasAttribute", () => TargetingRules.Not(TargetingRules.HasAttribute("blocked")))
            .When("evaluating with the attribute", rule =>
                rule.Evaluate(CreateContext("user-1", ("blocked", true))))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("All rule with empty rules always matches")]
    [Fact]
    public Task All_rule_empty_matches()
        => Given("an All rule with no sub-rules", () => TargetingRules.All())
            .When("evaluating with any context", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("Any rule with empty rules never matches")]
    [Fact]
    public Task Any_rule_empty_never_matches()
        => Given("an Any rule with no sub-rules", () => TargetingRules.Any())
            .When("evaluating with any context", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("All rule with single matching rule")]
    [Fact]
    public Task All_rule_single_matching()
        => Given("an All rule with one matching sub-rule", () => TargetingRules.All(TargetingRules.Always()))
            .When("evaluating", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("Any rule with single matching rule")]
    [Fact]
    public Task Any_rule_single_matching()
        => Given("an Any rule with one matching sub-rule", () => TargetingRules.Any(TargetingRules.Always()))
            .When("evaluating", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("All rule short-circuits on first failure")]
    [Fact]
    public Task All_rule_short_circuits()
        => Given("an All rule with Never followed by Always", () =>
                TargetingRules.All(TargetingRules.Never(), TargetingRules.Always()))
            .When("evaluating", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it does not match", result => !result)
            .AssertPassed();

    [Scenario("Any rule short-circuits on first success")]
    [Fact]
    public Task Any_rule_short_circuits()
        => Given("an Any rule with Always followed by Never", () =>
                TargetingRules.Any(TargetingRules.Always(), TargetingRules.Never()))
            .When("evaluating", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("AttributeEquals with integer value")]
    [Fact]
    public Task AttributeEquals_with_integer()
        => Given("an AttributeEquals rule with integer", () => TargetingRules.AttributeEquals("tier", 3))
            .When("evaluating with matching integer", rule =>
                rule.Evaluate(CreateContext("user-1", ("tier", 3))))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("AttributeIn with integer values")]
    [Fact]
    public Task AttributeIn_with_integers()
        => Given("an AttributeIn rule with integers", () => TargetingRules.AttributeIn("tier", 1, 2, 3))
            .When("evaluating with matching integer", rule =>
                rule.Evaluate(CreateContext("user-1", ("tier", 2))))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("Users rule with multiple matches")]
    [Fact]
    public Task Users_rule_matches_second_user()
        => Given("a Users rule for multiple IDs", () => TargetingRules.Users("user-1", "user-2", "user-3"))
            .When("evaluating with second user", rule => rule.Evaluate(CreateContext("user-2")))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("Nested All and Any rules")]
    [Fact]
    public Task Nested_all_and_any_rules()
        => Given("a nested rule structure", () =>
                TargetingRules.All(
                    TargetingRules.HasAttribute("active"),
                    TargetingRules.Any(
                        TargetingRules.AttributeEquals("plan", "premium"),
                        TargetingRules.AttributeEquals("plan", "enterprise"))))
            .When("evaluating with active premium user", rule =>
                rule.Evaluate(CreateContext("user-1", ("active", true), ("plan", "premium"))))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("Nested Not and All rules")]
    [Fact]
    public Task Nested_not_and_all_rules()
        => Given("a Not(All) rule structure", () =>
                TargetingRules.Not(TargetingRules.All(
                    TargetingRules.HasAttribute("blocked"),
                    TargetingRules.AttributeEquals("reason", "spam"))))
            .When("evaluating user without blocked attribute", rule =>
                rule.Evaluate(CreateContext("user-1")))
            .Then("it matches", result => result)
            .AssertPassed();

    [Scenario("Double Not rule")]
    [Fact]
    public Task Double_not_rule()
        => Given("a Not(Not) rule", () =>
                TargetingRules.Not(TargetingRules.Not(TargetingRules.Always())))
            .When("evaluating", rule => rule.Evaluate(CreateContext("user-1")))
            .Then("it matches (double negation)", result => result)
            .AssertPassed();

    private static ITargetingContext CreateContext(string? userId, params (string Key, object Value)[] attributes)
    {
        var context = new SimpleTargetingContext(userId);
        foreach (var (key, value) in attributes)
        {
            context.WithAttribute(key, value);
        }
        return context;
    }
}
