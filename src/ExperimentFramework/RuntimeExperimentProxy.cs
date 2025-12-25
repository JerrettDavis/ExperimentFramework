using System.Reflection;
using ExperimentFramework.Activation;
using ExperimentFramework.Decorators;
using ExperimentFramework.Models;
using ExperimentFramework.Naming;
using ExperimentFramework.Selection;
using ExperimentFramework.Telemetry;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework;

/// <summary>
/// DispatchProxy-based runtime proxy for experiment framework.
/// </summary>
/// <typeparam name="TService">The service interface being proxied.</typeparam>
/// <remarks>
/// <para>
/// This proxy uses reflection to dynamically intercept method calls and route them
/// to trial implementations based on experiment configuration.
/// </para>
/// <para>
/// <strong>Performance Note:</strong> Runtime proxies incur ~800ns overhead per call
/// compared to &lt;100ns for source-generated proxies. Use source generators when possible.
/// </para>
/// </remarks>
internal class RuntimeExperimentProxy<TService> : DispatchProxy
    where TService : class
{
    private IServiceScopeFactory? _scopeFactory;
    private ExperimentRegistration? _registration;
    private IExperimentDecoratorFactory[]? _decoratorFactories;
    private IExperimentTelemetry? _telemetry;

    /// <summary>
    /// Creates a new runtime proxy instance.
    /// </summary>
    public static TService Create(
        IServiceScopeFactory scopeFactory,
        ExperimentRegistration registration,
        IExperimentDecoratorFactory[] decoratorFactories,
        IExperimentTelemetry telemetry)
    {
        if (Create<TService, RuntimeExperimentProxy<TService>>() is not RuntimeExperimentProxy<TService> proxy)
            throw new InvalidOperationException($"Failed to create DispatchProxy for {typeof(TService).FullName}");

        proxy._scopeFactory = scopeFactory;
        proxy._registration = registration;
        proxy._decoratorFactories = decoratorFactories;
        proxy._telemetry = telemetry;

        return (proxy as TService)!;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        if (targetMethod == null)
            throw new ArgumentNullException(nameof(targetMethod));

        if (_scopeFactory == null || _registration == null || _decoratorFactories == null || _telemetry == null)
            throw new InvalidOperationException("Proxy not initialized");

        // Handle special methods (ToString, GetHashCode, Equals, etc.)
        if (targetMethod.DeclaringType == typeof(object))
        {
            return targetMethod.Invoke(this, args);
        }

        // Create scope for this invocation
        using var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        // Build decorator pipeline
        var pipeline = new DecoratorPipeline(_decoratorFactories, sp);

        // Select trial key based on mode
        var preferredKey = SelectTrialKey(sp);

        // Build candidate keys based on error policy
        var candidates = BuildCandidateKeys(preferredKey);

        // Create telemetry scope
        using var telemetryScope = _telemetry.StartInvocation(
            typeof(TService),
            targetMethod.Name,
            _registration.SelectorName,
            preferredKey,
            candidates);

        Exception? lastException = null;

        // Try each candidate in order
        foreach (var trialKey in candidates)
        {
            try
            {
                var result = InvokeTrial(sp, pipeline, trialKey, targetMethod, args ?? []);

                // Handle async methods
                if (targetMethod.ReturnType == typeof(Task))
                {
                    var task = (Task)result!;
                    task.GetAwaiter().GetResult();
                    telemetryScope.RecordSuccess();
                    return task;
                }

                if (targetMethod.ReturnType.IsGenericType &&
                    targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var task = (Task)result!;
                    task.GetAwaiter().GetResult();
                    telemetryScope.RecordSuccess();
                    return task;
                }

                if (targetMethod.ReturnType == typeof(ValueTask))
                {
                    var valueTask = (ValueTask)result!;
                    valueTask.GetAwaiter().GetResult();
                    telemetryScope.RecordSuccess();
                    // Wrap completed task in new ValueTask
                    return new ValueTask(Task.CompletedTask);
                }

                if (targetMethod.ReturnType.IsGenericType &&
                    targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    // Get the result using reflection
                    var valueTaskType = result!.GetType();
                    var asTaskMethod = valueTaskType.GetMethod("AsTask");
                    var task = (Task)asTaskMethod!.Invoke(result, null)!;
                    task.GetAwaiter().GetResult();
                    telemetryScope.RecordSuccess();

                    // Get the Result property
                    var resultProperty = task.GetType().GetProperty("Result");
                    var taskResult = resultProperty!.GetValue(task);

                    // Create new ValueTask<T> with the result
                    var valueTaskResultType = typeof(ValueTask<>).MakeGenericType(targetMethod.ReturnType.GetGenericArguments()[0]);
                    return Activator.CreateInstance(valueTaskResultType, taskResult);
                }

                // Synchronous method
                telemetryScope.RecordSuccess();
                return result;
            }
            catch (Exception ex)
            {
                lastException = ex;

                // If throw policy, propagate immediately
                if (_registration.OnErrorPolicy == OnErrorPolicy.Throw)
                {
                    telemetryScope.RecordFailure(ex);
                    throw;
                }

                // Otherwise, continue to next candidate
            }
        }

        // All candidates failed
        telemetryScope.RecordFailure(lastException!);
        throw lastException!;
    }

    private object? InvokeTrial(
        IServiceProvider sp,
        DecoratorPipeline pipeline,
        string trialKey,
        MethodInfo targetMethod,
        object?[] args)
    {
        // Resolve trial implementation
        var implType = _registration!.Trials.TryGetValue(trialKey, out var t)
            ? t
            : _registration.Trials[_registration.DefaultKey];

        var impl = sp.GetRequiredService(implType);

        // Execute through decorator pipeline
        async ValueTask<object?> Terminal()
        {
            // Invoke the method on the implementation
            var result = targetMethod.Invoke(impl, args);

            // Handle async results
            if (result is Task task)
            {
                await task.ConfigureAwait(false);

                // For Task<T>, extract the result
                if (task.GetType().IsGenericType)
                {
                    var resultProperty = task.GetType().GetProperty("Result");
                    return resultProperty?.GetValue(task);
                }

                return null; // Task (non-generic) returns void
            }

            if (result != null && result.GetType().IsGenericType &&
                result.GetType().GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                // Convert ValueTask<T> to Task<T>
                var asTaskMethod = result.GetType().GetMethod("AsTask");
                var task2 = (Task)asTaskMethod!.Invoke(result, null)!;
                await task2.ConfigureAwait(false);

                var resultProperty = task2.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task2);
            }

            if (result is ValueTask valueTask)
            {
                await valueTask.ConfigureAwait(false);
                return null;
            }

            return result;
        }

        var ctx = new InvocationContext(
            typeof(TService),
            targetMethod.Name,
            trialKey,
            args);

        var boxedResult = pipeline.InvokeAsync(ctx, Terminal).GetAwaiter().GetResult();

        // Return the original task/value task if needed
        if (targetMethod.ReturnType == typeof(Task) || targetMethod.ReturnType.IsGenericType)
        {
            if (targetMethod.ReturnType == typeof(Task))
            {
                return Task.CompletedTask;
            }

            if (targetMethod.ReturnType.IsGenericType &&
                targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = targetMethod.ReturnType.GetGenericArguments()[0];
                var fromResultMethod = typeof(Task).GetMethod("FromResult", BindingFlags.Public | BindingFlags.Static);
                if (fromResultMethod == null)
                {
                    throw new InvalidOperationException("Could not find Task.FromResult method");
                }

                try
                {
                    var genericFromResult = fromResultMethod.MakeGenericMethod(resultType);
                    return genericFromResult.Invoke(null, [boxedResult]);
                }
                catch (TargetInvocationException ex)
                {
                    throw new InvalidOperationException(
                        $"Error invoking Task.FromResult<{resultType.Name}>. See inner exception for details.",
                        ex.InnerException ?? ex);
                }
                catch (ArgumentException ex)
                {
                    throw new InvalidOperationException(
                        $"Error creating Task.FromResult<{resultType.Name}>: incompatible type arguments.",
                        ex);
                }
            }

            if (targetMethod.ReturnType == typeof(ValueTask))
            {
                return new ValueTask(Task.CompletedTask);
            }

            if (targetMethod.ReturnType.IsGenericType &&
                targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                var resultType = targetMethod.ReturnType.GetGenericArguments()[0];
                var valueTaskType = typeof(ValueTask<>).MakeGenericType(resultType);
                return Activator.CreateInstance(valueTaskType, boxedResult);
            }
        }

        return boxedResult;
    }

    private string SelectTrialKey(IServiceProvider sp)
    {
        // Check if the trial is active based on time bounds and predicates
        // If not active, immediately return the control (default) key
        if (!IsTrialActive(sp))
        {
            return _registration!.DefaultKey;
        }

        // Use the provider-based registry for selection
        var registry = sp.GetService<SelectionModeRegistry>();
        if (registry == null)
        {
            // No registry available, fall back to default
            return _registration!.DefaultKey;
        }

        var provider = registry.GetProvider(_registration!.ModeIdentifier, sp);
        if (provider == null)
        {
            // Provider not found for this mode, fall back to default
            return _registration.DefaultKey;
        }

        // Get the naming convention for default selector name derivation
        var namingConvention = sp.GetService<IExperimentNamingConvention>()
            ?? DefaultExperimentNamingConvention.Instance;

        // Use provider's default naming if no selector name was specified
        var selectorName = string.IsNullOrEmpty(_registration.SelectorName)
            ? provider.GetDefaultSelectorName(_registration.ServiceType, namingConvention)
            : _registration.SelectorName;

        // Build the selection context
        var context = new SelectionContext
        {
            ServiceProvider = sp,
            SelectorName = selectorName,
            TrialKeys = _registration.Trials.Keys.ToList().AsReadOnly(),
            DefaultKey = _registration.DefaultKey,
            ServiceType = _registration.ServiceType
        };

        // Execute selection asynchronously
        var selectedKey = provider.SelectTrialKeyAsync(context).GetAwaiter().GetResult();

        return selectedKey ?? _registration.DefaultKey;
    }

    private bool IsTrialActive(IServiceProvider sp)
    {
        // If no activation constraints, the trial is always active
        if (_registration!.StartTime == null &&
            _registration.EndTime == null &&
            _registration.ActivationPredicate == null)
        {
            return true;
        }

        // Get or create the time provider
        var timeProvider = sp.GetService<IExperimentTimeProvider>() ?? SystemTimeProvider.Instance;
        var evaluator = new ActivationEvaluator(timeProvider, sp);

        return evaluator.IsActive(_registration);
    }

    private List<string> BuildCandidateKeys(string preferredKey)
    {
        switch (_registration!.OnErrorPolicy)
        {
            case OnErrorPolicy.Throw:
                return [preferredKey];

            case OnErrorPolicy.RedirectAndReplayDefault:
                if (preferredKey == _registration.DefaultKey)
                {
                    return [preferredKey];
                }
                return [preferredKey, _registration.DefaultKey];

            case OnErrorPolicy.RedirectAndReplayAny:
                var candidates = new List<string> { preferredKey };
                var allKeys = _registration.Trials.Keys
                    .Where(k => k != preferredKey)
                    .OrderBy(k => k)
                    .ToList();
                candidates.AddRange(allKeys);
                return candidates;

            case OnErrorPolicy.RedirectAndReplay:
                if (preferredKey == _registration.FallbackTrialKey)
                {
                    return [preferredKey];
                }
                return [preferredKey, _registration.FallbackTrialKey!];

            case OnErrorPolicy.RedirectAndReplayOrdered:
                var orderedCandidates = new List<string> { preferredKey };
                foreach (var key in _registration.OrderedFallbackKeys!)
                {
                    if (key != preferredKey)
                    {
                        orderedCandidates.Add(key);
                    }
                }
                return orderedCandidates;

            default:
                return [preferredKey];
        }
    }
}
