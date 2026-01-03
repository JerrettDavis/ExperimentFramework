namespace AspireDemo.Web.Models;

/// <summary>
/// Model for the Create Experiment wizard form state.
/// </summary>
public class ExperimentWizardModel
{
    // Step 1: Basic Information
    public string Name { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "Engagement";

    // Step 2: Service & Variants
    public string ServiceInterface { get; set; } = "";
    public VariantModel Control { get; set; } = new();
    public List<VariantModel> Variants { get; set; } = [new()];

    // Step 3: Selection & Error Policy
    public SelectionModeType SelectionMode { get; set; } = SelectionModeType.ConfigurationKey;
    public string SelectionModeKey { get; set; } = "";
    public string CustomModeIdentifier { get; set; } = "";
    public ErrorPolicyType ErrorPolicy { get; set; } = ErrorPolicyType.FallbackToControl;
    public string FallbackKey { get; set; } = "";
    public List<string> FallbackOrder { get; set; } = [];

    /// <summary>
    /// Validates Step 1 fields.
    /// </summary>
    public (bool IsValid, List<string> Errors) ValidateStep1()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Experiment name is required");
        else if (!System.Text.RegularExpressions.Regex.IsMatch(Name, @"^[a-z0-9]+(-[a-z0-9]+)*$"))
            errors.Add("Name must be kebab-case (e.g., 'my-experiment')");

        if (string.IsNullOrWhiteSpace(DisplayName))
            errors.Add("Display name is required");

        if (string.IsNullOrWhiteSpace(Category))
            errors.Add("Category is required");

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Validates Step 2 fields.
    /// </summary>
    public (bool IsValid, List<string> Errors) ValidateStep2()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ServiceInterface))
            errors.Add("Service interface is required");

        if (string.IsNullOrWhiteSpace(Control.Key))
            errors.Add("Control variant key is required");

        if (string.IsNullOrWhiteSpace(Control.ImplementationType))
            errors.Add("Control implementation type is required");

        if (Variants.Count == 0)
            errors.Add("At least one additional variant is required");

        for (int i = 0; i < Variants.Count; i++)
        {
            var v = Variants[i];
            if (string.IsNullOrWhiteSpace(v.Key))
                errors.Add($"Variant {i + 1}: Key is required");
            if (string.IsNullOrWhiteSpace(v.ImplementationType))
                errors.Add($"Variant {i + 1}: Implementation type is required");
        }

        // Check for duplicate keys
        var allKeys = new List<string> { Control.Key };
        allKeys.AddRange(Variants.Select(v => v.Key));
        var duplicates = allKeys.Where(k => !string.IsNullOrEmpty(k))
            .GroupBy(k => k)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        foreach (var dup in duplicates)
            errors.Add($"Duplicate variant key: '{dup}'");

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Validates Step 3 fields.
    /// </summary>
    public (bool IsValid, List<string> Errors) ValidateStep3()
    {
        var errors = new List<string>();

        switch (SelectionMode)
        {
            case SelectionModeType.ConfigurationKey:
                if (string.IsNullOrWhiteSpace(SelectionModeKey))
                    errors.Add("Configuration key is required");
                break;
            case SelectionModeType.FeatureFlag:
                if (string.IsNullOrWhiteSpace(SelectionModeKey))
                    errors.Add("Feature flag name is required");
                break;
            case SelectionModeType.Custom:
                if (string.IsNullOrWhiteSpace(CustomModeIdentifier))
                    errors.Add("Custom mode identifier is required");
                break;
        }

        if (ErrorPolicy == ErrorPolicyType.FallbackTo && string.IsNullOrWhiteSpace(FallbackKey))
            errors.Add("Fallback key is required");

        if (ErrorPolicy == ErrorPolicyType.TryInOrder && FallbackOrder.Count == 0)
            errors.Add("At least one fallback key is required for ordered fallback");

        return (errors.Count == 0, errors);
    }

    /// <summary>
    /// Resets the model to default state.
    /// </summary>
    public void Reset()
    {
        Name = "";
        DisplayName = "";
        Description = "";
        Category = "Engagement";
        ServiceInterface = "";
        Control = new VariantModel();
        Variants = [new VariantModel()];
        SelectionMode = SelectionModeType.ConfigurationKey;
        SelectionModeKey = "";
        CustomModeIdentifier = "";
        ErrorPolicy = ErrorPolicyType.FallbackToControl;
        FallbackKey = "";
        FallbackOrder = [];
    }
}

/// <summary>
/// Model for a single variant/condition.
/// </summary>
public class VariantModel
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string ImplementationType { get; set; } = "";
}

/// <summary>
/// Selection mode types for experiment variant selection.
/// </summary>
public enum SelectionModeType
{
    ConfigurationKey,
    FeatureFlag,
    Custom
}

/// <summary>
/// Error policy types for experiment error handling.
/// </summary>
public enum ErrorPolicyType
{
    FallbackToControl,
    Throw,
    TryAny,
    FallbackTo,
    TryInOrder
}
