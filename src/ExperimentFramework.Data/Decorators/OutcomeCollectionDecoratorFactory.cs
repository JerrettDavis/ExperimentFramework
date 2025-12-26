using System.Diagnostics;
using ExperimentFramework.Data.Models;
using ExperimentFramework.Data.Recording;
using ExperimentFramework.Data.Storage;
using ExperimentFramework.Decorators;

namespace ExperimentFramework.Data.Decorators;

/// <summary>
/// A decorator factory that creates decorators for automatic outcome collection.
/// </summary>
/// <remarks>
/// <para>
/// When enabled, this decorator automatically records:
/// <list type="bullet">
/// <item><description>Duration of each invocation</description></item>
/// <item><description>Success/failure outcomes</description></item>
/// </list>
/// </para>
/// </remarks>
public sealed class OutcomeCollectionDecoratorFactory : IExperimentDecoratorFactory
{
    private readonly OutcomeRecorderOptions _options;
    private readonly Func<string, string>? _experimentNameResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutcomeCollectionDecoratorFactory"/> class.
    /// </summary>
    /// <param name="options">The recorder options.</param>
    /// <param name="experimentNameResolver">
    /// Optional function to resolve experiment name from service type name.
    /// If not provided, uses the service type name.
    /// </param>
    public OutcomeCollectionDecoratorFactory(
        OutcomeRecorderOptions? options = null,
        Func<string, string>? experimentNameResolver = null)
    {
        _options = options ?? new OutcomeRecorderOptions();
        _experimentNameResolver = experimentNameResolver;
    }

    /// <inheritdoc />
    public IExperimentDecorator Create(IServiceProvider sp)
    {
        var store = sp.GetService(typeof(IOutcomeStore)) as IOutcomeStore ?? NoopOutcomeStore.Instance;
        return new OutcomeCollectionDecorator(store, _options, _experimentNameResolver);
    }

    private sealed class OutcomeCollectionDecorator : IExperimentDecorator
    {
        private readonly IOutcomeStore _store;
        private readonly OutcomeRecorderOptions _options;
        private readonly Func<string, string>? _experimentNameResolver;

        public OutcomeCollectionDecorator(
            IOutcomeStore store,
            OutcomeRecorderOptions options,
            Func<string, string>? experimentNameResolver)
        {
            _store = store;
            _options = options;
            _experimentNameResolver = experimentNameResolver;
        }

        public async ValueTask<object?> InvokeAsync(InvocationContext ctx, Func<ValueTask<object?>> next)
        {
            var experimentName = ResolveExperimentName(ctx.ServiceType);
            var subjectId = GenerateSubjectId();
            var sw = _options.CollectDuration ? Stopwatch.StartNew() : null;

            try
            {
                var result = await next();
                sw?.Stop();

                // Record success
                if (_options.CollectDuration && sw != null)
                {
                    await RecordDurationAsync(experimentName, ctx.TrialKey, subjectId, sw.Elapsed);
                }

                await RecordSuccessAsync(experimentName, ctx.TrialKey, subjectId);

                return result;
            }
            catch (Exception ex)
            {
                sw?.Stop();

                // Record failure
                if (_options.CollectDuration && sw != null)
                {
                    await RecordDurationAsync(experimentName, ctx.TrialKey, subjectId, sw.Elapsed);
                }

                if (_options.CollectErrors)
                {
                    await RecordErrorAsync(experimentName, ctx.TrialKey, subjectId, ex);
                }

                throw;
            }
        }

        private string ResolveExperimentName(Type serviceType)
        {
            var typeName = serviceType.Name;

            // Remove leading 'I' from interface names
            if (typeName.StartsWith('I') && typeName.Length > 1 && char.IsUpper(typeName[1]))
            {
                typeName = typeName[1..];
            }

            return _experimentNameResolver?.Invoke(typeName) ?? typeName;
        }

        private static string GenerateSubjectId()
        {
            // In a real implementation, this would come from an identity provider
            // For now, generate a unique ID per invocation
            return Guid.NewGuid().ToString("N")[..8];
        }

        private ValueTask RecordDurationAsync(string experimentName, string trialKey, string subjectId, TimeSpan duration)
        {
            var outcome = new ExperimentOutcome
            {
                Id = Guid.NewGuid().ToString("N"),
                ExperimentName = experimentName,
                TrialKey = trialKey,
                SubjectId = subjectId,
                MetricName = _options.DurationMetricName,
                OutcomeType = OutcomeType.Duration,
                Value = duration.TotalSeconds,
                Timestamp = DateTimeOffset.UtcNow
            };

            return _store.RecordAsync(outcome);
        }

        private ValueTask RecordSuccessAsync(string experimentName, string trialKey, string subjectId)
        {
            var outcome = new ExperimentOutcome
            {
                Id = Guid.NewGuid().ToString("N"),
                ExperimentName = experimentName,
                TrialKey = trialKey,
                SubjectId = subjectId,
                MetricName = _options.SuccessMetricName,
                OutcomeType = OutcomeType.Binary,
                Value = 1.0,
                Timestamp = DateTimeOffset.UtcNow
            };

            return _store.RecordAsync(outcome);
        }

        private ValueTask RecordErrorAsync(string experimentName, string trialKey, string subjectId, Exception ex)
        {
            var outcome = new ExperimentOutcome
            {
                Id = Guid.NewGuid().ToString("N"),
                ExperimentName = experimentName,
                TrialKey = trialKey,
                SubjectId = subjectId,
                MetricName = _options.ErrorMetricName,
                OutcomeType = OutcomeType.Binary,
                Value = 0.0,
                Timestamp = DateTimeOffset.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["exception_type"] = ex.GetType().Name,
                    ["exception_message"] = ex.Message
                }
            };

            return _store.RecordAsync(outcome);
        }
    }
}
