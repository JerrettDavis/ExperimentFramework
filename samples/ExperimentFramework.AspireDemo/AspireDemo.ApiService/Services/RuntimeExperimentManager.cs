using System.Text;
using ExperimentFramework.Configuration.Loading;
using ExperimentFramework.Configuration.Models;
using ExperimentFramework.Configuration.Validation;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AspireDemo.ApiService.Services;

/// <summary>
/// Manages runtime DSL configuration validation, preview, and application.
/// </summary>
public sealed class RuntimeExperimentManager
{
    private readonly ExperimentStateManager _stateManager;
    private readonly ILogger<RuntimeExperimentManager> _logger;

    private readonly IDeserializer _yamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly ISerializer _yamlSerializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .Build();

    private readonly ConfigurationValidator _validator = new();

    // Track last applied configuration
    private string? _lastAppliedYaml;
    private DateTime? _lastAppliedTime;

    public RuntimeExperimentManager(
        ExperimentStateManager stateManager,
        ILogger<RuntimeExperimentManager> logger)
    {
        _stateManager = stateManager;
        _logger = logger;
    }

    /// <summary>
    /// Validates YAML configuration without applying it.
    /// </summary>
    public DslValidationResult Validate(string yamlContent)
    {
        var errors = new List<DslValidationError>();
        var parsedExperiments = new List<ExperimentPreview>();

        try
        {
            // Phase 1: YAML Syntax validation
            ExperimentFrameworkConfigurationRoot? config;
            try
            {
                config = ParseYaml(yamlContent);
            }
            catch (YamlException ex)
            {
                errors.Add(new DslValidationError
                {
                    Path = "yaml",
                    Message = $"YAML syntax error: {ex.Message}",
                    Severity = "error",
                    Line = (int)ex.Start.Line,
                    Column = (int)ex.Start.Column,
                    EndLine = (int)ex.End.Line,
                    EndColumn = (int)ex.End.Column
                });
                return new DslValidationResult(false, errors, parsedExperiments);
            }
            catch (Exception ex)
            {
                errors.Add(new DslValidationError
                {
                    Path = "yaml",
                    Message = $"Failed to parse YAML: {ex.Message}",
                    Severity = "error",
                    Line = 1,
                    Column = 1
                });
                return new DslValidationResult(false, errors, parsedExperiments);
            }

            if (config == null)
            {
                errors.Add(new DslValidationError
                {
                    Path = "yaml",
                    Message = "Configuration is empty or invalid",
                    Severity = "error",
                    Line = 1,
                    Column = 1
                });
                return new DslValidationResult(false, errors, parsedExperiments);
            }

            // Phase 2: Schema/structure validation
            var validationResult = _validator.Validate(config);
            foreach (var error in validationResult.Errors)
            {
                var lineInfo = EstimateLineFromPath(yamlContent, error.Path);
                errors.Add(new DslValidationError
                {
                    Path = error.Path,
                    Message = error.Message,
                    Severity = error.Severity.ToString().ToLowerInvariant(),
                    Line = lineInfo.line,
                    Column = lineInfo.column,
                    EndLine = lineInfo.line,
                    EndColumn = lineInfo.column + 20
                });
            }

            // Phase 3: Build experiment preview
            if (config.Experiments != null)
            {
                foreach (var exp in config.Experiments)
                {
                    var existing = _stateManager.GetExperiment(exp.Name);
                    string action;

                    if (existing == null)
                    {
                        action = "create";
                    }
                    else if (HasChanges(existing, exp))
                    {
                        action = "update";
                    }
                    else
                    {
                        // No changes detected - don't show in preview
                        continue;
                    }

                    parsedExperiments.Add(new ExperimentPreview
                    {
                        Name = exp.Name,
                        TrialCount = exp.Trials?.Count ?? 0,
                        Action = action
                    });
                }
            }

            // Check for removals (experiments in current state but not in config)
            var configNames = config.Experiments?.Select(e => e.Name).ToHashSet(StringComparer.OrdinalIgnoreCase)
                              ?? new HashSet<string>();
            foreach (var existing in _stateManager.GetDslExperiments())
            {
                if (!configNames.Contains(existing.Name))
                {
                    parsedExperiments.Add(new ExperimentPreview
                    {
                        Name = existing.Name,
                        TrialCount = existing.Variants.Count,
                        Action = "remove"
                    });
                }
            }

            var isValid = !errors.Any(e => e.Severity == "error");
            return new DslValidationResult(isValid, errors, parsedExperiments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during DSL validation");
            errors.Add(new DslValidationError
            {
                Path = "",
                Message = $"Unexpected validation error: {ex.Message}",
                Severity = "error",
                Line = 1,
                Column = 1
            });
            return new DslValidationResult(false, errors, parsedExperiments);
        }
    }

    /// <summary>
    /// Applies validated configuration to the running application.
    /// </summary>
    public DslApplyResult Apply(string yamlContent)
    {
        // First validate
        var validation = Validate(yamlContent);
        if (!validation.IsValid)
        {
            return new DslApplyResult(false, [], validation.Errors);
        }

        var changes = new List<AppliedExperiment>();
        var snapshot = _stateManager.CreateSnapshot();

        try
        {
            var config = ParseYaml(yamlContent);
            if (config?.Experiments == null)
            {
                return new DslApplyResult(true, [], []);
            }

            // Track which experiments we're updating
            var updatedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var expConfig in config.Experiments)
            {
                var existing = _stateManager.GetExperiment(expConfig.Name);
                var variants = expConfig.Trials?.Select(t => new VariantInfo
                {
                    Name = t.Control?.Key ?? "default",
                    DisplayName = ToPascalCase(t.Control?.Key ?? "Default"),
                    Description = $"Implementation: {t.Control?.ImplementationType ?? "Unknown"}"
                }).ToList() ?? [];

                // Add conditions as variants
                if (expConfig.Trials != null)
                {
                    foreach (var trial in expConfig.Trials)
                    {
                        if (trial.Conditions != null)
                        {
                            foreach (var condition in trial.Conditions)
                            {
                                variants.Add(new VariantInfo
                                {
                                    Name = condition.Key,
                                    DisplayName = ToPascalCase(condition.Key),
                                    Description = $"Implementation: {condition.ImplementationType}"
                                });
                            }
                        }
                    }
                }

                var experimentInfo = new ExperimentInfo
                {
                    Name = expConfig.Name,
                    DisplayName = GetMetadataString(expConfig.Metadata, "displayName") ?? expConfig.Name,
                    Description = GetMetadataString(expConfig.Metadata, "description") ?? "",
                    ActiveVariant = variants.FirstOrDefault()?.Name ?? "default",
                    DefaultVariant = variants.FirstOrDefault()?.Name ?? "default",
                    Variants = variants.Any() ? variants : [new VariantInfo { Name = "default", DisplayName = "Default", Description = "Default variant" }],
                    Category = GetMetadataString(expConfig.Metadata, "category") ?? "Uncategorized",
                    Status = "Active"
                };
                experimentInfo.SourceEnum = ExperimentSource.Dsl;

                if (existing != null)
                {
                    _stateManager.UpdateExperiment(expConfig.Name, experimentInfo);
                    changes.Add(new AppliedExperiment { Name = expConfig.Name, Action = "updated" });
                }
                else
                {
                    _stateManager.AddExperiment(experimentInfo);
                    changes.Add(new AppliedExperiment { Name = expConfig.Name, Action = "created" });
                }

                updatedNames.Add(expConfig.Name);
            }

            // Remove DSL experiments that are no longer in config
            foreach (var existing in _stateManager.GetDslExperiments().ToList())
            {
                if (!updatedNames.Contains(existing.Name))
                {
                    _stateManager.RemoveExperiment(existing.Name);
                    changes.Add(new AppliedExperiment { Name = existing.Name, Action = "removed" });
                }
            }

            // Store applied configuration
            _lastAppliedYaml = yamlContent;
            _lastAppliedTime = DateTime.UtcNow;

            _logger.LogInformation("Applied DSL configuration: {ChangeCount} changes", changes.Count);
            return new DslApplyResult(true, changes, []);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply DSL configuration, rolling back");
            _stateManager.RestoreSnapshot(snapshot);
            return new DslApplyResult(false, [], [new DslValidationError
            {
                Path = "",
                Message = $"Failed to apply configuration: {ex.Message}",
                Severity = "error",
                Line = 1,
                Column = 1
            }]);
        }
    }

