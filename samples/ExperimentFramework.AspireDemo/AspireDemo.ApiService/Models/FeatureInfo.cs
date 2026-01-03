namespace AspireDemo.ApiService.Models;

public sealed record FeatureInfo(string Name, bool Enabled, string Description, string Category = "Core");

public sealed record KillSwitchStatus(string Experiment, bool ExperimentDisabled, List<string> DisabledVariants);

public sealed record KillSwitchUpdate(string Experiment, string? Variant, bool Disabled);
