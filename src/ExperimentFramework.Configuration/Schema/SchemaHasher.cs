using System.Text;

namespace ExperimentFramework.Configuration.Schema;

/// <summary>
/// Provides fast, deterministic hash computation for configuration schemas using the FNV-1a algorithm.
/// This is a non-cryptographic hash designed for speed and determinism, not security.
/// </summary>
public static class SchemaHasher
{
    // FNV-1a 64-bit constants
    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    /// <summary>
    /// Computes a deterministic hash of the given schema content using FNV-1a.
    /// The hash is stable across builds for identical input.
    /// </summary>
    /// <param name="content">The normalized schema content to hash.</param>
    /// <returns>A hexadecimal string representation of the hash.</returns>
    public static string ComputeHash(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return "0000000000000000";
        }

        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = ComputeFnv1aHash(bytes);
        return hash.ToString("x16"); // 16 hex characters for 64-bit hash
    }

    /// <summary>
    /// Computes a unified hash from multiple individual hashes.
    /// This is used to create a single hash representing the entire solution's schema.
    /// </summary>
    /// <param name="hashes">The collection of individual schema hashes, sorted deterministically.</param>
    /// <returns>A hexadecimal string representation of the unified hash.</returns>
    public static string ComputeUnifiedHash(IEnumerable<string> hashes)
    {
        // Sort hashes to ensure deterministic ordering
        var sortedHashes = hashes.OrderBy(h => h, StringComparer.Ordinal).ToList();
        
        // Concatenate all hashes with a delimiter
        var combined = string.Join("|", sortedHashes);
        
        return ComputeHash(combined);
    }

    /// <summary>
    /// Implements the FNV-1a (Fowler-Noll-Vo) 64-bit hash algorithm.
    /// </summary>
    /// <param name="data">The byte array to hash.</param>
    /// <returns>The 64-bit hash value.</returns>
    private static ulong ComputeFnv1aHash(byte[] data)
    {
        var hash = FnvOffsetBasis;

        foreach (var b in data)
        {
            hash ^= b;
            hash *= FnvPrime;
        }

        return hash;
    }

    /// <summary>
    /// Normalizes a schema object to a deterministic string representation for hashing.
    /// Properties are sorted alphabetically to ensure consistent ordering.
    /// </summary>
    /// <param name="schemaDefinition">The schema definition to normalize.</param>
    /// <returns>A normalized string representation suitable for hashing.</returns>
    public static string NormalizeSchema(SchemaDefinition schemaDefinition)
    {
        var sb = new StringBuilder();

        // Sort types by name for deterministic ordering
        var sortedTypes = schemaDefinition.Types
            .OrderBy(t => t.TypeName, StringComparer.Ordinal)
            .ToList();

        foreach (var type in sortedTypes)
        {
            sb.AppendLine($"TYPE:{type.TypeName}");
            sb.AppendLine($"NAMESPACE:{type.Namespace}");

            // Sort properties by name for deterministic ordering
            var sortedProperties = type.Properties
                .OrderBy(p => p.Name, StringComparer.Ordinal)
                .ToList();

            foreach (var prop in sortedProperties)
            {
                sb.AppendLine($"  PROP:{prop.Name}");
                sb.AppendLine($"  TYPE:{prop.TypeName}");
                sb.AppendLine($"  REQUIRED:{prop.IsRequired}");
                sb.AppendLine($"  NULLABLE:{prop.IsNullable}");
            }

            sb.AppendLine(); // Blank line between types
        }

        return sb.ToString().TrimEnd();
    }
}
