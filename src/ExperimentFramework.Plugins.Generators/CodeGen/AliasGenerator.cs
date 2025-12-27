using System.Text;

namespace ExperimentFramework.Plugins.Generators.CodeGen;

/// <summary>
/// Generates aliases from class names using kebab-case convention.
/// </summary>
internal static class AliasGenerator
{
    private static readonly string[] SuffixesToStrip =
    [
        "Processor",
        "Handler",
        "Service",
        "Provider",
        "Impl",
        "Implementation"
    ];

    /// <summary>
    /// Generates a kebab-case alias from a class name.
    /// </summary>
    /// <param name="className">The class name (e.g., "StripeV2Processor").</param>
    /// <returns>A kebab-case alias (e.g., "stripe-v2").</returns>
    public static string GenerateAlias(string className)
    {
        var name = StripSuffixes(className);
        return ToKebabCase(name);
    }

    private static string StripSuffixes(string name)
    {
        bool found;
        do
        {
            found = false;
            foreach (var suffix in SuffixesToStrip)
            {
                if (name.EndsWith(suffix, System.StringComparison.Ordinal) && name.Length > suffix.Length)
                {
                    name = name.Substring(0, name.Length - suffix.Length);
                    found = true;
                    break; // Start over from the beginning of suffixes
                }
            }
        } while (found);

        return name;
    }

    private static string ToKebabCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var result = new StringBuilder();

        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];

            if (char.IsUpper(c))
            {
                if (i > 0)
                {
                    // Insert hyphen before uppercase letter if:
                    // 1. Previous char is lowercase, OR
                    // 2. Next char exists and is lowercase (handles "V2P" -> "v2-p")
                    var prevIsLower = char.IsLower(name[i - 1]);
                    var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);

                    if (prevIsLower || nextIsLower)
                    {
                        result.Append('-');
                    }
                }
                result.Append(char.ToLowerInvariant(c));
            }
            else if (char.IsDigit(c))
            {
                // Keep digits attached to previous chars
                result.Append(c);
            }
            else
            {
                result.Append(char.ToLowerInvariant(c));
            }
        }

        return result.ToString();
    }
}
