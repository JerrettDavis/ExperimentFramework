using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.ServiceRegistration;

/// <summary>
/// Represents a canonical patch operation that can be applied to an IServiceCollection.
/// </summary>
/// <remarks>
/// <para>
/// Each operation defines:
/// </para>
/// <list type="bullet">
/// <item><description>A match predicate to find target descriptors</description></item>
/// <item><description>The operation type (Replace/Insert/Append/Merge)</description></item>
/// <item><description>Expected result cardinality</description></item>
/// <item><description>Rollback strategy if validation fails</description></item>
/// </list>
/// </remarks>
public sealed class ServiceGraphPatchOperation
{
    /// <summary>
    /// Gets the unique identifier for this operation.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    /// Gets the type of operation to perform.
    /// </summary>
    public MultiRegistrationBehavior OperationType { get; }

    /// <summary>
    /// Gets the service type being mutated.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the match predicate used to find target descriptors.
    /// </summary>
    public Func<ServiceDescriptor, bool> MatchPredicate { get; }

    /// <summary>
    /// Gets the new descriptor(s) to apply.
    /// </summary>
    public IReadOnlyList<ServiceDescriptor> NewDescriptors { get; }

    /// <summary>
    /// Gets the expected number of matches. Null means any number is acceptable.
    /// </summary>
    public int? ExpectedMatchCount { get; }

    /// <summary>
    /// Gets a value indicating whether this operation allows no matches.
    /// </summary>
    public bool AllowNoMatches { get; }

    /// <summary>
    /// Gets metadata about this operation for auditing and reporting.
    /// </summary>
    public OperationMetadata Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceGraphPatchOperation"/> class.
    /// </summary>
    public ServiceGraphPatchOperation(
        string operationId,
        MultiRegistrationBehavior operationType,
        Type serviceType,
        Func<ServiceDescriptor, bool> matchPredicate,
        IReadOnlyList<ServiceDescriptor> newDescriptors,
        int? expectedMatchCount = null,
        bool allowNoMatches = false,
        OperationMetadata? metadata = null)
    {
        OperationId = operationId ?? throw new ArgumentNullException(nameof(operationId));
        OperationType = operationType;
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        MatchPredicate = matchPredicate ?? throw new ArgumentNullException(nameof(matchPredicate));
        NewDescriptors = newDescriptors ?? throw new ArgumentNullException(nameof(newDescriptors));
        ExpectedMatchCount = expectedMatchCount;
        AllowNoMatches = allowNoMatches;
        Metadata = metadata ?? new OperationMetadata(serviceType.FullName ?? serviceType.Name);
    }

