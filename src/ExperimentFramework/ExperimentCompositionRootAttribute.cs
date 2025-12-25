namespace ExperimentFramework;

/// <summary>
/// Marks a method as an experiment composition root, triggering source generator
/// analysis to create compile-time proxies for all experiments defined within.
/// </summary>
/// <remarks>
/// <para>
/// The decorated method should return an <see cref="ExperimentFrameworkBuilder"/>
/// and contain all .Define&lt;T&gt; calls for the application's experiments.
/// </para>
/// <para>
/// This attribute provides an alternative to the fluent .UseSourceGenerators() API
/// for triggering compile-time proxy generation.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [ExperimentCompositionRoot]
/// public static ExperimentFrameworkBuilder ConfigureExperiments()
/// {
///     return ExperimentFrameworkBuilder.Create()
///         .Define&lt;IMyDatabase&gt;(c =&gt; c.UsingFeatureFlag("UseCloudDb")
///             .AddDefaultTrial&lt;MyDbContext&gt;("false")
///             .AddTrial&lt;MyCloudDbContext&gt;("true"))
///         .Define&lt;IMyTaxProvider&gt;(c =&gt; c.UsingConfigurationKey("TaxProvider")
///             .AddDefaultTrial&lt;DefaultTaxProvider&gt;("")
///             .AddTrial&lt;OkTaxProvider&gt;("OK"));
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class ExperimentCompositionRootAttribute : Attribute
{
}
