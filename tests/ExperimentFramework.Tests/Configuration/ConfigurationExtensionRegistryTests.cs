using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using Microsoft.Extensions.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration;

[Feature("ConfigurationExtensionRegistry manages decorator and selection mode handlers")]
public sealed class ConfigurationExtensionRegistryTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("RegisterDecoratorHandler registers handler successfully")]
    [Fact]
    public Task RegisterDecoratorHandler_registers_handler()
        => Given("a registry", () => new ConfigurationExtensionRegistry())
            .When("registering a decorator handler", registry =>
            {
                var handler = new TestDecoratorHandler("test");
                return (Registry: registry, Success: registry.RegisterDecoratorHandler(handler));
            })
            .Then("registration succeeds", result => result.Success)
            .And("handler can be retrieved", result =>
                result.Registry.GetDecoratorHandler("test") != null)
            .AssertPassed();

    [Scenario("RegisterDecoratorHandler returns false for duplicate")]
    [Fact]
    public Task RegisterDecoratorHandler_returns_false_for_duplicate()
        => Given("a registry with a handler", () =>
            {
                var registry = new ConfigurationExtensionRegistry();
                registry.RegisterDecoratorHandler(new TestDecoratorHandler("test"));
                return registry;
            })
            .When("registering a duplicate handler", registry =>
                registry.RegisterDecoratorHandler(new TestDecoratorHandler("test")))
            .Then("registration fails", success => !success)
            .AssertPassed();

    [Scenario("RegisterDecoratorHandler throws for null handler")]
    [Fact]
    public Task RegisterDecoratorHandler_throws_for_null()
        => Given("a registry", () => new ConfigurationExtensionRegistry())
            .When("registering null handler", registry =>
            {
                var exception = Assert.Throws<ArgumentNullException>(() =>
                    registry.RegisterDecoratorHandler(null!));
                return exception;
            })
            .Then("throws ArgumentNullException", ex => ex.ParamName == "handler")
            .AssertPassed();

    [Scenario("RegisterSelectionModeHandler registers handler successfully")]
    [Fact]
    public Task RegisterSelectionModeHandler_registers_handler()
        => Given("a registry", () => new ConfigurationExtensionRegistry())
            .When("registering a selection mode handler", registry =>
            {
                var handler = new TestSelectionModeHandler("test");
                return (Registry: registry, Success: registry.RegisterSelectionModeHandler(handler));
            })
            .Then("registration succeeds", result => result.Success)
            .And("handler can be retrieved", result =>
                result.Registry.GetSelectionModeHandler("test") != null)
            .AssertPassed();

    [Scenario("RegisterSelectionModeHandler returns false for duplicate")]
    [Fact]
    public Task RegisterSelectionModeHandler_returns_false_for_duplicate()
        => Given("a registry with a handler", () =>
            {
                var registry = new ConfigurationExtensionRegistry();
                registry.RegisterSelectionModeHandler(new TestSelectionModeHandler("test"));
                return registry;
            })
            .When("registering a duplicate handler", registry =>
                registry.RegisterSelectionModeHandler(new TestSelectionModeHandler("test")))
            .Then("registration fails", success => !success)
            .AssertPassed();

    [Scenario("RegisterSelectionModeHandler throws for null handler")]
    [Fact]
    public Task RegisterSelectionModeHandler_throws_for_null()
        => Given("a registry", () => new ConfigurationExtensionRegistry())
            .When("registering null handler", registry =>
            {
                var exception = Assert.Throws<ArgumentNullException>(() =>
                    registry.RegisterSelectionModeHandler(null!));
                return exception;
            })
            .Then("throws ArgumentNullException", ex => ex.ParamName == "handler")
            .AssertPassed();

    [Scenario("GetDecoratorHandler returns null for unregistered type")]
    [Fact]
    public Task GetDecoratorHandler_returns_null_for_unregistered()
        => Given("an empty registry", () => new ConfigurationExtensionRegistry())
            .When("getting unregistered handler", registry => registry.GetDecoratorHandler("unknown"))
            .Then("returns null", handler => handler == null)
            .AssertPassed();

    [Scenario("GetSelectionModeHandler returns null for unregistered type")]
    [Fact]
    public Task GetSelectionModeHandler_returns_null_for_unregistered()
        => Given("an empty registry", () => new ConfigurationExtensionRegistry())
            .When("getting unregistered handler", registry => registry.GetSelectionModeHandler("unknown"))
            .Then("returns null", handler => handler == null)
            .AssertPassed();

    [Scenario("HasDecoratorHandler returns true for registered handler")]
    [Fact]
    public Task HasDecoratorHandler_returns_true_for_registered()
        => Given("a registry with a handler", () =>
            {
                var registry = new ConfigurationExtensionRegistry();
                registry.RegisterDecoratorHandler(new TestDecoratorHandler("test"));
                return registry;
            })
            .When("checking for handler", registry => registry.HasDecoratorHandler("test"))
            .Then("returns true", exists => exists)
            .AssertPassed();

    [Scenario("HasDecoratorHandler returns false for unregistered handler")]
    [Fact]
    public Task HasDecoratorHandler_returns_false_for_unregistered()
        => Given("an empty registry", () => new ConfigurationExtensionRegistry())
            .When("checking for handler", registry => registry.HasDecoratorHandler("unknown"))
            .Then("returns false", exists => !exists)
            .AssertPassed();

    [Scenario("HasSelectionModeHandler returns true for registered handler")]
    [Fact]
    public Task HasSelectionModeHandler_returns_true_for_registered()
        => Given("a registry with a handler", () =>
            {
                var registry = new ConfigurationExtensionRegistry();
                registry.RegisterSelectionModeHandler(new TestSelectionModeHandler("test"));
                return registry;
            })
            .When("checking for handler", registry => registry.HasSelectionModeHandler("test"))
            .Then("returns true", exists => exists)
            .AssertPassed();

    [Scenario("HasSelectionModeHandler returns false for unregistered handler")]
    [Fact]
    public Task HasSelectionModeHandler_returns_false_for_unregistered()
        => Given("an empty registry", () => new ConfigurationExtensionRegistry())
            .When("checking for handler", registry => registry.HasSelectionModeHandler("unknown"))
            .Then("returns false", exists => !exists)
            .AssertPassed();

    [Scenario("GetRegisteredDecoratorTypes returns all registered types")]
    [Fact]
    public Task GetRegisteredDecoratorTypes_returns_all_types()
        => Given("a registry with multiple handlers", () =>
            {
                var registry = new ConfigurationExtensionRegistry();
                registry.RegisterDecoratorHandler(new TestDecoratorHandler("type1"));
                registry.RegisterDecoratorHandler(new TestDecoratorHandler("type2"));
                return registry;
            })
            .When("getting registered types", registry => registry.GetRegisteredDecoratorTypes().ToList())
            .Then("returns all types", types => types.Count == 2)
            .And("contains type1", types => types.Contains("type1"))
            .And("contains type2", types => types.Contains("type2"))
            .AssertPassed();

    [Scenario("GetRegisteredSelectionModeTypes returns all registered types")]
    [Fact]
    public Task GetRegisteredSelectionModeTypes_returns_all_types()
        => Given("a registry with multiple handlers", () =>
            {
                var registry = new ConfigurationExtensionRegistry();
                registry.RegisterSelectionModeHandler(new TestSelectionModeHandler("mode1"));
                registry.RegisterSelectionModeHandler(new TestSelectionModeHandler("mode2"));
                return registry;
            })
            .When("getting registered types", registry => registry.GetRegisteredSelectionModeTypes().ToList())
            .Then("returns all types", types => types.Count == 2)
            .And("contains mode1", types => types.Contains("mode1"))
            .And("contains mode2", types => types.Contains("mode2"))
            .AssertPassed();

    [Scenario("GetDecoratorHandlers returns all registered handlers")]
    [Fact]
    public Task GetDecoratorHandlers_returns_all_handlers()
        => Given("a registry with multiple handlers", () =>
            {
                var registry = new ConfigurationExtensionRegistry();
                registry.RegisterDecoratorHandler(new TestDecoratorHandler("type1"));
                registry.RegisterDecoratorHandler(new TestDecoratorHandler("type2"));
                return registry;
            })
            .When("getting handlers", registry => registry.GetDecoratorHandlers().ToList())
            .Then("returns all handlers", handlers => handlers.Count == 2)
            .AssertPassed();

    [Scenario("GetSelectionModeHandlers returns all registered handlers")]
    [Fact]
    public Task GetSelectionModeHandlers_returns_all_handlers()
        => Given("a registry with multiple handlers", () =>
            {
                var registry = new ConfigurationExtensionRegistry();
                registry.RegisterSelectionModeHandler(new TestSelectionModeHandler("mode1"));
                registry.RegisterSelectionModeHandler(new TestSelectionModeHandler("mode2"));
                return registry;
            })
            .When("getting handlers", registry => registry.GetSelectionModeHandlers().ToList())
            .Then("returns all handlers", handlers => handlers.Count == 2)
            .AssertPassed();

    [Scenario("Registry is case insensitive for decorator types")]
    [Fact]
    public Task Registry_is_case_insensitive_for_decorators()
        => Given("a registry with a handler", () =>
            {
                var registry = new ConfigurationExtensionRegistry();
                registry.RegisterDecoratorHandler(new TestDecoratorHandler("Test"));
                return registry;
            })
            .When("getting handler with different case", registry => registry.GetDecoratorHandler("TEST"))
            .Then("returns the handler", handler => handler != null)
            .AssertPassed();

    [Scenario("Registry is case insensitive for selection modes")]
    [Fact]
    public Task Registry_is_case_insensitive_for_selection_modes()
        => Given("a registry with a handler", () =>
            {
                var registry = new ConfigurationExtensionRegistry();
                registry.RegisterSelectionModeHandler(new TestSelectionModeHandler("Test"));
                return registry;
            })
            .When("getting handler with different case", registry => registry.GetSelectionModeHandler("TEST"))
            .Then("returns the handler", handler => handler != null)
            .AssertPassed();

    private sealed class TestDecoratorHandler : IConfigurationDecoratorHandler
    {
        public TestDecoratorHandler(string decoratorType)
        {
            DecoratorType = decoratorType;
        }

        public string DecoratorType { get; }

        public void Apply(ExperimentFrameworkBuilder builder, DecoratorConfig config, ILogger? logger) { }

        public IEnumerable<ConfigurationValidationError> Validate(DecoratorConfig config, string path)
        {
            yield break;
        }
    }

    private sealed class TestSelectionModeHandler : IConfigurationSelectionModeHandler
    {
        public TestSelectionModeHandler(string modeType)
        {
            ModeType = modeType;
        }

        public string ModeType { get; }

        public void Apply<TService>(ServiceExperimentBuilder<TService> builder, SelectionModeConfig config, ILogger? logger)
            where TService : class
        { }

        public IEnumerable<ConfigurationValidationError> Validate(SelectionModeConfig config, string path)
        {
            yield break;
        }
    }
}
