using System.Reflection;
using ExperimentFramework.Decorators;
using ExperimentFramework.Models;
using ExperimentFramework.Telemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;

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
                var result = InvokeTrial(sp, pipeline, trialKey, targetMethod, args ?? Array.Empty<object?>());

                // Handle async methods
                if (targetMethod.ReturnType == typeof(Task))
                {
                    var task = (Task)result!;
                    task.GetAwaiter().GetResult();
                    telemetryScope.RecordSuccess();
                    return task;
                }
                else if (targetMethod.ReturnType.IsGenericType &&
                         targetMethod.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    var task = (Task)result!;
                    task.GetAwaiter().GetResult();
                    telemetryScope.RecordSuccess();
                    return task;
                }
                else if (targetMethod.ReturnType == typeof(ValueTask))
                {
                    var valueTask = (ValueTask)result!;
                    valueTask.GetAwaiter().GetResult();
                    telemetryScope.RecordSuccess();
                    // Wrap completed task in new ValueTask
                    return new ValueTask(Task.CompletedTask);
                }
                else if (targetMethod.ReturnType.IsGenericType &&
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
                else
                {
                    // Synchronous method
                    telemetryScope.RecordSuccess();
                    return result;
                }
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
            else if (result != null && result.GetType().IsGenericType &&
                     result.GetType().GetGenericTypeDefinition() == typeof(ValueTask<>))
            {
                // Convert ValueTask<T> to Task<T>
                var asTaskMethod = result.GetType().GetMethod("AsTask");
                var task2 = (Task)asTaskMethod!.Invoke(result, null)!;
                await task2.ConfigureAwait(false);

                var resultProperty = task2.GetType().GetProperty("Result");
                return resultProperty?.GetValue(task2);
            }
            else if (result is ValueTask valueTask)
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
            else if (targetMethod.ReturnType.IsGenericType &&
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
                    return genericFromResult.Invoke(null, new[] { boxedResult });
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
            else if (targetMethod.ReturnType == typeof(ValueTask))
            {
                return new ValueTask(Task.CompletedTask);
            }
            else if (targetMethod.ReturnType.IsGenericType &&
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
        => _registration!.Mode switch
        {
            SelectionMode.BooleanFeatureFlag => SelectBooleanFeatureFlag(sp),
            SelectionMode.ConfigurationValue => SelectConfigurationValue(sp),
            SelectionMode.VariantFeatureFlag => SelectVariantFeatureFlag(sp),
            SelectionMode.StickyRouting => SelectStickyRouting(sp),
            SelectionMode.OpenFeature => SelectOpenFeature(sp),
            _ => _registration.DefaultKey
        };

    private string SelectBooleanFeatureFlag(IServiceProvider sp)
    {
        // Try IFeatureManagerSnapshot first (for request-scoped snapshots)
        var snapshot = sp.GetService<IFeatureManagerSnapshot>();
        if (snapshot != null)
        {
            var enabled = snapshot.IsEnabledAsync(_registration!.SelectorName).GetAwaiter().GetResult();
            return enabled ? "true" : "false";
        }

        // Fall back to IFeatureManager
        var manager = sp.GetService<IFeatureManager>();
        if (manager != null)
        {
            var enabled = manager.IsEnabledAsync(_registration!.SelectorName).GetAwaiter().GetResult();
            return enabled ? "true" : "false";
        }

        return _registration!.DefaultKey;
    }

    private string SelectConfigurationValue(IServiceProvider sp)
    {
        var configuration = sp.GetService<IConfiguration>();
        if (configuration != null)
        {
            var value = configuration[_registration!.SelectorName];
            if (!string.IsNullOrEmpty(value))
                return value;
        }

        return _registration!.DefaultKey;
    }

    private string SelectVariantFeatureFlag(IServiceProvider sp)
    {
        // Use reflection to access IVariantFeatureManager since it's not in the core package
        var variantManagerType = Type.GetType("Microsoft.FeatureManagement.IVariantFeatureManager, Microsoft.FeatureManagement");
        if (variantManagerType != null)
        {
            var variantManager = sp.GetService(variantManagerType);
            if (variantManager != null)
            {
                var getVariantMethod = variantManagerType.GetMethod("GetVariantAsync");
                if (getVariantMethod != null)
                {
                    var task = (Task?)getVariantMethod.Invoke(variantManager, new object[] { _registration!.SelectorName, CancellationToken.None });
                    if (task != null)
                    {
                        task.GetAwaiter().GetResult();
                        var resultProperty = task.GetType().GetProperty("Result");
                        var variant = resultProperty?.GetValue(task);

                        if (variant != null)
                        {
                            var configurationProperty = variant.GetType().GetProperty("Configuration");
                            var configuration = configurationProperty?.GetValue(variant);

                            if (configuration != null)
                            {
                                var nameProperty = configuration.GetType().GetProperty("Name");
                                var name = nameProperty?.GetValue(configuration) as string;

                                if (!string.IsNullOrEmpty(name))
                                    return name;
                            }
                        }
                    }
                }
            }
        }

        return _registration!.DefaultKey;
    }

    private string SelectStickyRouting(IServiceProvider sp)
    {
        // Try to get identity provider
        var identityProviderType = Type.GetType("ExperimentFramework.Routing.IExperimentIdentityProvider, ExperimentFramework");
        if (identityProviderType != null)
        {
            var identityProvider = sp.GetService(identityProviderType);
            if (identityProvider != null)
            {
                var getIdentityMethod = identityProviderType.GetMethod("GetIdentity");
                if (getIdentityMethod != null)
                {
                    var identity = getIdentityMethod.Invoke(identityProvider, null) as string;
                    if (!string.IsNullOrEmpty(identity))
                    {
                        // Hash identity to select a trial
                        var hash = identity.GetHashCode();
                        var trialKeys = _registration!.Trials.Keys.OrderBy(k => k).ToArray();
                        var index = Math.Abs(hash) % trialKeys.Length;
                        return trialKeys[index];
                    }
                }
            }
        }

        // Fall back to boolean feature flag
        return SelectBooleanFeatureFlag(sp);
    }

    private string SelectOpenFeature(IServiceProvider sp)
    {
        // Use reflection to access OpenFeature API (soft dependency)
        var apiType = Type.GetType("OpenFeature.Api, OpenFeature");
        if (apiType == null)
            return _registration!.DefaultKey;

        // Get Api.Instance property
        var instanceProperty = apiType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceProperty == null)
            return _registration!.DefaultKey;

        var api = instanceProperty.GetValue(null);
        if (api == null)
            return _registration!.DefaultKey;

        // Get client via GetClient()
        var getClientMethod = apiType.GetMethod("GetClient", Type.EmptyTypes);
        if (getClientMethod == null)
            return _registration!.DefaultKey;

        var client = getClientMethod.Invoke(api, null);
        if (client == null)
            return _registration!.DefaultKey;

        var clientType = client.GetType();

        // Determine if we should use boolean or string evaluation based on trial keys
        var trialKeys = _registration!.Trials.Keys.ToList();
        var isBooleanFlag = trialKeys.Count == 2 &&
                            trialKeys.Contains("true") &&
                            trialKeys.Contains("false");

        if (isBooleanFlag)
        {
            // Use GetBooleanValueAsync for boolean flags
            var getBoolMethod = clientType.GetMethod("GetBooleanValueAsync",
                new[] { typeof(string), typeof(bool), typeof(CancellationToken) });

            if (getBoolMethod != null)
            {
                try
                {
                    var task = (Task<bool>?)getBoolMethod.Invoke(client,
                        new object[] { _registration.SelectorName, false, CancellationToken.None });

                    if (task != null)
                    {
                        var result = task.GetAwaiter().GetResult();
                        return result ? "true" : "false";
                    }
                }
                catch
                {
                    // Fall through to default
                }
            }
        }
        else
        {
            // Use GetStringValueAsync for multi-variant flags
            var getStringMethod = clientType.GetMethod("GetStringValueAsync",
                new[] { typeof(string), typeof(string), typeof(CancellationToken) });

            if (getStringMethod != null)
            {
                try
                {
                    var task = (Task<string>?)getStringMethod.Invoke(client,
                        new object[] { _registration.SelectorName, _registration.DefaultKey, CancellationToken.None });

                    if (task != null)
                    {
                        var result = task.GetAwaiter().GetResult();
                        if (!string.IsNullOrEmpty(result))
                            return result;
                    }
                }
                catch
                {
                    // Fall through to default
                }
            }
        }

        return _registration.DefaultKey;
    }

    private List<string> BuildCandidateKeys(string preferredKey)
    {
        switch (_registration!.OnErrorPolicy)
        {
            case OnErrorPolicy.Throw:
                return new List<string> { preferredKey };

            case OnErrorPolicy.RedirectAndReplayDefault:
                if (preferredKey == _registration.DefaultKey)
                {
                    return new List<string> { preferredKey };
                }
                return new List<string> { preferredKey, _registration.DefaultKey };

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
                    return new List<string> { preferredKey };
                }
                return new List<string> { preferredKey, _registration.FallbackTrialKey! };

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
                return new List<string> { preferredKey };
        }
    }
}
