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
        if (TestSelectionContext.TryGetForcedSelection(context.ServiceType, out var forcedKey))
        {
            // Validate that the forced key exists in available trial keys
            if (context.TrialKeys.Contains(forcedKey!))
            {
                // If selection is frozen and we haven't recorded this yet, record it
                if (TestSelectionContext.IsFrozen && 
                    (TestSelectionContext.FrozenSelections == null || 
                     !TestSelectionContext.FrozenSelections.ContainsKey(context.ServiceType)))
                {
                    TestSelectionContext.FreezeSelection(context.ServiceType, forcedKey!);
                }

                return ValueTask.FromResult<string?>(forcedKey);
            }
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
