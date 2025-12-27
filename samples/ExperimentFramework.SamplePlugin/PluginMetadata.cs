using ExperimentFramework.Plugins.Manifest;

// Configure plugin manifest generation with custom metadata
[assembly: GeneratePluginManifest(
    Id = "Acme.PaymentExperiments",
    Name = "Acme Payment Experiments Plugin",
    Description = "Sample plugin demonstrating experimental payment processors")]

// All public classes implementing IPaymentProcessor are automatically discovered:
// - StripeV2Processor -> alias: "stripe-v2"
// - AdyenProcessor -> alias: "adyen"
// - MollieProcessor -> alias: "mollie"
