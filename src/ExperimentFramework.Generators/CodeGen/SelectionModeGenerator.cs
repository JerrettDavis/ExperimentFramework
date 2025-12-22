using ExperimentFramework.Generators.Models;
using System.Text;

namespace ExperimentFramework.Generators.CodeGen;

/// <summary>
/// Generates trial selection helper methods based on selection mode.
/// </summary>
internal static class SelectionModeGenerator
{
    /// <summary>
    /// Generates the SelectTrialKey helper method based on the experiment's selection mode.
    /// </summary>
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

            case SelectionModeModel.VariantFeatureFlag:
                GenerateVariantFeatureFlagSelector(sb, experiment);
                break;

            case SelectionModeModel.StickyRouting:
                GenerateStickyRoutingSelector(sb, experiment);
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

    private static void GenerateVariantFeatureFlagSelector(StringBuilder sb, ExperimentDefinitionModel experiment)
    {
        var selectorName = experiment.SelectorName;
        var defaultKey = experiment.DefaultKey;

        sb.AppendLine("        private string SelectTrialKey(global::System.IServiceProvider sp)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Variant feature flags require reflection to access IVariantFeatureManager");
        sb.AppendLine("            // This is because Microsoft.FeatureManagement.FeatureVariants is a separate package");
        sb.AppendLine("            var variantManagerType = global::System.Type.GetType(\"Microsoft.FeatureManagement.IVariantFeatureManager, Microsoft.FeatureManagement\");");
        sb.AppendLine("            if (variantManagerType != null)");
        sb.AppendLine("            {");
        sb.AppendLine("                var variantManager = sp.GetService(variantManagerType);");
        sb.AppendLine("                if (variantManager != null)");
        sb.AppendLine("                {");
        sb.AppendLine("                    try");
        sb.AppendLine("                    {");
        sb.AppendLine("                        var getVariantMethod = variantManagerType.GetMethod(\"GetVariantAsync\");");
        sb.AppendLine("                        if (getVariantMethod != null)");
        sb.AppendLine("                        {");
        sb.AppendLine($"                            var task = getVariantMethod.Invoke(variantManager, new object[] {{ \"{selectorName}\", default(global::System.Threading.CancellationToken) }}) as global::System.Threading.Tasks.Task<object>;");
        sb.AppendLine("                            var variant = task?.GetAwaiter().GetResult();");
        sb.AppendLine("                            if (variant != null)");
        sb.AppendLine("                            {");
        sb.AppendLine("                                var nameProperty = variant.GetType().GetProperty(\"Name\");");
        sb.AppendLine("                                var variantName = nameProperty?.GetValue(variant) as string;");
        sb.AppendLine("                                if (!string.IsNullOrEmpty(variantName))");
        sb.AppendLine("                                {");
        sb.AppendLine("                                    return variantName;");
        sb.AppendLine("                                }");
        sb.AppendLine("                            }");
        sb.AppendLine("                        }");
        sb.AppendLine("                    }");
        sb.AppendLine("                    catch");
        sb.AppendLine("                    {");
        sb.AppendLine("                        // Fall through to default");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            return \"{defaultKey}\";");
        sb.AppendLine("        }");
    }

    private static void GenerateStickyRoutingSelector(StringBuilder sb, ExperimentDefinitionModel experiment)
    {
        var defaultKey = experiment.DefaultKey;

        sb.AppendLine("        private string SelectTrialKey(global::System.IServiceProvider sp)");
        sb.AppendLine("        {");
        sb.AppendLine("            var identityProvider = sp.GetService(typeof(global::ExperimentFramework.Routing.IExperimentIdentityProvider)) as global::ExperimentFramework.Routing.IExperimentIdentityProvider;");
        sb.AppendLine("            if (identityProvider == null || !identityProvider.TryGetIdentity(out var identity) || string.IsNullOrEmpty(identity))");
        sb.AppendLine("            {");
        sb.AppendLine($"                return \"{defaultKey}\";");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // Get all trial keys");
        sb.AppendLine("            var allKeys = _registration.Trials.Keys.ToArray();");
        sb.AppendLine("            if (allKeys.Length == 0)");
        sb.AppendLine("            {");
        sb.AppendLine($"                return \"{defaultKey}\";");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine("            // Compute hash-based index for deterministic routing");
        sb.AppendLine("            var hash = identity.GetHashCode();");
        sb.AppendLine("            var index = global::System.Math.Abs(hash % allKeys.Length);");
        sb.AppendLine("            return allKeys[index];");
        sb.AppendLine("        }");
    }
}
