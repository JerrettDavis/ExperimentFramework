using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.ServiceRegistration;

/// <summary>
/// Executes registration plans and applies mutations to IServiceCollection.
/// </summary>
public sealed class RegistrationPlanExecutor
{
    /// <summary>
    /// Executes a registration plan and applies all operations to the service collection.
    /// </summary>
    /// <param name="plan">The plan to execute.</param>
    /// <param name="services">The service collection to mutate.</param>
    /// <param name="dryRun">If true, validates the plan but does not apply mutations.</param>
    /// <returns>An execution result indicating success or failure.</returns>
    public static PlanExecutionResult Execute(
        RegistrationPlan plan,
        IServiceCollection services,
        bool dryRun = false)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(services);

        // Check if plan is valid
        if (!plan.IsValid)
        {
            return PlanExecutionResult.CreateValidationFailed(
                plan.PlanId,
                "Plan validation failed. Review the validation findings before execution.",
                plan.Findings);
        }

        // If dry run, just return success without applying changes
        if (dryRun)
        {
            return PlanExecutionResult.CreateDryRunSuccess(plan.PlanId, plan.Operations.Count);
        }

        var appliedOperations = new List<OperationResult>();
        var rollbackOperations = new List<Action>();

        try
        {
            // Execute each operation in order
            foreach (var operation in plan.Operations)
            {
                // Capture state before operation for potential rollback
                var beforeState = services.ToArray();

                var result = operation.Execute(services);
                appliedOperations.Add(result);

                if (!result.Success)
                {
                    // Operation failed - rollback
                    RollbackOperations(services, rollbackOperations);

                    return PlanExecutionResult.CreateOperationFailed(
                        plan.PlanId,
                        operation.OperationId,
                        result.ErrorMessage ?? "Operation failed without error message",
                        appliedOperations);
                }

                // Add rollback action
                rollbackOperations.Add(() => RestoreServiceCollection(services, beforeState));
            }

            return PlanExecutionResult.CreateSuccess(plan.PlanId, appliedOperations);
        }
        catch (Exception ex)
        {
            // Unexpected error - attempt rollback
            RollbackOperations(services, rollbackOperations);

            return PlanExecutionResult.CreateUnexpectedError(
                plan.PlanId,
                $"Unexpected error during plan execution: {ex.Message}",
                appliedOperations,
                ex);
        }
    }

    /// <summary>
    /// Executes rollback operations in reverse order.
    /// </summary>
    private static void RollbackOperations(IServiceCollection services, List<Action> rollbackOperations)
    {
        // Execute rollback in reverse order
        for (int i = rollbackOperations.Count - 1; i >= 0; i--)
        {
            try
            {
                rollbackOperations[i]();
            }
            catch (Exception ex)
            {
                // Rollback failed - this is a critical error but we can't do much about it
                // Continue with other rollback operations and report the error
                Console.Error.WriteLine($"Rollback operation {i} failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Restores service collection to a previous state.
    /// </summary>
    private static void RestoreServiceCollection(IServiceCollection services, ServiceDescriptor[] previousState)
    {
        services.Clear();
        foreach (var descriptor in previousState)
        {
            services.Add(descriptor);
        }
    }
}

/// <summary>
/// Represents the result of executing a registration plan.
/// </summary>
public sealed class PlanExecutionResult
{
    /// <summary>
    /// Gets the plan identifier.
    /// </summary>
    public string PlanId { get; }

    /// <summary>
    /// Gets a value indicating whether execution succeeded.
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// Gets a value indicating whether this was a dry run (no mutations applied).
    /// </summary>
    public bool IsDryRun { get; }

    /// <summary>
    /// Gets the error message if execution failed.
    /// </summary>
    public string? ErrorMessage { get; }

    /// <summary>
    /// Gets the operation results from executed operations.
    /// </summary>
    public IReadOnlyList<OperationResult> OperationResults { get; }

    /// <summary>
    /// Gets the validation findings if validation failed.
    /// </summary>
    public IReadOnlyList<ValidationFinding>? ValidationFindings { get; }

    /// <summary>
    /// Gets the exception if an unexpected error occurred.
    /// </summary>
    public Exception? Exception { get; }

    private PlanExecutionResult(
        string planId,
        bool success,
        bool isDryRun,
        string? errorMessage,
        IReadOnlyList<OperationResult> operationResults,
        IReadOnlyList<ValidationFinding>? validationFindings = null,
        Exception? exception = null)
    {
        PlanId = planId;
        Success = success;
        IsDryRun = isDryRun;
        ErrorMessage = errorMessage;
        OperationResults = operationResults;
        ValidationFindings = validationFindings;
        Exception = exception;
    }

    /// <summary>
    /// Creates a successful execution result.
    /// </summary>
    public static PlanExecutionResult CreateSuccess(string planId, IReadOnlyList<OperationResult> operationResults)
        => new(planId, true, false, null, operationResults);

    /// <summary>
    /// Creates a dry run success result.
    /// </summary>
    public static PlanExecutionResult CreateDryRunSuccess(string planId, int operationCount)
        => new(planId, true, true, null, Array.Empty<OperationResult>());

    /// <summary>
    /// Creates a validation failed result.
    /// </summary>
    public static PlanExecutionResult CreateValidationFailed(
        string planId,
        string errorMessage,
        IReadOnlyList<ValidationFinding> findings)
        => new(planId, false, false, errorMessage, Array.Empty<OperationResult>(), findings);

    /// <summary>
    /// Creates an operation failed result.
    /// </summary>
    public static PlanExecutionResult CreateOperationFailed(
        string planId,
        string operationId,
        string errorMessage,
        IReadOnlyList<OperationResult> operationResults)
        => new(planId, false, false, $"Operation {operationId} failed: {errorMessage}", operationResults);

    /// <summary>
    /// Creates an unexpected error result.
    /// </summary>
    public static PlanExecutionResult CreateUnexpectedError(
        string planId,
        string errorMessage,
        IReadOnlyList<OperationResult> operationResults,
        Exception exception)
        => new(planId, false, false, errorMessage, operationResults, exception: exception);
}
