namespace ExperimentFramework.ServiceRegistration;

/// <summary>
/// Defines how the framework handles services with multiple registrations (IEnumerable&lt;T&gt;).
/// </summary>
public enum MultiRegistrationBehavior
{
    /// <summary>
    /// Replace matched descriptor(s) with proxy/router descriptor(s).
    /// </summary>
    /// <remarks>
    /// This is the default behavior. The original registration is removed and replaced
    /// with the experiment proxy. Use this for single-registration services.
    /// </remarks>
    Replace = 0,

    /// <summary>
    /// Insert descriptor before a matched one (preserve downstream ordering).
    /// </summary>
    /// <remarks>
    /// The experiment proxy is inserted before the first matching descriptor,
    /// preserving the order of subsequent registrations.
    /// </remarks>
    Insert = 1,

    /// <summary>
    /// Add descriptor after the matched one(s).
    /// </summary>
    /// <remarks>
    /// The experiment proxy is added after the last matching descriptor,
    /// preserving the order of previous registrations.
    /// </remarks>
    Append = 2,

    /// <summary>
    /// Merge multiple matches into one router/aggregate descriptor that preserves IEnumerable&lt;T&gt; semantics.
    /// </summary>
    /// <remarks>
    /// All matching descriptors are replaced with a single proxy that can route to any
    /// of the original implementations based on experiment configuration.
    /// </remarks>
    Merge = 3
}