    /// <summary>
    /// Exports the current configuration as YAML.
    /// </summary>
    public string ExportCurrentConfiguration()
    {
        var experiments = _stateManager.GetAllExperiments();

        var sb = new StringBuilder();
        sb.AppendLine("# ExperimentFramework Configuration DSL");
        sb.AppendLine("# Generated from current runtime state");
        sb.AppendLine($"# Timestamp: {DateTime.UtcNow:O}");
        sb.AppendLine();
        sb.AppendLine("experiments:");

        foreach (var exp in experiments)
        {
            sb.AppendLine($"  - name: {exp.Name}");
            sb.AppendLine($"    metadata:");
            sb.AppendLine($"      displayName: \"{exp.DisplayName}\"");
            sb.AppendLine($"      description: \"{exp.Description}\"");
            sb.AppendLine($"      category: \"{exp.Category}\"");
            sb.AppendLine($"    trials:");
            sb.AppendLine($"      - serviceType: \"I{ToPascalCase(exp.Name)}\"");
            sb.AppendLine($"        selectionMode:");
            sb.AppendLine($"          type: configurationKey");
            sb.AppendLine($"          key: \"Experiments:{ToPascalCase(exp.Name)}\"");
            sb.AppendLine($"        control:");
            sb.AppendLine($"          key: {exp.Variants.FirstOrDefault()?.Name ?? "default"}");
            sb.AppendLine($"          implementationType: \"{ToPascalCase(exp.Variants.FirstOrDefault()?.Name ?? "Default")}Implementation\"");

            if (exp.Variants.Count > 1)
            {
                sb.AppendLine($"        conditions:");
                foreach (var variant in exp.Variants.Skip(1))
                {
                    sb.AppendLine($"          - key: {variant.Name}");
                    sb.AppendLine($"            implementationType: \"{ToPascalCase(variant.Name)}Implementation\"");
                }
            }

            sb.AppendLine($"        errorPolicy:");
            sb.AppendLine($"          type: fallbackToControl");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets information about the last applied configuration.
    /// </summary>
    public (string? yaml, DateTime? appliedAt) GetLastApplied() => (_lastAppliedYaml, _lastAppliedTime);

    private ExperimentFrameworkConfigurationRoot? ParseYaml(string content)
    {
        // Try wrapped format first
        try
        {
            var wrapped = _yamlDeserializer.Deserialize<Dictionary<string, ExperimentFrameworkConfigurationRoot>>(content);
            if (wrapped != null && wrapped.TryGetValue("experimentFramework", out var config))
            {
                return config;
            }
        }
        catch { /* Try direct format */ }

        // Try direct format
        return _yamlDeserializer.Deserialize<ExperimentFrameworkConfigurationRoot>(content);
    }

    private static (int line, int column) EstimateLineFromPath(string yaml, string path)
    {
        // Simple estimation: find the key in the YAML and return its line
        var lines = yaml.Split('\n');
        var pathParts = path.Split('.');

        // Extract key name (e.g., "name" from "experiments[0].name")
        var lastPart = pathParts.LastOrDefault() ?? "";
        var keyName = lastPart.Contains('[') ? lastPart.Split('[')[0] : lastPart;

        // Find the line containing this key
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].TrimStart().StartsWith(keyName + ":"))
            {
                return (i + 1, lines[i].IndexOf(keyName) + 1);
            }
        }

