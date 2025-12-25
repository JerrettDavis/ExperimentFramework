using System.Collections.Immutable;
using System.Text;
using ExperimentFramework.Generators.Models;

namespace ExperimentFramework.Generators.CodeGen;

/// <summary>
/// Generates error policy helper methods for building candidate trial keys.
/// </summary>
internal static class ErrorPolicyGenerator
{
    /// <summary>
    /// Generates the BuildCandidateKeys helper method based on the experiment's error policy.
    /// </summary>
    public static void GenerateErrorPolicyHelper(StringBuilder sb, ExperimentDefinitionModel experiment)
    {
        var defaultKey = experiment.DefaultKey;

        switch (experiment.ErrorPolicy)
        {
            case ErrorPolicyModel.Throw:
                GenerateThrowPolicy(sb);
                break;

            case ErrorPolicyModel.RedirectAndReplayDefault:
                GenerateRedirectAndReplayDefaultPolicy(sb, defaultKey);
                break;

            case ErrorPolicyModel.RedirectAndReplayAny:
                GenerateRedirectAndReplayAnyPolicy(sb, defaultKey);
                break;

            case ErrorPolicyModel.RedirectAndReplay:
                GenerateRedirectAndReplayPolicy(sb, experiment.FallbackTrialKey!);
                break;

            case ErrorPolicyModel.RedirectAndReplayOrdered:
                GenerateRedirectAndReplayOrderedPolicy(sb, experiment.OrderedFallbackKeys);
                break;
        }
    }

    private static void GenerateThrowPolicy(StringBuilder sb)
    {
        sb.AppendLine("        private System.Collections.Generic.List<string> BuildCandidateKeys(string preferredKey)");
        sb.AppendLine("        {");
        sb.AppendLine("            // Throw policy: Only try the preferred key, fail immediately on error");
        sb.AppendLine("            return new System.Collections.Generic.List<string> { preferredKey };");
        sb.AppendLine("        }");
    }

    private static void GenerateRedirectAndReplayDefaultPolicy(StringBuilder sb, string defaultKey)
    {
        sb.AppendLine("        private System.Collections.Generic.List<string> BuildCandidateKeys(string preferredKey)");
        sb.AppendLine("        {");
        sb.AppendLine("            // RedirectAndReplayDefault: Try preferred, then fall back to default");
        sb.AppendLine($"            if (preferredKey == \"{defaultKey}\")");
        sb.AppendLine("            {");
        sb.AppendLine("                return new System.Collections.Generic.List<string> { preferredKey };");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            return new System.Collections.Generic.List<string> {{ preferredKey, \"{defaultKey}\" }};");
        sb.AppendLine("        }");
    }

    private static void GenerateRedirectAndReplayAnyPolicy(StringBuilder sb, string defaultKey)
    {
        sb.AppendLine("        private System.Collections.Generic.List<string> BuildCandidateKeys(string preferredKey)");
        sb.AppendLine("        {");
        sb.AppendLine("            // RedirectAndReplayAny: Try all keys, starting with preferred");
        sb.AppendLine("            var candidates = new System.Collections.Generic.List<string>();");
        sb.AppendLine();
        sb.AppendLine("            // Add preferred key first");
        sb.AppendLine("            candidates.Add(preferredKey);");
        sb.AppendLine();
        sb.AppendLine("            // Add all other keys sorted alphabetically");
        sb.AppendLine("            var allKeys = _registration.Trials.Keys");
        sb.AppendLine("                .Where(k => k != preferredKey)");
        sb.AppendLine("                .OrderBy(k => k)");
        sb.AppendLine("                .ToList();");
        sb.AppendLine();
        sb.AppendLine("            candidates.AddRange(allKeys);");
        sb.AppendLine();
        sb.AppendLine("            return candidates;");
        sb.AppendLine("        }");
    }

    private static void GenerateRedirectAndReplayPolicy(StringBuilder sb, string fallbackTrialKey)
    {
        sb.AppendLine("        private System.Collections.Generic.List<string> BuildCandidateKeys(string preferredKey)");
        sb.AppendLine("        {");
        sb.AppendLine($"            // RedirectAndReplay: Try preferred, then fallback to specific trial '{fallbackTrialKey}'");
        sb.AppendLine($"            if (preferredKey == \"{fallbackTrialKey}\")");
        sb.AppendLine("            {");
        sb.AppendLine("                // If preferred is already the fallback, only try it once");
        sb.AppendLine("                return new System.Collections.Generic.List<string> { preferredKey };");
        sb.AppendLine("            }");
        sb.AppendLine();
        sb.AppendLine($"            return new System.Collections.Generic.List<string> {{ preferredKey, \"{fallbackTrialKey}\" }};");
        sb.AppendLine("        }");
    }

    private static void GenerateRedirectAndReplayOrderedPolicy(StringBuilder sb, ImmutableArray<string> orderedFallbackKeys)
    {
        sb.AppendLine("        private System.Collections.Generic.List<string> BuildCandidateKeys(string preferredKey)");
        sb.AppendLine("        {");
        sb.AppendLine("            // RedirectAndReplayOrdered: Try preferred, then ordered fallbacks");
        sb.AppendLine("            var candidates = new System.Collections.Generic.List<string> { preferredKey };");
        sb.AppendLine();
        sb.AppendLine("            // Add ordered fallbacks, skipping the preferred key if it appears");

        foreach (var key in orderedFallbackKeys)
        {
            sb.AppendLine($"            if (preferredKey != \"{key}\")");
            sb.AppendLine($"                candidates.Add(\"{key}\");");
        }

        sb.AppendLine();
        sb.AppendLine("            return candidates;");
        sb.AppendLine("        }");
    }
}
