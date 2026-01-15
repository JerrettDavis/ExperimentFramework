using ExperimentFramework.Selection;

namespace ExperimentFramework.Testing;

/// <summary>
/// Factory for creating test selection providers.
/// </summary>
public sealed class TestSelectionProviderFactory : ISelectionModeProviderFactory
{
    /// <inheritdoc/>
    public string ModeIdentifier => TestSelectionProvider.ModeId;

    /// <inheritdoc/>
    public ISelectionModeProvider Create(IServiceProvider scopedProvider)
    {
        return new TestSelectionProvider();
    }
}
