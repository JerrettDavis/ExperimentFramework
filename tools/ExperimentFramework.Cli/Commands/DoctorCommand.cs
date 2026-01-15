using System.CommandLine;
using System.Reflection;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Selection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ExperimentFramework.Cli.Commands;

/// <summary>
/// Doctor command for validating experiment setup and DI wiring.
/// </summary>
internal static class DoctorCommand
{
    public static Command Create()
    {
        var assemblyOption = new Option<FileInfo?>(
            name: "--assembly",
            description: "Path to the application assembly containing the host configuration");

        var configOption = new Option<FileInfo?>(
            name: "--config",
            description: "Path to the configuration file (JSON)");

        var doctorCommand = new Command("doctor", "Validate experiment configuration and DI wiring")
        {
            assemblyOption,
            configOption
        };

        doctorCommand.SetHandler(ExecuteDoctor, assemblyOption, configOption);

        return doctorCommand;
    }

    private static async Task<int> ExecuteDoctor(FileInfo? assemblyFile, FileInfo? configFile)
    {
        Console.WriteLine("ExperimentFramework Doctor");
        Console.WriteLine("==========================");
        Console.WriteLine();

        var hasErrors = false;

        // If config file provided, validate it
        if (configFile != null)
        {
            Console.WriteLine($"Checking configuration file: {configFile.FullName}");
            
            if (!configFile.Exists)
            {
                Console.Error.WriteLine("✗ Configuration file not found");
                hasErrors = true;
            }
            else
            {
                try
                {
                    var configJson = await File.ReadAllTextAsync(configFile.FullName);
                    var config = System.Text.Json.JsonSerializer.Deserialize<ExperimentFrameworkConfigurationRoot>(
                        configJson,
                        new System.Text.Json.JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true,
                            AllowTrailingCommas = true,
                            ReadCommentHandling = System.Text.Json.JsonCommentHandling.Skip
                        });

                    if (config == null)
                    {
                        Console.Error.WriteLine("✗ Failed to parse configuration file");
                        hasErrors = true;
                    }
                    else
                    {
                        // NOTE: This uses the default ConfigurationValidator without a ConfigurationExtensionRegistry.
                        // As a result, custom selection modes and decorators from extension packages are not validated here.
                        var validator = new ExperimentFramework.Configuration.Validation.ConfigurationValidator();
                        var result = validator.Validate(config);

                        if (!result.IsValid)
                        {
                            Console.Error.WriteLine($"✗ Configuration validation failed with {result.Errors.Count} error(s):");
                            foreach (var error in result.Errors)
                            {
                                Console.Error.WriteLine($"  - {error.Path}: {error.Message}");
                            }
                            hasErrors = true;
                        }
                        else
                        {
                            Console.WriteLine("✓ Configuration file is valid");
                            
                            // Validate trial definitions
                            var trialErrors = ValidateTrialDefinitions(config);
                            if (trialErrors.Any())
                            {
                                Console.Error.WriteLine("✗ Trial definition issues found:");
                                foreach (var error in trialErrors)
                                {
                                    Console.Error.WriteLine($"  - {error}");
                                }
                                hasErrors = true;
                            }
                            else
                            {
                                Console.WriteLine("✓ Trial definitions are valid");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"✗ Error validating configuration: {ex.Message}");
                    hasErrors = true;
                }
            }

            Console.WriteLine();
        }

        // If assembly provided, try to load and check types
        if (assemblyFile != null)
        {
            Console.WriteLine($"Checking assembly: {assemblyFile.FullName}");
            
            if (!assemblyFile.Exists)
            {
                Console.Error.WriteLine("✗ Assembly file not found");
                hasErrors = true;
            }
            else
            {
                try
                {
                    var assembly = Assembly.LoadFrom(assemblyFile.FullName);
                    Console.WriteLine($"✓ Assembly loaded: {assembly.GetName().Name}");
                    
                    // Check for ISelectionModeProvider implementations
                    var providerTypes = assembly.GetTypes()
                        .Where(t => typeof(ISelectionModeProvider).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
                        .ToList();

                    if (providerTypes.Any())
                    {
                        Console.WriteLine($"✓ Found {providerTypes.Count} selection provider(s):");
                        foreach (var type in providerTypes)
                        {
                            Console.WriteLine($"  - {type.Name}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("⚠ No selection providers found in assembly");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"✗ Error loading assembly: {ex.Message}");
                    hasErrors = true;
                }
            }

            Console.WriteLine();
        }

        // If neither option provided, show guidance
        if (assemblyFile == null && configFile == null)
        {
            Console.WriteLine("No configuration or assembly specified.");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  dotnet experimentframework doctor --config <path-to-config.json>");
            Console.WriteLine("  dotnet experimentframework doctor --assembly <path-to-assembly.dll>");
            Console.WriteLine("  dotnet experimentframework doctor --config <config> --assembly <assembly>");
            Console.WriteLine();
            Console.WriteLine("The doctor command validates:");
            Console.WriteLine("  - Configuration file syntax and schema compliance");
            Console.WriteLine("  - Trial definitions (control/condition compatibility)");
            Console.WriteLine("  - Selection provider registrations");
            Console.WriteLine();
            return 1;
        }

        if (hasErrors)
        {
            Console.WriteLine("✗ Doctor check failed - please address the errors above");
            return 1;
        }

        Console.WriteLine("✓ All checks passed!");
        return 0;
    }

    private static List<string> ValidateTrialDefinitions(ExperimentFrameworkConfigurationRoot config)
    {
        var errors = new List<string>();

        // Validate standalone trials
        if (config.Trials != null)
        {
            foreach (var trial in config.Trials)
            {
                ValidateTrial(trial, errors);
            }
        }

        // Validate experiments
        if (config.Experiments != null)
        {
            foreach (var experiment in config.Experiments.Where(e => e.Trials != null))
            {
                foreach (var trial in experiment.Trials!)
                {
                    ValidateTrial(trial, errors);
                }
            }
        }

        return errors;
    }

    private static void ValidateTrial(TrialConfig trial, List<string> errors)
    {
        // Check that control exists
        if (trial.Control == null)
        {
            errors.Add($"Trial '{trial.ServiceType}': Control implementation is required");
            return;
        }

        // Check for duplicate keys
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { trial.Control.Key };
        
        if (trial.Conditions != null)
        {
            var duplicates = trial.Conditions
                .Where(c => !keys.Add(c.Key))
                .ToList();
            
            foreach (var condition in duplicates)
            {
                errors.Add($"Trial '{trial.ServiceType}': Duplicate condition key '{condition.Key}'");
            }
        }

        // Validate type names are provided
        if (string.IsNullOrWhiteSpace(trial.Control.ImplementationType))
        {
            errors.Add($"Trial '{trial.ServiceType}': Control implementation type is required");
        }

        if (trial.Conditions != null)
        {
            foreach (var condition in trial.Conditions.Where(c => string.IsNullOrWhiteSpace(c.ImplementationType)))
            {
                errors.Add($"Trial '{trial.ServiceType}': Condition '{condition.Key}' implementation type is required");
            }
        }
    }
}
