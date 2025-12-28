using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Extensions;
using ExperimentFramework.Configuration.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration;

[Feature("Built-in configuration handlers work correctly")]
public sealed class BuiltInHandlersTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    #region TimeoutDecoratorHandler Tests

    [Scenario("Timeout handler validates invalid timeout format")]
    [Fact]
    public async Task TimeoutHandler_validates_invalid_timeout_format()
    {
        var registry = CreateRegistryWithHandlers();
        var handler = registry.GetDecoratorHandler("timeout")!;

        var config = new DecoratorConfig
        {
            Type = "timeout",
            Options = new Dictionary<string, object>
            {
                ["timeout"] = "not-a-timespan"
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Single(errors);
        Assert.Contains("Invalid timeout format", errors[0].Message);

        await Task.CompletedTask;
    }

    [Scenario("Timeout handler validates unknown timeout action")]
    [Fact]
    public async Task TimeoutHandler_validates_unknown_timeout_action()
    {
        var registry = CreateRegistryWithHandlers();
        var handler = registry.GetDecoratorHandler("timeout")!;

        var config = new DecoratorConfig
        {
            Type = "timeout",
            Options = new Dictionary<string, object>
            {
                ["onTimeout"] = "unknownAction"
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Single(errors);
        Assert.Contains("Unknown timeout action", errors[0].Message);

        await Task.CompletedTask;
    }

    [Scenario("Timeout handler accepts valid timeout actions")]
    [Fact]
    public async Task TimeoutHandler_accepts_valid_timeout_actions()
    {
        var registry = CreateRegistryWithHandlers();
        var handler = registry.GetDecoratorHandler("timeout")!;

        var validActions = new[] { "throw", "throwException", "fallbackToDefault", "fallbackToSpecificTrial" };

        foreach (var action in validActions)
        {
            var config = new DecoratorConfig
            {
                Type = "timeout",
                Options = new Dictionary<string, object>
                {
                    ["onTimeout"] = action
                }
            };

            var errors = handler.Validate(config, "decorators[0]").ToList();
            Assert.Empty(errors);
        }

        await Task.CompletedTask;
    }

    [Scenario("Timeout handler accepts valid TimeSpan formats")]
    [Fact]
    public async Task TimeoutHandler_accepts_valid_timespan_formats()
    {
        var registry = CreateRegistryWithHandlers();
        var handler = registry.GetDecoratorHandler("timeout")!;

        var config = new DecoratorConfig
        {
            Type = "timeout",
            Options = new Dictionary<string, object>
            {
                ["timeout"] = "00:00:30"
            }
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();
        Assert.Empty(errors);

        await Task.CompletedTask;
    }

    [Scenario("Timeout handler Apply uses TimeSpan value from options")]
    [Fact]
    public async Task TimeoutHandler_Apply_uses_timespan_from_options()
    {
        var registry = CreateRegistryWithHandlers();
        var handler = registry.GetDecoratorHandler("timeout")!;
        var builder = ExperimentFrameworkBuilder.Create();

        var config = new DecoratorConfig
        {
            Type = "timeout",
            Options = new Dictionary<string, object>
            {
                ["timeout"] = TimeSpan.FromSeconds(45)
            }
        };

        // Should not throw
        handler.Apply(builder, config, null);

        await Task.CompletedTask;
    }

    [Scenario("Timeout handler Apply uses fallbackTrialKey option")]
    [Fact]
    public async Task TimeoutHandler_Apply_uses_fallback_trial_key()
    {
        var registry = CreateRegistryWithHandlers();
        var handler = registry.GetDecoratorHandler("timeout")!;
        var builder = ExperimentFrameworkBuilder.Create();

        var config = new DecoratorConfig
        {
            Type = "timeout",
            Options = new Dictionary<string, object>
            {
                ["onTimeout"] = "fallbackToSpecificTrial",
                ["fallbackTrialKey"] = "control"
            }
        };

        // Should not throw
        handler.Apply(builder, config, null);

        await Task.CompletedTask;
    }

    [Scenario("Timeout handler Apply handles all timeout actions")]
    [Fact]
    public async Task TimeoutHandler_Apply_handles_all_timeout_actions()
    {
        var registry = CreateRegistryWithHandlers();
        var handler = registry.GetDecoratorHandler("timeout")!;

        var actions = new[] { "throw", "throwException", "fallbackToDefault", "fallbackToSpecificTrial", "unknown" };

        foreach (var action in actions)
        {
            var builder = ExperimentFrameworkBuilder.Create();
            var config = new DecoratorConfig
            {
                Type = "timeout",
                Options = new Dictionary<string, object>
                {
                    ["onTimeout"] = action
                }
            };

            // Should not throw for any action
            handler.Apply(builder, config, null);
        }

        await Task.CompletedTask;
    }

    [Scenario("Timeout handler Apply handles non-string timeout value")]
    [Fact]
    public async Task TimeoutHandler_Apply_handles_non_string_timeout()
    {
        var registry = CreateRegistryWithHandlers();
        var handler = registry.GetDecoratorHandler("timeout")!;
        var builder = ExperimentFrameworkBuilder.Create();

        var config = new DecoratorConfig
        {
            Type = "timeout",
            Options = new Dictionary<string, object>
            {
                ["timeout"] = 12345 // Not a TimeSpan or string
            }
        };

        // Should not throw - uses default timeout
        handler.Apply(builder, config, null);

        await Task.CompletedTask;
    }

    #endregion

    #region CustomDecoratorHandler Tests

    [Scenario("Custom handler validates missing typeName")]
    [Fact]
    public async Task CustomHandler_validates_missing_typename()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITypeResolver>(new TestTypeResolver());
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<ConfigurationExtensionRegistry>();
        var handler = registry.GetDecoratorHandler("custom")!;

        var config = new DecoratorConfig
        {
            Type = "custom",
            TypeName = null
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Single(errors);
        Assert.Contains("Type name is required", errors[0].Message);

        await Task.CompletedTask;
    }

    [Scenario("Custom handler validates whitespace typeName")]
    [Fact]
    public async Task CustomHandler_validates_whitespace_typename()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITypeResolver>(new TestTypeResolver());
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<ConfigurationExtensionRegistry>();
        var handler = registry.GetDecoratorHandler("custom")!;

        var config = new DecoratorConfig
        {
            Type = "custom",
            TypeName = "   "
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Single(errors);
        Assert.Contains("Type name is required", errors[0].Message);

        await Task.CompletedTask;
    }

    [Scenario("Custom handler Apply skips when typeName is empty")]
    [Fact]
    public async Task CustomHandler_Apply_skips_empty_typename()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITypeResolver>(new TestTypeResolver());
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<ConfigurationExtensionRegistry>();
        var handler = registry.GetDecoratorHandler("custom")!;
        var builder = ExperimentFrameworkBuilder.Create();
        var logger = new TestLogger();

        var config = new DecoratorConfig
        {
            Type = "custom",
            TypeName = ""
        };

        handler.Apply(builder, config, logger);

        Assert.Contains(logger.Messages, m => m.Contains("missing typeName"));

        await Task.CompletedTask;
    }

    [Scenario("Custom handler Apply logs warning when type resolution fails")]
    [Fact]
    public async Task CustomHandler_Apply_logs_warning_on_type_resolution_failure()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITypeResolver>(new FailingTypeResolver());
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<ConfigurationExtensionRegistry>();
        var handler = registry.GetDecoratorHandler("custom")!;
        var builder = ExperimentFrameworkBuilder.Create();
        var logger = new TestLogger();

        var config = new DecoratorConfig
        {
            Type = "custom",
            TypeName = "NonExistentType"
        };

        handler.Apply(builder, config, logger);

        Assert.Contains(logger.Messages, m => m.Contains("Failed to create custom decorator"));

        await Task.CompletedTask;
    }

    [Scenario("Custom handler Apply logs warning when type is not IExperimentDecoratorFactory")]
    [Fact]
    public async Task CustomHandler_Apply_logs_warning_for_invalid_type()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITypeResolver>(new NonFactoryTypeResolver());
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();

        var registry = sp.GetRequiredService<ConfigurationExtensionRegistry>();
        var handler = registry.GetDecoratorHandler("custom")!;
        var builder = ExperimentFrameworkBuilder.Create();
        var logger = new TestLogger();

        var config = new DecoratorConfig
        {
            Type = "custom",
            TypeName = "SomeType"
        };

        handler.Apply(builder, config, logger);

        Assert.Contains(logger.Messages, m => m.Contains("does not implement IExperimentDecoratorFactory"));

        await Task.CompletedTask;
    }

    #endregion

    #region LoggingDecoratorHandler Tests

    [Scenario("Logging handler validates with null options")]
    [Fact]
    public async Task LoggingHandler_validates_with_null_options()
    {
        var registry = CreateRegistryWithHandlers();
        var handler = registry.GetDecoratorHandler("logging")!;

        var config = new DecoratorConfig
        {
            Type = "logging",
            Options = null
        };

        var errors = handler.Validate(config, "decorators[0]").ToList();

        Assert.Empty(errors);

        await Task.CompletedTask;
    }

    [Scenario("Logging handler Apply with custom log level")]
    [Fact]
    public async Task LoggingHandler_Apply_with_custom_log_level()
    {
        var registry = CreateRegistryWithHandlers();
        var handler = registry.GetDecoratorHandler("logging")!;
        var builder = ExperimentFrameworkBuilder.Create();

        var config = new DecoratorConfig
        {
            Type = "logging",
            Options = new Dictionary<string, object>
            {
                ["logLevel"] = "Debug"
            }
        };

        // Should not throw
        handler.Apply(builder, config, null);

        await Task.CompletedTask;
    }

    [Scenario("Logging handler Apply with invalid log level falls back to default")]
    [Fact]
    public async Task LoggingHandler_Apply_with_invalid_log_level()
    {
        var registry = CreateRegistryWithHandlers();
        var handler = registry.GetDecoratorHandler("logging")!;
        var builder = ExperimentFrameworkBuilder.Create();

        var config = new DecoratorConfig
        {
            Type = "logging",
            Options = new Dictionary<string, object>
            {
                ["logLevel"] = "InvalidLevel"
            }
        };

        // Should not throw - uses default
        handler.Apply(builder, config, null);

        await Task.CompletedTask;
    }

    #endregion

    private static ConfigurationExtensionRegistry CreateRegistryWithHandlers()
    {
        var services = new ServiceCollection();
        services.AddExperimentConfigurationExtensions();
        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<ConfigurationExtensionRegistry>();
    }

    private sealed class TestTypeResolver : ITypeResolver
    {
        public Type Resolve(string typeName) => throw new InvalidOperationException("Type not found");

        public bool TryResolve(string typeName, out Type? type)
        {
            type = null;
            return false;
        }

        public void RegisterAlias(string alias, Type type) { }
    }

    private sealed class FailingTypeResolver : ITypeResolver
    {
        public Type Resolve(string typeName) => throw new InvalidOperationException("Type resolution failed");

        public bool TryResolve(string typeName, out Type? type)
        {
            type = null;
            return false;
        }

        public void RegisterAlias(string alias, Type type) { }
    }

    private sealed class NonFactoryTypeResolver : ITypeResolver
    {
        public Type Resolve(string typeName) => typeof(NonFactoryClass); // Doesn't implement IExperimentDecoratorFactory

        public bool TryResolve(string typeName, out Type? type)
        {
            type = typeof(NonFactoryClass);
            return true;
        }

        public void RegisterAlias(string alias, Type type) { }
    }

    // A class that can be instantiated but doesn't implement IExperimentDecoratorFactory
    private sealed class NonFactoryClass { }

    private sealed class TestLogger : ILogger
    {
        public List<string> Messages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
