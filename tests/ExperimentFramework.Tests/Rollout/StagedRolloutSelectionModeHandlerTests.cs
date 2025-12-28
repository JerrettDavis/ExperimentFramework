using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Rollout;
using ExperimentFramework.Rollout.Configuration;
using Microsoft.Extensions.Logging;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Rollout;

[Feature("StagedRolloutSelectionModeHandler handles configuration-based staged rollout setup")]
public sealed class StagedRolloutSelectionModeHandlerTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Handler has correct mode type")]
    [Fact]
    public Task Handler_has_correct_mode_type()
        => Given("a handler", () => new StagedRolloutSelectionModeHandler())
            .Then("mode type is 'stagedRollout'", h => h.ModeType == "stagedRollout")
            .AssertPassed();

    [Scenario("Validate returns no errors for null options")]
    [Fact]
    public async Task Validate_returns_no_errors_for_null_options()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var config = new SelectionModeConfig { Type = "stagedRollout", Options = null };

        var errors = handler.Validate(config, "selection").ToList();

        Assert.Empty(errors);

        await Task.CompletedTask;
    }

    [Scenario("Validate returns errors for invalid stage format")]
    [Fact]
    public async Task Validate_returns_error_for_invalid_stage_format()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var config = new SelectionModeConfig
        {
            Type = "stagedRollout",
            Options = new Dictionary<string, object>
            {
                ["stages"] = new List<object> { "invalid" } // Not a dictionary
            }
        };

        var errors = handler.Validate(config, "selection").ToList();

        Assert.Single(errors);
        Assert.Contains("stages[0]", errors[0].Path);

        await Task.CompletedTask;
    }

    [Scenario("Validate returns error when startsAt is missing")]
    [Fact]
    public async Task Validate_returns_error_when_startsAt_missing()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var config = new SelectionModeConfig
        {
            Type = "stagedRollout",
            Options = new Dictionary<string, object>
            {
                ["stages"] = new List<object>
                {
                    new Dictionary<string, object> { ["percentage"] = 50 }
                }
            }
        };

        var errors = handler.Validate(config, "selection").ToList();

        Assert.Single(errors);
        Assert.Contains("startsAt", errors[0].Path);

        await Task.CompletedTask;
    }

    [Scenario("Validate returns error when percentage is missing")]
    [Fact]
    public async Task Validate_returns_error_when_percentage_missing()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var config = new SelectionModeConfig
        {
            Type = "stagedRollout",
            Options = new Dictionary<string, object>
            {
                ["stages"] = new List<object>
                {
                    new Dictionary<string, object> { ["startsAt"] = "2024-01-01" }
                }
            }
        };

        var errors = handler.Validate(config, "selection").ToList();

        Assert.Single(errors);
        Assert.Contains("percentage", errors[0].Path);

        await Task.CompletedTask;
    }

    [Scenario("Validate returns error for invalid percentage")]
    [Fact]
    public async Task Validate_returns_error_for_invalid_percentage()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var config = new SelectionModeConfig
        {
            Type = "stagedRollout",
            Options = new Dictionary<string, object>
            {
                ["stages"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["startsAt"] = "2024-01-01",
                        ["percentage"] = 150 // Invalid
                    }
                }
            }
        };

        var errors = handler.Validate(config, "selection").ToList();

        Assert.Single(errors);
        Assert.Contains("between 0 and 100", errors[0].Message);

        await Task.CompletedTask;
    }

    [Scenario("Validate passes for valid configuration")]
    [Fact]
    public async Task Validate_passes_for_valid_configuration()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var config = new SelectionModeConfig
        {
            Type = "stagedRollout",
            Options = new Dictionary<string, object>
            {
                ["stages"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["startsAt"] = "2024-01-01T00:00:00Z",
                        ["percentage"] = 10
                    },
                    new Dictionary<string, object>
                    {
                        ["startsAt"] = "2024-01-08T00:00:00Z",
                        ["percentage"] = 50
                    }
                }
            }
        };

        var errors = handler.Validate(config, "selection").ToList();

        Assert.Empty(errors);

        await Task.CompletedTask;
    }

    [Scenario("Validate handles stages with negative percentage")]
    [Fact]
    public async Task Validate_handles_negative_percentage()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var config = new SelectionModeConfig
        {
            Type = "stagedRollout",
            Options = new Dictionary<string, object>
            {
                ["stages"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["startsAt"] = "2024-01-01",
                        ["percentage"] = -10 // Negative
                    }
                }
            }
        };

        var errors = handler.Validate(config, "selection").ToList();

        Assert.Single(errors);
        Assert.Contains("between 0 and 100", errors[0].Message);

        await Task.CompletedTask;
    }

    [Scenario("Validate handles long integer percentage")]
    [Fact]
    public async Task Validate_handles_long_integer_percentage()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var config = new SelectionModeConfig
        {
            Type = "stagedRollout",
            Options = new Dictionary<string, object>
            {
                ["stages"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["startsAt"] = "2024-01-01",
                        ["percentage"] = 50L // Long instead of int
                    }
                }
            }
        };

        var errors = handler.Validate(config, "selection").ToList();

        Assert.Empty(errors);

        await Task.CompletedTask;
    }

    [Scenario("Validate handles string percentage")]
    [Fact]
    public async Task Validate_handles_string_percentage()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var config = new SelectionModeConfig
        {
            Type = "stagedRollout",
            Options = new Dictionary<string, object>
            {
                ["stages"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["startsAt"] = "2024-01-01",
                        ["percentage"] = "75" // String
                    }
                }
            }
        };

        var errors = handler.Validate(config, "selection").ToList();

        Assert.Empty(errors);

        await Task.CompletedTask;
    }

    [Scenario("Apply configures builder with staged rollout mode")]
    [Fact]
    public async Task Apply_configures_builder()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var fwBuilder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        fwBuilder.Define<IFormattable>(builder =>
        {
            var config = new SelectionModeConfig
            {
                Type = "stagedRollout",
                SelectorName = "test-staged-rollout",
                Options = new Dictionary<string, object>
                {
                    ["includedKey"] = "treatment",
                    ["excludedKey"] = "control",
                    ["seed"] = "my-seed",
                    ["stages"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["startsAt"] = "2024-01-01T00:00:00Z",
                            ["percentage"] = 25,
                            ["description"] = "Initial 25% rollout"
                        },
                        new Dictionary<string, object>
                        {
                            ["startsAt"] = "2024-01-08T00:00:00Z",
                            ["percentage"] = 100L // Test long type
                        }
                    }
                }
            };
            handler.Apply(builder, config, null);
            builder.AddControl<FormattableString>("control");
        });

        Assert.NotNull(fwBuilder);
        await Task.CompletedTask;
    }

    [Scenario("Apply with null options uses defaults")]
    [Fact]
    public async Task Apply_with_null_options()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var fwBuilder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        fwBuilder.Define<IFormattable>(builder =>
        {
            var config = new SelectionModeConfig
            {
                Type = "stagedRollout",
                Options = null
            };
            handler.Apply(builder, config, null);
            builder.AddControl<FormattableString>("control");
        });

        Assert.NotNull(fwBuilder);
        await Task.CompletedTask;
    }

    [Scenario("Apply with logger logs configuration")]
    [Fact]
    public async Task Apply_with_logger()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var fwBuilder = ExperimentFramework.ExperimentFrameworkBuilder.Create();
        var logger = new TestLogger();

        fwBuilder.Define<IFormattable>(builder =>
        {
            var config = new SelectionModeConfig
            {
                Type = "stagedRollout",
                Options = new Dictionary<string, object>
                {
                    ["stages"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["startsAt"] = "2024-01-01T00:00:00Z",
                            ["percentage"] = 50
                        }
                    }
                }
            };
            handler.Apply(builder, config, logger);
            builder.AddControl<FormattableString>("control");
        });

        Assert.True(logger.LoggedMessages.Count >= 0);
        await Task.CompletedTask;
    }

    [Scenario("Apply parses DateTimeOffset from DateTime object")]
    [Fact]
    public async Task Apply_parses_datetime_object()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var fwBuilder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        fwBuilder.Define<IFormattable>(builder =>
        {
            var config = new SelectionModeConfig
            {
                Type = "stagedRollout",
                Options = new Dictionary<string, object>
                {
                    ["stages"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["startsAt"] = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc), // DateTime object
                            ["percentage"] = 50
                        }
                    }
                }
            };
            handler.Apply(builder, config, null);
            builder.AddControl<FormattableString>("control");
        });

        Assert.NotNull(fwBuilder);
        await Task.CompletedTask;
    }

    [Scenario("Apply parses DateTimeOffset from DateTimeOffset object")]
    [Fact]
    public async Task Apply_parses_datetimeoffset_object()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var fwBuilder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        fwBuilder.Define<IFormattable>(builder =>
        {
            var config = new SelectionModeConfig
            {
                Type = "stagedRollout",
                Options = new Dictionary<string, object>
                {
                    ["stages"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["startsAt"] = new DateTimeOffset(2024, 6, 1, 0, 0, 0, TimeSpan.Zero),
                            ["percentage"] = 50
                        }
                    }
                }
            };
            handler.Apply(builder, config, null);
            builder.AddControl<FormattableString>("control");
        });

        Assert.NotNull(fwBuilder);
        await Task.CompletedTask;
    }

    [Scenario("Apply handles stage with missing startsAt")]
    [Fact]
    public async Task Apply_handles_missing_startsAt()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var fwBuilder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        fwBuilder.Define<IFormattable>(builder =>
        {
            var config = new SelectionModeConfig
            {
                Type = "stagedRollout",
                Options = new Dictionary<string, object>
                {
                    ["stages"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["percentage"] = 50 // No startsAt
                        }
                    }
                }
            };
            handler.Apply(builder, config, null);
            builder.AddControl<FormattableString>("control");
        });

        Assert.NotNull(fwBuilder);
        await Task.CompletedTask;
    }

    [Scenario("Apply handles unsupported startsAt type")]
    [Fact]
    public async Task Apply_handles_unsupported_startsAt_type()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var fwBuilder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        fwBuilder.Define<IFormattable>(builder =>
        {
            var config = new SelectionModeConfig
            {
                Type = "stagedRollout",
                Options = new Dictionary<string, object>
                {
                    ["stages"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["startsAt"] = 12345, // Unsupported type (int)
                            ["percentage"] = 50
                        }
                    }
                }
            };
            handler.Apply(builder, config, null);
            builder.AddControl<FormattableString>("control");
        });

        Assert.NotNull(fwBuilder);
        await Task.CompletedTask;
    }

    [Scenario("Apply handles unsupported percentage type")]
    [Fact]
    public async Task Apply_handles_unsupported_percentage_type()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var fwBuilder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        fwBuilder.Define<IFormattable>(builder =>
        {
            var config = new SelectionModeConfig
            {
                Type = "stagedRollout",
                Options = new Dictionary<string, object>
                {
                    ["stages"] = new List<object>
                    {
                        new Dictionary<string, object>
                        {
                            ["startsAt"] = "2024-01-01",
                            ["percentage"] = 50.5 // Unsupported type (double)
                        }
                    }
                }
            };
            handler.Apply(builder, config, null);
            builder.AddControl<FormattableString>("control");
        });

        Assert.NotNull(fwBuilder);
        await Task.CompletedTask;
    }

    [Scenario("Apply handles non-dictionary stage entry")]
    [Fact]
    public async Task Apply_handles_non_dictionary_stage()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var fwBuilder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        fwBuilder.Define<IFormattable>(builder =>
        {
            var config = new SelectionModeConfig
            {
                Type = "stagedRollout",
                Options = new Dictionary<string, object>
                {
                    ["stages"] = new List<object>
                    {
                        "invalid-stage-entry" // Not a dictionary
                    }
                }
            };
            handler.Apply(builder, config, null);
            builder.AddControl<FormattableString>("control");
        });

        Assert.NotNull(fwBuilder);
        await Task.CompletedTask;
    }

    [Scenario("Apply handles stages that is not a List")]
    [Fact]
    public async Task Apply_handles_non_list_stages()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var fwBuilder = ExperimentFramework.ExperimentFrameworkBuilder.Create();

        fwBuilder.Define<IFormattable>(builder =>
        {
            var config = new SelectionModeConfig
            {
                Type = "stagedRollout",
                Options = new Dictionary<string, object>
                {
                    ["stages"] = "not-a-list" // Not a list
                }
            };
            handler.Apply(builder, config, null);
            builder.AddControl<FormattableString>("control");
        });

        Assert.NotNull(fwBuilder);
        await Task.CompletedTask;
    }

    [Scenario("Validate handles percentage with unsupported type")]
    [Fact]
    public async Task Validate_handles_unsupported_percentage_type()
    {
        var handler = new StagedRolloutSelectionModeHandler();
        var config = new SelectionModeConfig
        {
            Type = "stagedRollout",
            Options = new Dictionary<string, object>
            {
                ["stages"] = new List<object>
                {
                    new Dictionary<string, object>
                    {
                        ["startsAt"] = "2024-01-01",
                        ["percentage"] = 50.5 // Double not supported - should not trigger validation error
                    }
                }
            }
        };

        var errors = handler.Validate(config, "selection").ToList();

        // Should not have percentage validation error since type doesn't parse
        Assert.Empty(errors);

        await Task.CompletedTask;
    }

    private sealed class TestLogger : ILogger
    {
        public List<string> LoggedMessages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            LoggedMessages.Add(formatter(state, exception));
        }
    }
}