    /// <summary>
    /// Executes the patch operation on the service collection.
    /// </summary>
    /// <param name="services">The service collection to mutate.</param>
    /// <returns>A result indicating success or failure and the changes made.</returns>
    public OperationResult Execute(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var matchedDescriptors = services.Where(MatchPredicate).ToList();
        var matchCount = matchedDescriptors.Count;

        // Validate match count expectations
        if (ExpectedMatchCount.HasValue && matchCount != ExpectedMatchCount.Value)
        {
            return OperationResult.CreateFailure(
                OperationId,
                $"Expected {ExpectedMatchCount.Value} matches but found {matchCount} for service type {ServiceType.FullName}");
        }

        if (!AllowNoMatches && matchCount == 0)
        {
            return OperationResult.CreateFailure(
                OperationId,
                $"No matching descriptors found for service type {ServiceType.FullName}");
        }

        // Apply the operation based on type
        var removedDescriptors = new List<ServiceDescriptor>();
        var addedDescriptors = new List<ServiceDescriptor>();

        switch (OperationType)
        {
            case MultiRegistrationBehavior.Replace:
                foreach (var descriptor in matchedDescriptors)
                {
                    services.Remove(descriptor);
                    removedDescriptors.Add(descriptor);
                }
                foreach (var newDescriptor in NewDescriptors)
                {
                    services.Add(newDescriptor);
                    addedDescriptors.Add(newDescriptor);
                }
                break;

            case MultiRegistrationBehavior.Insert:
                if (matchedDescriptors.Count > 0)
                {
                    var firstMatch = matchedDescriptors[0];
                    var index = services.IndexOf(firstMatch);
                    // Insert in reverse order so that when all insertions are complete,
                    // the new descriptors appear in their original order before the matched descriptor
                    foreach (var newDescriptor in NewDescriptors.Reverse())
                    {
                        services.Insert(index, newDescriptor);
                        addedDescriptors.Add(newDescriptor);
                    }
                }
                else if (AllowNoMatches)
                {
                    // If no matches and allowed, just add at the end
                    foreach (var newDescriptor in NewDescriptors)
                    {
                        services.Add(newDescriptor);
                        addedDescriptors.Add(newDescriptor);
                    }
                }
                break;

            case MultiRegistrationBehavior.Append:
                if (matchedDescriptors.Count > 0)
                {
                    var lastMatch = matchedDescriptors[^1];
                    var index = services.IndexOf(lastMatch);
                    foreach (var newDescriptor in NewDescriptors)
                    {
                        services.Insert(index + 1, newDescriptor);
                        addedDescriptors.Add(newDescriptor);
                        index++;
                    }
                }
                else if (AllowNoMatches)
                {
                    // If no matches and allowed, just add at the end
                    foreach (var newDescriptor in NewDescriptors)
                    {
                        services.Add(newDescriptor);
                        addedDescriptors.Add(newDescriptor);
                    }
                }
                break;

            case MultiRegistrationBehavior.Merge:
                // Remove all matches
                foreach (var descriptor in matchedDescriptors)
                {
                    services.Remove(descriptor);
                    removedDescriptors.Add(descriptor);
                }
                // Add merged descriptor
                foreach (var newDescriptor in NewDescriptors)
                {
                    services.Add(newDescriptor);
                    addedDescriptors.Add(newDescriptor);
                }
                break;

            default:
                return OperationResult.CreateFailure(
                    OperationId,
                    $"Unknown operation type: {OperationType}");
        }

        return OperationResult.CreateSuccess(
            OperationId,
            matchCount,
            removedDescriptors,
            addedDescriptors);
    }
}

/// <summary>
/// Metadata about a patch operation for auditing and reporting.
/// </summary>
public sealed class OperationMetadata
{
    /// <summary>
    /// Gets a human-readable description of this operation.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Gets additional context properties for this operation.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Properties { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationMetadata"/> class.
    /// </summary>
    public OperationMetadata(string description, IReadOnlyDictionary<string, string>? properties = null)
    {
        Description = description ?? throw new ArgumentNullException(nameof(description));
        Properties = properties;
    }
}

/// <summary>
/// Represents the result of executing a patch operation.
/// </summary>
public sealed class OperationResult
{
    /// <summary>
    /// Gets the operation identifier.
    /// </summary>
    public string OperationId { get; }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the number of descriptors that matched the predicate.
    /// </summary>
    public int MatchCount { get; }

    /// <summary>
    /// Gets the descriptors that were removed.
    /// </summary>
    public IReadOnlyList<ServiceDescriptor> RemovedDescriptors { get; }

    /// <summary>
    /// Gets the descriptors that were added.
    /// </summary>
    public IReadOnlyList<ServiceDescriptor> AddedDescriptors { get; }

    private OperationResult(
        string operationId,
        bool success,
        string? errorMessage,
        int matchCount,
        IReadOnlyList<ServiceDescriptor> removedDescriptors,
        IReadOnlyList<ServiceDescriptor> addedDescriptors)
    {
        OperationId = operationId;
        Success = success;
        ErrorMessage = errorMessage;
        MatchCount = matchCount;
        RemovedDescriptors = removedDescriptors;
        AddedDescriptors = addedDescriptors;
    }

    /// <summary>
    /// Creates a successful operation result.
    /// </summary>
    public static OperationResult CreateSuccess(
        string operationId,
        int matchCount,
        IReadOnlyList<ServiceDescriptor> removedDescriptors,
        IReadOnlyList<ServiceDescriptor> addedDescriptors)
    {
        return new OperationResult(
            operationId,
            true,
            null,
            matchCount,
            removedDescriptors,
            addedDescriptors);
    }

    /// <summary>
    /// Creates a failed operation result.
    /// </summary>
    public static OperationResult CreateFailure(string operationId, string errorMessage)
    {
        return new OperationResult(
            operationId,
            false,
            errorMessage,
            0,
            Array.Empty<ServiceDescriptor>(),
            Array.Empty<ServiceDescriptor>());
    }
}
