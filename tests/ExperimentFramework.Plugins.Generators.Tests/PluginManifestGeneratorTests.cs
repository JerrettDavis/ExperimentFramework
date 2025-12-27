using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace ExperimentFramework.Plugins.Generators.Tests;

/// <summary>
/// Tests for the PluginManifestGenerator source generator.
/// </summary>
public class PluginManifestGeneratorTests
{
    [Fact]
    public void Generator_WithPublicClass_GeneratesManifest()
    {
        // Arrange
        var source = """
            namespace TestPlugin;

            public interface IPaymentProcessor
            {
                void Process();
            }

            public class StripeProcessor : IPaymentProcessor
            {
                public void Process() { }
            }
            """;

        // Act
        var (compilation, generatedTrees) = RunGenerator(source);

        // Assert
        Assert.Single(generatedTrees);
        var generatedCode = generatedTrees[0].GetText().ToString();
        Assert.Contains("PluginManifestAttribute", generatedCode);
        Assert.Contains("PluginServiceAttribute", generatedCode);
        Assert.Contains("TestPlugin.IPaymentProcessor", generatedCode);
        Assert.Contains("StripeProcessor:stripe", generatedCode);
    }

    [Fact]
    public void Generator_WithMultipleImplementations_GeneratesManifest()
    {
        // Arrange
        var source = """
            namespace TestPlugin;

            public interface IPaymentProcessor
            {
                void Process();
            }

            public class StripeProcessor : IPaymentProcessor
            {
                public void Process() { }
            }

            public class AdyenProcessor : IPaymentProcessor
            {
                public void Process() { }
            }

            public class PayPalProcessor : IPaymentProcessor
            {
                public void Process() { }
            }
            """;

        // Act
        var (compilation, generatedTrees) = RunGenerator(source);

        // Assert
        Assert.Single(generatedTrees);
        var generatedCode = generatedTrees[0].GetText().ToString();
        Assert.Contains("StripeProcessor:stripe", generatedCode);
        Assert.Contains("AdyenProcessor:adyen", generatedCode);
        Assert.Contains("PayPalProcessor:pay-pal", generatedCode);
    }