        return (1, 1);
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var parts = input.Split(['-', '_', ' '], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(p =>
            char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p[1..] : "")));
    }

    private static string? GetMetadataString(Dictionary<string, object>? metadata, string key)
    {
        if (metadata == null) return null;
        return metadata.TryGetValue(key, out var value) ? value?.ToString() : null;
    }

    /// <summary>
    /// Determines if the DSL configuration would result in changes to an existing experiment.
    /// </summary>
    private bool HasChanges(ExperimentInfo existing, ExperimentConfig config)
    {
        // Compare display name
        var newDisplayName = GetMetadataString(config.Metadata, "displayName") ?? config.Name;
        if (existing.DisplayName != newDisplayName)
            return true;

        // Compare description
        var newDescription = GetMetadataString(config.Metadata, "description") ?? "";
        if (existing.Description != newDescription)
            return true;

        // Compare category
        var newCategory = GetMetadataString(config.Metadata, "category") ?? "Uncategorized";
        if (existing.Category != newCategory)
            return true;

        // Compare variants (build the variant list from config like in Apply)
        var newVariants = config.Trials?
            .SelectMany(trial =>
            {
                var keys = new List<string>();
                if (trial.Control?.Key != null)
                    keys.Add(trial.Control.Key);
                if (trial.Conditions != null)
                    keys.AddRange(trial.Conditions.Where(c => c.Key != null).Select(c => c.Key!));
                return keys;
            })
            .ToList() ?? new List<string>();

        var existingVariantNames = existing.Variants.Select(v => v.Name).OrderBy(n => n).ToList();
        var newVariantNames = newVariants.Distinct().OrderBy(n => n).ToList();

        if (!existingVariantNames.SequenceEqual(newVariantNames))
            return true;

        // No changes detected
        return false;
    }
}

// ============================================================================
// DTOs for DSL Operations
// ============================================================================

public record DslValidationResult(
    bool IsValid,
    List<DslValidationError> Errors,
    List<ExperimentPreview> ParsedExperiments);

public record DslApplyResult(
    bool Success,
    List<AppliedExperiment> Changes,
    List<DslValidationError> Errors);

public class DslValidationError
{
    public string Path { get; set; } = "";
    public string Message { get; set; } = "";
    public string Severity { get; set; } = "error";
    public int Line { get; set; } = 1;
    public int Column { get; set; } = 1;
    public int EndLine { get; set; } = 1;
    public int EndColumn { get; set; } = 1;
}

public class ExperimentPreview
{
    public string Name { get; set; } = "";
    public int TrialCount { get; set; }
    public string Action { get; set; } = ""; // "create", "update", "remove"
}

public class AppliedExperiment
{
    public string Name { get; set; } = "";
    public string Action { get; set; } = ""; // "created", "updated", "removed"
}
