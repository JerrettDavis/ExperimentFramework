namespace ExperimentFramework.Selection;

/// <summary>
/// Factory for creating selection mode providers with access to scoped services.
/// </summary>
/// <remarks>
/// <para>
/// Provider factories are registered in the DI container and used by the framework
/// to create provider instances. This allows providers to access scoped services
/// like <c>IFeatureManagerSnapshot</c> or <c>HttpContext</c>.
/// </para>
/// <para>
/// External packages should implement this interface to register their selection modes:
/// <code>
/// services.AddSingleton&lt;ISelectionModeProviderFactory, OpenFeatureProviderFactory&gt;();
/// </code>
/// </para>
/// </remarks>
public interface ISelectionModeProviderFactory
{
    /// <summary>
    /// Gets the mode identifier this factory creates providers for.
    /// </summary>
    string ModeIdentifier { get; }

    /// <summary>
    /// Creates a provider instance with access to the scoped service provider.
    /// </summary>
    /// <param name="scopedProvider">The scoped service provider for the current invocation.</param>
    /// <returns>A provider instance configured with the scoped services.</returns>
    ISelectionModeProvider Create(IServiceProvider scopedProvider);
}
