using ExperimentFramework.Plugins.Abstractions;
using ExperimentFramework.Plugins.Manifest;

// Plugin manifest attribute for testing TryLoadFromAttributes
[assembly: PluginManifest("TestFixtures.AttributePlugin",
    Name = "Attribute Plugin",
    Version = "3.0.0",
    Description = "A test plugin defined via attributes",
    SupportsHotReload = false)]

// Isolation configuration
[assembly: PluginIsolation(
    Mode = PluginIsolationMode.Full,
    SharedAssemblies = new[] { "SharedLib1", "SharedLib2" })]

// Service registrations
[assembly: PluginService(
    InterfaceName = "IPaymentProcessor",
    Implementations = new[] { "StripeProcessor:stripe", "PayPalProcessor:paypal" })]

[assembly: PluginService(
    InterfaceName = "INotificationService",
    Implementations = new[] { "EmailNotifier:email", "SmsNotifier" })]
