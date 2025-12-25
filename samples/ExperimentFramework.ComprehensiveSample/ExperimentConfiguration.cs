using ExperimentFramework.ComprehensiveSample.Decorators;
using ExperimentFramework.ComprehensiveSample.Services.Decorator;
using ExperimentFramework.ComprehensiveSample.Services.ErrorPolicy;
using ExperimentFramework.ComprehensiveSample.Services.ReturnTypes;
using ExperimentFramework.ComprehensiveSample.Services.Telemetry;
using ExperimentFramework.ComprehensiveSample.Services.Variant;

namespace ExperimentFramework.ComprehensiveSample;

/// <summary>
/// Comprehensive experiment configuration demonstrating all features
/// </summary>
public static class ExperimentConfiguration
{
    /// <summary>
    /// Configures all experiment demos using the [ExperimentCompositionRoot] attribute
    /// </summary>
    [ExperimentCompositionRoot]
    public static ExperimentFrameworkBuilder ConfigureAllExperiments()
    {
        return ExperimentFrameworkBuilder.Create()

            // Add custom decorators (applied to all experiments)
            .AddDecoratorFactory(new TimingDecoratorFactory())
            .AddDecoratorFactory(new CachingDecoratorFactory())
            .AddDecoratorFactory(new CustomLoggingDecoratorFactory())

            // ========================================
            // DEMO 1: Error Policies
            // ========================================

            // 1.1: OnErrorThrow - Exception propagates immediately (fail fast)
            .Define<IThrowPolicyService>(c => c
                .UsingFeatureFlag("EnableUnstable")
                .AddDefaultTrial<StableImplementation>("false")
                .AddTrial<UnstableImplementation>("true"))
            // Default policy is Throw, so no .OnError*() call needed

            // 1.2: OnErrorRedirectAndReplayDefault - Falls back to default trial
            .Define<IRedirectDefaultService>(c => c
                .UsingFeatureFlag("UseExperimental")
                .AddDefaultTrial<DefaultImplementation>("false")
                .AddTrial<ExperimentalImplementation>("true")
                .OnErrorRedirectAndReplayDefault()) // Tries [preferred, default]

            // 1.3: OnErrorRedirectAndReplayAny - Tries all trials until success
            .Define<IRedirectAnyService>(c => c
                .UsingConfigurationKey("Experiments:PreferredProvider")
                .AddDefaultTrial<TertiaryProvider>("")
                .AddTrial<PrimaryProvider>("primary")
                .AddTrial<SecondaryProvider>("secondary")
                .OnErrorRedirectAndReplayAny()) // Tries all until one succeeds

            // 1.4: OnErrorRedirectAndReplay - Redirects to a specific fallback trial
            .Define<IRedirectSpecificService>(c => c
                .UsingFeatureFlag("UsePrimaryImplementation")
                .AddDefaultTrial<PrimaryImplementation>("true")
                .AddTrial<SecondaryImplementation>("false")
                .AddTrial<NoopDiagnosticsHandler>("noop")
                .OnErrorRedirectAndReplay("noop")) // Always redirect to Noop handler on error

            // 1.5: OnErrorRedirectAndReplayOrdered - Tries ordered list of fallback trials
            .Define<IRedirectOrderedService>(c => c
                .UsingFeatureFlag("UseCloudDatabase")
                .AddDefaultTrial<CloudDatabaseImplementation>("true")
                .AddTrial<LocalCacheImplementation>("cache")
                .AddTrial<InMemoryCacheImplementation>("memory")
                .AddTrial<StaticDataImplementation>("static")
                .OnErrorRedirectAndReplayOrdered("cache", "memory", "static")) // Try in order: cache → memory → static

            // ========================================
            // DEMO 2: Custom Decorators
            // ========================================

            .Define<IDataService>(c => c
                .UsingFeatureFlag("EnablePremiumCaching")
                .AddDefaultTrial<DatabaseDataService>("false")
                .AddTrial<CacheDataService>("true")
                .OnErrorRedirectAndReplayDefault())
            // Custom decorators added at framework level (see above)

            // ========================================
            // DEMO 3: OpenTelemetry Integration
            // ========================================

            .Define<INotificationService>(c => c
                .UsingFeatureFlag("UseSmsNotifications")
                .AddDefaultTrial<EmailNotificationService>("false")
                .AddTrial<SmsNotificationService>("true")
                .OnErrorRedirectAndReplayDefault())
            // OpenTelemetry telemetry is automatically enabled when registered in DI

            // ========================================
            // DEMO 4: Custom Selection Mode (Variant Feature Flags)
            // ========================================
            // Note: In a real app, you would use ExperimentFramework.FeatureManagement
            // package which provides .UsingVariantFeatureFlag() extension method

            .Define<IPaymentProcessor>(c => c
                .UsingCustomMode("VariantFeatureFlag", "PaymentProviderVariant")
                .AddDefaultTrial<StripePaymentProcessor>("stripe")
                .AddTrial<PayPalPaymentProcessor>("paypal")
                .AddTrial<SquarePaymentProcessor>("square")
                .OnErrorRedirectAndReplayDefault())

            // ========================================
            // DEMO 5: All Return Types
            // ========================================

            // 5.1: void
            .Define<IVoidService>(c => c
                .UsingConfigurationKey("ReturnTypes:Void")
                .AddDefaultTrial<VoidImplementationA>("")
                .AddTrial<VoidImplementationB>("b"))

            // 5.2: Task
            .Define<ITaskService>(c => c
                .UsingConfigurationKey("ReturnTypes:Task")
                .AddDefaultTrial<TaskImplementationA>("")
                .AddTrial<TaskImplementationB>("b"))

            // 5.3: Task<T>
            .Define<ITaskTService>(c => c
                .UsingConfigurationKey("ReturnTypes:TaskT")
                .AddDefaultTrial<TaskTImplementationA>("")
                .AddTrial<TaskTImplementationB>("b"))

            // 5.4: ValueTask
            .Define<IValueTaskService>(c => c
                .UsingConfigurationKey("ReturnTypes:ValueTask")
                .AddDefaultTrial<ValueTaskImplementationA>("")
                .AddTrial<ValueTaskImplementationB>("b"))

            // 5.5: ValueTask<T>
            .Define<IValueTaskTService>(c => c
                .UsingConfigurationKey("ReturnTypes:ValueTaskT")
                .AddDefaultTrial<ValueTaskTImplementationA>("")
                .AddTrial<ValueTaskTImplementationB>("b"));
    }
}
