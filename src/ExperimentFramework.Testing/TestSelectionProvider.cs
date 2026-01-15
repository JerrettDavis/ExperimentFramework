using ExperimentFramework.Naming;
using ExperimentFramework.Selection;

namespace ExperimentFramework.Testing;

/// <summary>
/// Selection mode provider for deterministic testing scenarios.
/// Supports forced control/condition selection and frozen selections.
/// </summary>
public sealed class TestSelectionProvider : ISelectionModeProvider
{
    /// <summary>
    /// Mode identifier for test selection.
    /// </summary>
    public const string ModeId = "Test";

    /// <inheritdoc/>
    public string ModeIdentifier => ModeId;

    /// <inheritdoc/>
    public ValueTask<string?> SelectTrialKeyAsync(SelectionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Check if there's a forced or frozen selection for this service type
        if (TestSelectionContext.TryGetForcedSelection(context.ServiceType, out var forcedKey) &&
            context.TrialKeys.Contains(forcedKey!))
        {
            string? selectedKey = forcedKey;

            if (TestSelectionContext.IsFrozen)
            {
                // If already frozen for this service type, use the existing frozen selection
                if (TestSelectionContext.FrozenSelections != null &&
                    TestSelectionContext.FrozenSelections.TryGetValue(context.ServiceType, out var frozenKey))
                {
                    selectedKey = frozenKey;
                }
                else
                {
                    // No frozen selection recorded yet; freeze the forced selection
                    TestSelectionContext.FreezeSelection(context.ServiceType, forcedKey!);
                }
            }

            return ValueTask.FromResult<string?>(selectedKey);
        }

        // Fall back to default (control)
        return ValueTask.FromResult<string?>(context.DefaultKey);
    }

    /// <inheritdoc/>
    public string GetDefaultSelectorName(Type serviceType, IExperimentNamingConvention convention)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(convention);

        // Use service type name as default selector name for tests
        return $"Test_{serviceType.Name}";
    }
}