    [Fact]
    public void Generator_WithNoPublicClasses_GeneratesNothing()
    {
        // Arrange - only internal classes
        var source = """
            namespace TestPlugin;

            public interface IPaymentProcessor
            {
                void Process();
            }

            internal class InternalProcessor : IPaymentProcessor
            {
                public void Process() { }
            }
            """;

        // Act
        var (compilation, generatedTrees) = RunGenerator(source);

        // Assert - should generate nothing
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void Generator_WithAbstractClass_ExcludesAbstract()
    {
        // Arrange
        var source = """
            namespace TestPlugin;

            public interface IPaymentProcessor
            {
                void Process();
            }

            public abstract class BaseProcessor : IPaymentProcessor
            {
                public abstract void Process();
            }

            public class ConcreteProcessor : BaseProcessor
            {
                public override void Process() { }
            }
            """;

        // Act
        var (compilation, generatedTrees) = RunGenerator(source);

        // Assert - only ConcreteProcessor should be included
        Assert.Single(generatedTrees);
        var generatedCode = generatedTrees[0].GetText().ToString();
        Assert.Contains("ConcreteProcessor:concrete", generatedCode);
        Assert.DoesNotContain("BaseProcessor", generatedCode);
    }

    [Fact]
    public void Generator_WithSystemInterfaces_ExcludesSystemInterfaces()
    {
        // Arrange - class only implements IDisposable (System namespace)
        var source = """
            using System;

            namespace TestPlugin;

            public class MyDisposable : IDisposable
            {
                public void Dispose() { }
            }
            """;

        // Act
        var (compilation, generatedTrees) = RunGenerator(source);

        // Assert - should not generate manifest since only system interfaces
        Assert.Empty(generatedTrees);
    }

    [Fact]
    public void Generator_WithMultipleInterfaces_IncludesAll()
    {
        // Arrange
        var source = """
            using System;

            namespace TestPlugin;

            public interface IPaymentProcessor
            {
                void Process();
            }

            public interface IRefundProcessor
            {
                void Refund();
            }

            public class StripeProcessor : IPaymentProcessor, IRefundProcessor, IDisposable
            {
                public void Process() { }
                public void Refund() { }
                public void Dispose() { }
            }
            """;

        // Act
        var (compilation, generatedTrees) = RunGenerator(source);

        // Assert - should include both custom interfaces but not IDisposable
        Assert.Single(generatedTrees);
        var generatedCode = generatedTrees[0].GetText().ToString();
        Assert.Contains("IPaymentProcessor", generatedCode);
        Assert.Contains("IRefundProcessor", generatedCode);
        Assert.DoesNotContain("IDisposable", generatedCode);
    }

    [Fact]
    public void Generator_WithExcludeAttribute_ExcludesClass()
    {
        // Arrange
        var source = """
            using ExperimentFramework.Plugins.Manifest;

            namespace TestPlugin;

            public interface IPaymentProcessor
            {
                void Process();
            }

            public class StripeProcessor : IPaymentProcessor
            {
                public void Process() { }
            }

            [PluginImplementation(Exclude = true)]
            public class InternalProcessor : IPaymentProcessor
            {
                public void Process() { }
            }
            """;

        // Act
        var (compilation, generatedTrees) = RunGenerator(source, includePluginsReference: true);

        // Assert - only StripeProcessor should be included
        Assert.Single(generatedTrees);
        var generatedCode = generatedTrees[0].GetText().ToString();
        Assert.Contains("StripeProcessor:stripe", generatedCode);
        Assert.DoesNotContain("InternalProcessor", generatedCode);
    }

    [Fact]
    public void Generator_WithExplicitAlias_UsesProvidedAlias()
    {
        // Arrange
        var source = """
            using ExperimentFramework.Plugins.Manifest;

            namespace TestPlugin;

            public interface IPaymentProcessor
            {
                void Process();
            }

            [PluginImplementation(Alias = "stripe-v2")]
            public class StripeProcessorV2 : IPaymentProcessor
            {
                public void Process() { }
            }
            """;

        // Act
        var (compilation, generatedTrees) = RunGenerator(source, includePluginsReference: true);

        // Assert
        Assert.Single(generatedTrees);
        var generatedCode = generatedTrees[0].GetText().ToString();
        Assert.Contains("StripeProcessorV2:stripe-v2", generatedCode);
    }

    [Fact]
    public void Generator_WithGeneratePluginManifestAttribute_UsesProvidedMetadata()
    {
        // Arrange
        var source = """
            using ExperimentFramework.Plugins.Manifest;

            [assembly: GeneratePluginManifest(
                Id = "Acme.Payments",
                Name = "Acme Payment Processors",
                Description = "Payment processing implementations")]

            namespace TestPlugin;

            public interface IPaymentProcessor
            {
                void Process();
            }

            public class StripeProcessor : IPaymentProcessor
            {
                public void Process() { }
            }
            """;

        // Act
        var (compilation, generatedTrees) = RunGenerator(source, includePluginsReference: true);

        // Assert
        Assert.Single(generatedTrees);
        var generatedCode = generatedTrees[0].GetText().ToString();
        Assert.Contains("Acme.Payments", generatedCode);
        Assert.Contains("Acme Payment Processors", generatedCode);
        Assert.Contains("Payment processing implementations", generatedCode);
    }

    [Fact]
    public void Generator_GeneratesCorrectAlias_StripsCommonSuffixes()
    {
        // Arrange
        var source = """
            namespace TestPlugin;

            public interface IPaymentProcessor { void Process(); }
            public interface IDataHandler { void Handle(); }
            public interface IUserService { void Serve(); }
            public interface IConfigProvider { void Provide(); }

            public class PaymentProcessor : IPaymentProcessor { public void Process() { } }
            public class DataHandler : IDataHandler { public void Handle() { } }
            public class UserService : IUserService { public void Serve() { } }
            public class ConfigProvider : IConfigProvider { public void Provide() { } }
            """;

        // Act
        var (compilation, generatedTrees) = RunGenerator(source);

        // Assert
        Assert.Single(generatedTrees);
        var generatedCode = generatedTrees[0].GetText().ToString();
        Assert.Contains("PaymentProcessor:payment", generatedCode);
        Assert.Contains("DataHandler:data", generatedCode);
        Assert.Contains("UserService:user", generatedCode);
        Assert.Contains("ConfigProvider:config", generatedCode);
    }

    private static (Compilation, ImmutableArray<SyntaxTree>) RunGenerator(
        string source,
        bool includePluginsReference = false)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        }.ToList();

        // Add netstandard reference
        var netstandard = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "netstandard");
        if (netstandard != null)
        {
            references.Add(MetadataReference.CreateFromFile(netstandard.Location));
        }

        // Add System.Runtime reference
        var runtime = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "System.Runtime");
        if (runtime != null)
        {
            references.Add(MetadataReference.CreateFromFile(runtime.Location));
        }

        if (includePluginsReference)
        {
            references.Add(MetadataReference.CreateFromFile(
                typeof(ExperimentFramework.Plugins.Manifest.PluginManifestAttribute).Assembly.Location));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new PluginManifestGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees;

        return (outputCompilation, generatedTrees);
    }
}
