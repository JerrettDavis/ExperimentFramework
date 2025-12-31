using System.Text;
using AspireDemo.Web.Models;

namespace AspireDemo.Web.Services;

/// <summary>
/// Generates YAML DSL and Fluent API code from an ExperimentWizardModel.
/// </summary>
public class ExperimentCodeGenerator
{
    /// <summary>
    /// Generates YAML DSL configuration from the wizard model.
    /// </summary>
    public string GenerateYaml(ExperimentWizardModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine("experimentFramework:");
        sb.AppendLine("  experiments:");
        sb.AppendLine($"    - name: {model.Name}");

        // Metadata
        sb.AppendLine("      metadata:");
        sb.AppendLine($"        displayName: \"{EscapeYamlString(model.DisplayName)}\"");
        if (!string.IsNullOrWhiteSpace(model.Description))
            sb.AppendLine($"        description: \"{EscapeYamlString(model.Description)}\"");
        sb.AppendLine($"        category: \"{model.Category}\"");

        // Trials
        sb.AppendLine("      trials:");
        sb.AppendLine($"        - serviceType: {model.ServiceInterface}");

        // Selection mode
        sb.AppendLine("          selectionMode:");
        switch (model.SelectionMode)
        {
            case SelectionModeType.ConfigurationKey:
                sb.AppendLine("            type: configurationKey");
                sb.AppendLine($"            key: \"{model.SelectionModeKey}\"");
                break;
            case SelectionModeType.FeatureFlag:
                sb.AppendLine("            type: featureFlag");
                sb.AppendLine($"            flagName: \"{model.SelectionModeKey}\"");
                break;
            case SelectionModeType.Custom:
                sb.AppendLine("            type: custom");
                sb.AppendLine($"            modeIdentifier: \"{model.CustomModeIdentifier}\"");
                break;
        }

        // Control
        sb.AppendLine("          control:");
        sb.AppendLine($"            key: {model.Control.Key}");
        sb.AppendLine($"            implementationType: {model.Control.ImplementationType}");

        // Conditions
        if (model.Variants.Count > 0)
        {
            sb.AppendLine("          conditions:");
            foreach (var variant in model.Variants)
            {
                sb.AppendLine($"            - key: {variant.Key}");
                sb.AppendLine($"              implementationType: {variant.ImplementationType}");
            }
        }

        // Error policy
        sb.AppendLine("          errorPolicy:");
        switch (model.ErrorPolicy)
        {
            case ErrorPolicyType.FallbackToControl:
                sb.AppendLine("            type: fallbackToControl");
                break;
            case ErrorPolicyType.Throw:
                sb.AppendLine("            type: throw");
                break;
            case ErrorPolicyType.TryAny:
                sb.AppendLine("            type: tryAny");
                break;
            case ErrorPolicyType.FallbackTo:
                sb.AppendLine("            type: fallbackTo");
                sb.AppendLine($"            fallbackKey: {model.FallbackKey}");
                break;
            case ErrorPolicyType.TryInOrder:
                sb.AppendLine("            type: tryInOrder");
                sb.AppendLine("            fallbackKeys:");
                foreach (var key in model.FallbackOrder)
                    sb.AppendLine($"              - {key}");
                break;
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates Fluent API C# code from the wizard model.
    /// </summary>
    public string GenerateFluentApi(ExperimentWizardModel model)
    {
        var sb = new StringBuilder();

        sb.AppendLine("// Add this to your Program.cs or startup configuration");
        sb.AppendLine();
        sb.AppendLine("var experiments = ExperimentFrameworkBuilder.Create()");
        sb.AppendLine("    .UseDispatchProxy()");
        sb.AppendLine($"    .Experiment(\"{model.Name}\", exp => exp");

        // Metadata
        sb.AppendLine($"        .WithMetadata(\"displayName\", \"{EscapeCSharpString(model.DisplayName)}\")");
        if (!string.IsNullOrWhiteSpace(model.Description))
            sb.AppendLine($"        .WithMetadata(\"description\", \"{EscapeCSharpString(model.Description)}\")");
        sb.AppendLine($"        .WithMetadata(\"category\", \"{model.Category}\")");

        // Trial
        sb.AppendLine($"        .Trial<{model.ServiceInterface}>(trial => trial");

        // Selection mode
        switch (model.SelectionMode)
        {
            case SelectionModeType.ConfigurationKey:
                sb.AppendLine($"            .UsingConfigurationKey(\"{model.SelectionModeKey}\")");
                break;
            case SelectionModeType.FeatureFlag:
                sb.AppendLine($"            .UsingFeatureFlag(\"{model.SelectionModeKey}\")");
                break;
            case SelectionModeType.Custom:
                sb.AppendLine($"            .UsingCustomMode(\"{model.CustomModeIdentifier}\")");
                break;
        }

        // Control
        sb.AppendLine($"            .AddControl<{model.Control.ImplementationType}>(\"{model.Control.Key}\")");

        // Variants
        foreach (var variant in model.Variants)
        {
            sb.AppendLine($"            .AddVariant<{variant.ImplementationType}>(\"{variant.Key}\")");
        }

        // Error policy
        switch (model.ErrorPolicy)
        {
            case ErrorPolicyType.FallbackToControl:
                sb.AppendLine("            .OnErrorFallbackToControl()));");
                break;
            case ErrorPolicyType.Throw:
                sb.AppendLine("            .OnErrorThrow()));");
                break;
            case ErrorPolicyType.TryAny:
                sb.AppendLine("            .OnErrorTryAny()));");
                break;
            case ErrorPolicyType.FallbackTo:
                sb.AppendLine($"            .OnErrorFallbackTo(\"{model.FallbackKey}\")));");
                break;
            case ErrorPolicyType.TryInOrder:
                var keys = string.Join("\", \"", model.FallbackOrder);
                sb.AppendLine($"            .OnErrorTryInOrder(\"{keys}\")));");
                break;
        }

        sb.AppendLine();
        sb.AppendLine("builder.Services.AddExperimentFramework(experiments);");

        return sb.ToString();
    }

    private static string EscapeYamlString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

    private static string EscapeCSharpString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }
}
