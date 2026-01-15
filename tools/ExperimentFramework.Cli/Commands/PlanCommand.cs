using System.CommandLine;
using System.Text.Json;
using ExperimentFramework.Configuration.Building;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.ServiceRegistration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExperimentFramework.Cli.Commands;

/// <summary>
/// Plan command for exporting registration plans.
/// </summary>
internal static class PlanCommand
{
    public static Command Create()
    {
        var planCommand = new Command("plan", "Registration plan commands");

        var exportCommand = CreateExportCommand();
        planCommand.AddCommand(exportCommand);

        return planCommand;
    }

    private static Command CreateExportCommand()
    {
        var configOption = new Option<FileInfo>(
            name: "--config",
            description: "Path to the configuration file (JSON)");
        configOption.IsRequired = true;

        var formatOption = new Option<string>(
            name: "--format",
            description: "Output format (json or text)",
            getDefaultValue: () => "text");
        formatOption.AddValidator(result =>
        {
            var value = result.GetValueForOption(formatOption);
            if (value != null && value != "json" && value != "text")
            {
                result.ErrorMessage = "Format must be 'json' or 'text'";
            }
        });

        var outOption = new Option<FileInfo?>(
            name: "--out",
            description: "Output file path (default: stdout)");

        var exportCommand = new Command("export", "Export registration plan for experiment configuration")
        {
            configOption,
            formatOption,
            outOption
        };

        exportCommand.SetHandler(ExecuteExport, configOption, formatOption, outOption);

        return exportCommand;
    }

    private static async Task<int> ExecuteExport(FileInfo configFile, string format, FileInfo? outFile)
    {
        try
        {
            Console.WriteLine($"Loading configuration from: {configFile.FullName}");

            if (!configFile.Exists)
            {
                Console.Error.WriteLine("✗ Configuration file not found");
                return 1;
            }

            // Load configuration from file
            var configJson = await File.ReadAllTextAsync(configFile.FullName);
            var experimentConfig = JsonSerializer.Deserialize<ExperimentFrameworkConfigurationRoot>(
                configJson,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });

            if (experimentConfig == null)
            {
                Console.Error.WriteLine("✗ Failed to parse configuration file");
                return 1;
            }

            Console.WriteLine("✓ Configuration loaded successfully");
            Console.WriteLine();
            
            // Generate report directly from configuration
            // We don't try to resolve types here since this is just a plan view
            var report = GenerateConfigurationSummary(experimentConfig, format);

            // Output to file or stdout
            if (outFile != null)
            {
                await File.WriteAllTextAsync(outFile.FullName, report);
                Console.WriteLine($"✓ Plan exported to: {outFile.FullName}");
            }
            else
            {
                Console.WriteLine(report);
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"✗ Error exporting plan: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine($"  Inner: {ex.InnerException.Message}");
            }
            return 1;
        }
    }

    private static string GenerateConfigurationSummary(ExperimentFrameworkConfigurationRoot config, string format)
    {
        if (format.ToLowerInvariant() == "json")
        {
            return GenerateJsonSummary(config);
        }
        return GenerateTextSummary(config);
    }

    private static string GenerateTextSummary(ExperimentFrameworkConfigurationRoot config)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== ExperimentFramework Configuration Plan ===");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        // Global decorators
        if (config.Decorators?.Count > 0)
        {
            sb.AppendLine($"--- Global Decorators ({config.Decorators.Count}) ---");
            foreach (var decorator in config.Decorators)
            {
                sb.AppendLine($"  • {decorator.Type}");
                if (!string.IsNullOrEmpty(decorator.TypeName))
                {
                    sb.AppendLine($"    Type: {decorator.TypeName}");
                }
            }
            sb.AppendLine();
        }

        // Standalone trials
        if (config.Trials?.Count > 0)
        {
            sb.AppendLine($"--- Standalone Trials ({config.Trials.Count}) ---");
            foreach (var trial in config.Trials)
            {
                sb.AppendLine($"  Service: {trial.ServiceType}");
                sb.AppendLine($"    Selection Mode: {trial.SelectionMode.Type}");
                sb.AppendLine($"    Control: {trial.Control.Key} -> {trial.Control.ImplementationType}");
                if (trial.Conditions?.Count > 0)
                {
                    sb.AppendLine($"    Conditions:");
                    foreach (var condition in trial.Conditions)
                    {
                        sb.AppendLine($"      - {condition.Key} -> {condition.ImplementationType}");
                    }
                }
                sb.AppendLine();
            }
        }

        // Named experiments
        if (config.Experiments?.Count > 0)
        {
            sb.AppendLine($"--- Named Experiments ({config.Experiments.Count}) ---");
            foreach (var experiment in config.Experiments)
            {
                sb.AppendLine($"  Experiment: {experiment.Name}");
                if (experiment.Trials != null)
                {
                    sb.AppendLine($"    Trials: {experiment.Trials.Count}");
                    foreach (var trial in experiment.Trials)
                    {
                        sb.AppendLine($"      • {trial.ServiceType}");
                        sb.AppendLine($"        Mode: {trial.SelectionMode.Type}");
                        sb.AppendLine($"        Control: {trial.Control.Key}");
                        if (trial.Conditions?.Count > 0)
                        {
                            sb.AppendLine($"        Variants: {string.Join(", ", trial.Conditions.Select(c => c.Key))}");
                        }
                    }
                }
                sb.AppendLine();
            }
        }

        sb.AppendLine("=== End of Plan ===");
        return sb.ToString();
    }

    private static string GenerateJsonSummary(ExperimentFrameworkConfigurationRoot config)
    {
        var summary = new
        {
            generatedAt = DateTimeOffset.UtcNow,
            decorators = config.Decorators?.Select(d => new
            {
                type = d.Type,
                typeName = d.TypeName
            }).ToArray() ?? Array.Empty<object>(),
            trials = config.Trials?.Select(t => new
            {
                serviceType = t.ServiceType,
                selectionMode = t.SelectionMode.Type,
                control = new { key = t.Control.Key, implementation = t.Control.ImplementationType },
                conditions = t.Conditions?.Select(c => new
                {
                    key = c.Key,
                    implementation = c.ImplementationType
                }).ToArray() ?? Array.Empty<object>()
            }).ToArray() ?? Array.Empty<object>(),
            experiments = config.Experiments?.Select(e => new
            {
                name = e.Name,
                trials = e.Trials?.Select(t => new
                {
                    serviceType = t.ServiceType,
                    selectionMode = t.SelectionMode.Type,
                    control = new { key = t.Control.Key, implementation = t.Control.ImplementationType },
                    conditions = t.Conditions?.Select(c => new
                    {
                        key = c.Key,
                        implementation = c.ImplementationType
                    }).ToArray() ?? Array.Empty<object>()
                }).ToArray() ?? Array.Empty<object>()
            }).ToArray() ?? Array.Empty<object>()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return JsonSerializer.Serialize(summary, options);
    }
}
