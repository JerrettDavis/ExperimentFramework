using System.Security.Cryptography;
using System.Text;

namespace ExperimentFramework.StickyRouting;

/// <summary>
/// Routes trial selection using deterministic hashing based on user/session identity.
/// </summary>
/// <remarks>
/// <para>
/// This router provides consistent trial assignment for A/B testing by hashing the combination
/// of user identity and selector name, then mapping the hash to a trial key via modulo arithmetic.
/// </para>
/// <para>
/// The same identity and selector name will always produce the same trial key, ensuring users
/// have a consistent experience across sessions.
/// </para>
/// <para>
/// Hash algorithm: SHA256 (stable, cross-platform, collision-resistant)
/// </para>
/// </remarks>
public static class StickyTrialRouter
{
    /// <summary>
    /// Selects a trial key using sticky routing based on identity hash.
    /// </summary>
    /// <param name="identity">The user/session identity (must be stable).</param>
    /// <param name="selectorName">The selector name (used as salt to prevent cross-experiment bleeding).</param>
    /// <param name="trialKeys">Available trial keys (order-independent due to sorting).</param>
    /// <returns>The deterministically selected trial key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="trialKeys"/> is empty.</exception>
    /// <remarks>
    /// <para>
    /// Trial keys are sorted alphabetically before hashing to ensure deterministic selection
    /// regardless of the order they were registered.
    /// </para>
    /// <para>
    /// Hash input format: <c>"{identity}:{selectorName}"</c>
    /// </para>
    /// <para>
    /// Distribution: Uses modulo arithmetic to map hash values to trial indices. With a good hash
    /// function (SHA256), distribution across trials should be approximately uniform.
    /// </para>
    /// </remarks>
    public static string SelectTrial(string identity, string selectorName, IReadOnlyList<string> trialKeys)
    {
        if (trialKeys.Count == 0)
            throw new InvalidOperationException("No trial keys available for sticky routing.");

        if (trialKeys.Count == 1)
            return trialKeys[0];

        // Sort keys alphabetically for deterministic ordering
        var sortedKeys = trialKeys.OrderBy(k => k, StringComparer.Ordinal).ToArray();

        // Hash: identity + ":" + selectorName
        var input = $"{identity}:{selectorName}";
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        // Use first 4 bytes as uint for modulo
        var hashValue = BitConverter.ToUInt32(hashBytes, 0);
        var index = (int)(hashValue % (uint)sortedKeys.Length);

        return sortedKeys[index];
    }
}
