using System.Text;
using ExperimentFramework.Generators.Models;
using Microsoft.CodeAnalysis;

namespace ExperimentFramework.Generators.CodeGen;

/// <summary>
/// Generates trial selection helper methods based on selection mode.
/// </summary>
internal static class SelectionModeGenerator
{
    /// <summary>
    /// Generates the SelectTrialKey helper method based on the experiment's selection mode.
    /// </summary>
    /// <remarks>
    /// For built-in modes (BooleanFeatureFlag, ConfigurationValue), generates optimized inline code.
    /// For custom modes, generates delegation code to the runtime SelectionModeRegistry.
    /// </remarks>
    public static void GenerateSelectionHelper(StringBuilder sb, ExperimentDefinitionModel experiment)
    {
        switch (experiment.SelectionMode)
        {
            case SelectionModeModel.BooleanFeatureFlag:
                GenerateBooleanFeatureFlagSelector(sb, experiment);
                break;

            case SelectionModeModel.ConfigurationValue:
                GenerateConfigurationValueSelector(sb, experiment);
                break;

            case SelectionModeModel.Custom:
                GenerateCustomModeSelector(sb, experiment);
                break;
        }
    }

    private static void GenerateBooleanFeatureFlagSelector(StringBuilder sb, ExperimentDefinitionModel experiment)
    {
        var selectorName = experiment.SelectorName;
        var defaultKey = experiment.DefaultKey;

        sb.AppendLine("        private string SelectTrialKey(global::System.IServiceProvider sp)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Try IFeatureManagerSnapshot first (for request-scoped snapshots)");
        sb.AppendLine("            var snapshot = sp.GetService(typeof(global::Microsoft.FeatureManagement.IFeatureManagerSnapshot)) as global::Microsoft.FeatureManagement.IFeatureManagerSnapshot;");
        sb.AppendLine("            if (snapshot != null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                var task = snapshot.IsEnabledAsync(\"{selectorName}\");");
        sb.AppendLine("                var enabled = task.GetAwaiter().GetResult();");
        sb.AppendLine("                return enabled ? \"true\" : \"false\";");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // Fall back to IFeatureManager");
        sb.AppendLine("            var manager = sp.GetService(typeof(global::Microsoft.FeatureManagement.IFeatureManager)) as global::Microsoft.FeatureManagement.IFeatureManager;");
        sb.AppendLine("            if (manager != null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                var task = manager.IsEnabledAsync(\"{selectorName}\");");
        sb.AppendLine("                var enabled = task.GetAwaiter().GetResult();");
        sb.AppendLine("                return enabled ? \"true\" : \"false\";");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            return \"{defaultKey}\";");
        sb.AppendLine("        }");
    }

    private static void GenerateConfigurationValueSelector(StringBuilder sb, ExperimentDefinitionModel experiment)
    {
        var selectorName = experiment.SelectorName;
        var defaultKey = experiment.DefaultKey;

        sb.AppendLine("        private string SelectTrialKey(global::System.IServiceProvider sp)");
        sb.AppendLine("        {");
        sb.AppendLine("            var configuration = sp.GetService(typeof(global::Microsoft.Extensions.Configuration.IConfiguration)) as global::Microsoft.Extensions.Configuration.IConfiguration;");
        sb.AppendLine("            if (configuration != null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                var value = configuration[\"{selectorName}\"];");
        sb.AppendLine("                if (!string.IsNullOrEmpty(value))");
        sb.AppendLine("                {");
        sb.AppendLine("                    return value;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            return \"{defaultKey}\";");
        sb.AppendLine("        }");
    }

    private static void GenerateCustomModeSelector(StringBuilder sb, ExperimentDefinitionModel experiment)
    {
        var modeIdentifier = experiment.ModeIdentifier ?? "Unknown";
        var selectorName = experiment.SelectorName;
        var defaultKey = experiment.DefaultKey;
        var serviceTypeName = experiment.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        sb.AppendLine("        private string SelectTrialKey(global::System.IServiceProvider sp)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Delegate to runtime provider for custom selection mode");
        sb.AppendLine("            var registry = sp.GetService(typeof(global::ExperimentFramework.Selection.SelectionModeRegistry)) as global::ExperimentFramework.Selection.SelectionModeRegistry;");
        sb.AppendLine("            if (registry == null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                return \"{defaultKey}\";");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            var provider = registry.GetProvider(\"{modeIdentifier}\", sp);");
        sb.AppendLine("            if (provider == null)");
        sb.AppendLine("            {");
        sb.AppendLine($"                return \"{defaultKey}\";");
        sb.AppendLine("            }");
        sb.AppendLine();

        // Build selector name - use provided or delegate to provider
        if (!string.IsNullOrEmpty(selectorName))
        {
            sb.AppendLine($"            var selectorName = \"{selectorName}\";");
        }
        else
        {
            sb.AppendLine("            // Get default selector name from provider");
            sb.AppendLine("            var namingConvention = sp.GetService(typeof(global::ExperimentFramework.Naming.IExperimentNamingConvention)) as global::ExperimentFramework.Naming.IExperimentNamingConvention");
            sb.AppendLine("                ?? global::ExperimentFramework.Naming.DefaultExperimentNamingConvention.Instance;");
            sb.AppendLine($"            var selectorName = provider.GetDefaultSelectorName(typeof({serviceTypeName}), namingConvention);");
        }

        sb.AppendLine();
        sb.AppendLine("            // Build selection context");
        sb.AppendLine("            var context = new global::ExperimentFramework.Selection.SelectionContext");
        sb.AppendLine("            {");
        sb.AppendLine("                ServiceProvider = sp,");
        sb.AppendLine("                SelectorName = selectorName,");
        sb.AppendLine("                TrialKeys = _registration.Trials.Keys.ToList().AsReadOnly(),");
        sb.AppendLine($"                DefaultKey = \"{defaultKey}\",");
        sb.AppendLine($"                ServiceType = typeof({serviceTypeName})");
        sb.AppendLine("            };");
        sb.AppendLine();
        sb.AppendLine("            // Execute selection");
        sb.AppendLine("            var selectedKey = provider.SelectTrialKeyAsync(context).GetAwaiter().GetResult();");
        sb.AppendLine($"            return selectedKey ?? \"{defaultKey}\";");
        sb.AppendLine("        }");
    }
}
