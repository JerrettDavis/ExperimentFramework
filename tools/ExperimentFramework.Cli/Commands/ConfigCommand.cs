using System.CommandLine;
using System.Text.Json;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;

namespace ExperimentFramework.Cli.Commands;

/// <summary>
/// Config command for validating configuration files.
/// </summary>
internal static class ConfigCommand
{
    public static Command Create()
    {
        var configCommand = new Command("config", "Configuration validation commands");

        var validateCommand = CreateValidateCommand();
        configCommand.AddCommand(validateCommand);

        return configCommand;
    }

    private static Command CreateValidateCommand()
    {
        var pathArgument = new Argument<FileInfo>(
            name: "path",
            description: "Path to the configuration file to validate");

        var validateCommand = new Command("validate", "Validate a configuration file against the ExperimentFramework schema")
        {
            pathArgument
        };

        validateCommand.SetHandler(ExecuteValidate, pathArgument);

        return validateCommand;
    }

    private static async Task<int> ExecuteValidate(FileInfo configFile)
    {
        Console.WriteLine($"Validating configuration file: {configFile.FullName}");
        Console.WriteLine();

        if (!configFile.Exists)
        {
            Console.Error.WriteLine($"✗ Error: Configuration file not found: {configFile.FullName}");
            return 1;
        }

        try
        {
            // Load configuration from file
            var json = await File.ReadAllTextAsync(configFile.FullName);
            var config = JsonSerializer.Deserialize<ExperimentFrameworkConfigurationRoot>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (config == null)
            {
                Console.Error.WriteLine("✗ Error: Failed to parse configuration file");
                return 1;
            }

            // Validate configuration
            var validator = new ConfigurationValidator();
            var result = validator.Validate(config);

            if (result.IsValid)
            {
                Console.WriteLine("✓ Configuration is valid");
                return 0;
            }

            // Display validation errors
            Console.Error.WriteLine($"✗ Configuration validation failed with {result.Errors.Count} error(s):");
            Console.Error.WriteLine();

            foreach (var error in result.Errors)
            {
                var icon = error.Severity == ValidationSeverity.Error ? "✗" : "⚠";
                var severityText = error.Severity == ValidationSeverity.Error ? "ERROR" : "WARNING";
                
                Console.Error.WriteLine($"{icon} [{severityText}] {error.Path}");
                Console.Error.WriteLine($"  {error.Message}");
                Console.Error.WriteLine();
            }

            return 1;
        }
        catch (JsonException ex)
        {
            Console.Error.WriteLine($"✗ Error: Invalid JSON format");
            Console.Error.WriteLine($"  {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"✗ Error: {ex.Message}");
            return 1;
        }
    }
}
