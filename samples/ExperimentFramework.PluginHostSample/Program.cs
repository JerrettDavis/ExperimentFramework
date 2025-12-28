using ExperimentFramework.Plugins;
using ExperimentFramework.Plugins.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine("  ExperimentFramework Plugin System Demo");
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine();

// Build the host with plugin support
var builder = Host.CreateApplicationBuilder(args);

// Configure plugin system
builder.Services.AddExperimentPlugins(opts =>
{
    // Look for plugins in the plugins directory
    opts.DiscoveryPaths.Add("./plugins");
    opts.AutoLoadOnStartup = false; // We'll load manually for demo
    opts.DefaultIsolationMode = PluginIsolationMode.Shared;
});

var app = builder.Build();

// Get the plugin manager
var pluginManager = app.Services.GetRequiredService<IPluginManager>();

// Demonstrate plugin loading
Console.WriteLine("Step 1: Loading plugin from disk...");
Console.WriteLine("-".PadRight(40, '-'));

// Build the plugin first and get its path
var pluginPath = GetPluginPath();

if (!File.Exists(pluginPath))
{
    Console.WriteLine($"Plugin not found at: {pluginPath}");
    Console.WriteLine();
    Console.WriteLine("Please build the SamplePlugin project first:");
    Console.WriteLine("  dotnet build samples/ExperimentFramework.SamplePlugin");
    return;
}

try
{
    var context = await pluginManager.LoadAsync(pluginPath);

    Console.WriteLine($"Loaded: {context.Manifest.Name} v{context.Manifest.Version}");
    Console.WriteLine($"Plugin ID: {context.Manifest.Id}");
    Console.WriteLine($"Description: {context.Manifest.Description}");
    Console.WriteLine($"Isolation: {context.Manifest.Isolation.Mode}");
    Console.WriteLine($"Hot Reload: {(context.Manifest.Lifecycle.SupportsHotReload ? "Enabled" : "Disabled")}");
    Console.WriteLine();

    // Show registered services
    Console.WriteLine("Step 2: Discovering plugin services...");
    Console.WriteLine("-".PadRight(40, '-'));

    foreach (var service in context.Manifest.Services)
    {
        Console.WriteLine($"Interface: {service.Interface}");
        foreach (var impl in service.Implementations)
        {
            Console.WriteLine($"  - {impl.Alias ?? impl.Type}");

            // Try to resolve the type
            var type = context.GetTypeByAlias(impl.Alias ?? impl.Type)
                       ?? context.GetType(impl.Type);
            if (type is not null)
            {
                Console.WriteLine($"    Type: {type.FullName}");
            }
        }
    }
    Console.WriteLine();

    // Demonstrate type resolution
    Console.WriteLine("Step 3: Using plugin types...");
    Console.WriteLine("-".PadRight(40, '-'));

    // Create instances using the service provider
    var stripeType = context.GetTypeByAlias("stripe-v2");
    var adyenType = context.GetTypeByAlias("adyen");
    var mollieType = context.GetTypeByAlias("mollie");

    if (stripeType is not null && adyenType is not null && mollieType is not null)
    {
        // Create instances and their types for reflection
        var processors = new[]
        {
            (Type: stripeType, Instance: context.CreateInstance(stripeType, app.Services)),
            (Type: adyenType, Instance: context.CreateInstance(adyenType, app.Services)),
            (Type: mollieType, Instance: context.CreateInstance(mollieType, app.Services))
        };

        // Process payments with each processor
        foreach (var (type, processor) in processors)
        {
            // Use reflection to call methods on each type
            var processMethod = type.GetMethod("ProcessAsync")!;
            var nameProperty = type.GetProperty("Name")!;
            var versionProperty = type.GetProperty("Version")!;

            var name = nameProperty.GetValue(processor);
            var version = versionProperty.GetValue(processor);
            Console.WriteLine($"\nUsing {name} v{version}:");

            var task = (Task)processMethod.Invoke(processor, [99.99m, "USD"])!;
            await task;

            // Get the result from the task using reflection
            var resultProperty = task.GetType().GetProperty("Result")!;
            var result = resultProperty.GetValue(task)!;

            var successProp = result.GetType().GetProperty("Success")!;
            var transactionProp = result.GetType().GetProperty("TransactionId")!;
            var messageProp = result.GetType().GetProperty("Message")!;

            Console.WriteLine($"  Success: {successProp.GetValue(result)}");
            Console.WriteLine($"  Transaction: {transactionProp.GetValue(result)}");
            Console.WriteLine($"  Message: {messageProp.GetValue(result)}");
        }
    }
    Console.WriteLine();

    // Demonstrate plugin manager features
    Console.WriteLine("Step 4: Plugin manager features...");
    Console.WriteLine("-".PadRight(40, '-'));

    Console.WriteLine($"Loaded plugins: {pluginManager.GetLoadedPlugins().Count}");
    Console.WriteLine($"Is plugin loaded: {pluginManager.IsLoaded("Acme.PaymentExperiments")}");

    // Show type resolution syntax
    Console.WriteLine();
    Console.WriteLine("Type reference syntax for YAML configuration:");
    Console.WriteLine("  - plugin:Acme.PaymentExperiments/stripe-v2");
    Console.WriteLine("  - plugin:Acme.PaymentExperiments/adyen");
    Console.WriteLine("  - plugin:Acme.PaymentExperiments/mollie");

    // Demonstrate unloading
    Console.WriteLine();
    Console.WriteLine("Step 5: Unloading plugin...");
    Console.WriteLine("-".PadRight(40, '-'));

    await pluginManager.UnloadAsync("Acme.PaymentExperiments");
    Console.WriteLine($"Is plugin loaded: {pluginManager.IsLoaded("Acme.PaymentExperiments")}");
    Console.WriteLine("Plugin unloaded successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("=".PadRight(60, '='));
Console.WriteLine("  Demo Complete!");
Console.WriteLine("=".PadRight(60, '='));

static string GetPluginPath()
{
    // Get the path relative to the sample project
    var baseDir = AppContext.BaseDirectory;

    // Navigate to the SamplePlugin output
    var pluginPath = Path.Combine(baseDir, "..", "..", "..", "..",
        "ExperimentFramework.SamplePlugin", "bin", "Debug", "net10.0",
        "ExperimentFramework.SamplePlugin.dll");

    return Path.GetFullPath(pluginPath);
}
