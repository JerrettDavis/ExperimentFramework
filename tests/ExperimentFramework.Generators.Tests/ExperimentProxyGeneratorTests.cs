using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ExperimentFramework.Generators.Tests;

/// <summary>
/// Tests for the ExperimentProxyGenerator source generator.
/// </summary>
public class ExperimentProxyGeneratorTests
{
    [Fact]
    public void Generator_WithFluentApi_GeneratesDiagnosticFile()
    {
        // Arrange
        var source = """
            using ExperimentFramework;
            using System.Threading.Tasks;

            namespace TestApp;

            public interface IMyService
            {
                Task<string> GetDataAsync();
            }

            public class ServiceA : IMyService
            {
                public Task<string> GetDataAsync() => Task.FromResult("A");
            }

            public class ServiceB : IMyService
            {
                public Task<string> GetDataAsync() => Task.FromResult("B");
            }

            public static class ExperimentConfig
            {
                public static ExperimentFrameworkBuilder Configure()
                {
                    return ExperimentFrameworkBuilder.Create()
                        .Define<IMyService>(c => c
                            .UsingConfigurationKey("Service")
                            .AddDefaultTrial<ServiceA>("A")
                            .AddTrial<ServiceB>("B"))
                        .UseSourceGenerators();
                }
            }
            """;

        // Act
        var (compilation, generatedTrees, diagnostics) = RunGenerator(source);

        // Assert - verify generator runs without errors and produces diagnostic file
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("GeneratorDiagnostic.g.cs"));
    }

    [Fact]
    public void Generator_WithAttribute_GeneratesDiagnosticFile()
    {
        // Arrange
        var source = """
            using ExperimentFramework;
            using System.Threading.Tasks;

            namespace TestApp;

            public interface IDatabase
            {
                string GetName();
            }

            public class LocalDb : IDatabase
            {
                public string GetName() => "Local";
            }

            public class CloudDb : IDatabase
            {
                public string GetName() => "Cloud";
            }

            public static class ExperimentConfig
            {
                [ExperimentCompositionRoot]
                public static ExperimentFrameworkBuilder Configure()
                {
                    return ExperimentFrameworkBuilder.Create()
                        .Define<IDatabase>(c => c
                            .UsingConfigurationKey("Database")
                            .AddDefaultTrial<LocalDb>("local")
                            .AddTrial<CloudDb>("cloud"));
                }
            }
            """;

        // Act
        var (compilation, generatedTrees, diagnostics) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("GeneratorDiagnostic.g.cs"));
    }

    [Fact]
    public void Generator_WithMultipleServices_GeneratesDiagnosticFile()
    {
        // Arrange
        var source = """
            using ExperimentFramework;
            using System.Threading.Tasks;

            namespace TestApp;

            public interface IServiceA
            {
                Task DoWorkAsync();
            }

            public interface IServiceB
            {
                int Calculate();
            }

            public class ServiceA1 : IServiceA
            {
                public Task DoWorkAsync() => Task.CompletedTask;
            }

            public class ServiceB1 : IServiceB
            {
                public int Calculate() => 42;
            }

            public static class ExperimentConfig
            {
                public static ExperimentFrameworkBuilder Configure()
                {
                    return ExperimentFrameworkBuilder.Create()
                        .Define<IServiceA>(c => c
                            .UsingConfigurationKey("A")
                            .AddDefaultTrial<ServiceA1>("1"))
                        .Define<IServiceB>(c => c
                            .UsingConfigurationKey("B")
                            .AddDefaultTrial<ServiceB1>("1"))
                        .UseSourceGenerators();
                }
            }
            """;

        // Act
        var (compilation, generatedTrees, diagnostics) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("GeneratorDiagnostic.g.cs"));
    }

    [Fact]
    public void Generator_WithNoTrigger_GeneratesDiagnosticOnly()
    {
        // Arrange - no UseSourceGenerators() or [ExperimentCompositionRoot]
        var source = """
            using ExperimentFramework;

            namespace TestApp;

            public interface IMyService
            {
                string GetData();
            }

            public class ServiceImpl : IMyService
            {
                public string GetData() => "Data";
            }

            public static class ExperimentConfig
            {
                public static ExperimentFrameworkBuilder Configure()
                {
                    return ExperimentFrameworkBuilder.Create()
                        .Define<IMyService>(c => c
                            .UsingConfigurationKey("Service")
                            .AddDefaultTrial<ServiceImpl>("default"));
                    // No .UseSourceGenerators() and no [ExperimentCompositionRoot]
                }
            }
            """;

        // Act
        var (compilation, generatedTrees, diagnostics) = RunGenerator(source);

        // Assert - only diagnostic file generated (no proxies since no trigger found)
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("GeneratorDiagnostic.g.cs"));
        // Generator should only produce the diagnostic file when no experiments are found
        Assert.Single(generatedTrees);
    }

    [Fact]
    public void Generator_WithVoidMethod_GeneratesDiagnosticFile()
    {
        // Arrange
        var source = """
            using ExperimentFramework;

            namespace TestApp;

            public interface ILogger
            {
                void Log(string message);
            }

            public class ConsoleLogger : ILogger
            {
                public void Log(string message) { }
            }

            public class FileLogger : ILogger
            {
                public void Log(string message) { }
            }

            public static class ExperimentConfig
            {
                [ExperimentCompositionRoot]
                public static ExperimentFrameworkBuilder Configure()
                {
                    return ExperimentFrameworkBuilder.Create()
                        .Define<ILogger>(c => c
                            .UsingConfigurationKey("Logger")
                            .AddDefaultTrial<ConsoleLogger>("console")
                            .AddTrial<FileLogger>("file"));
                }
            }
            """;

        // Act
        var (compilation, generatedTrees, diagnostics) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("GeneratorDiagnostic.g.cs"));
    }

    [Fact]
    public void Generator_WithGenericInterface_GeneratesDiagnosticFile()
    {
        // Arrange
        var source = """
            using ExperimentFramework;
            using System.Threading.Tasks;

            namespace TestApp;

            public interface IRepository<T>
            {
                Task<T> GetByIdAsync(int id);
            }

            public class Entity { public int Id { get; set; } }

            public class RepositoryV1 : IRepository<Entity>
            {
                public Task<Entity> GetByIdAsync(int id) => Task.FromResult(new Entity { Id = id });
            }

            public class RepositoryV2 : IRepository<Entity>
            {
                public Task<Entity> GetByIdAsync(int id) => Task.FromResult(new Entity { Id = id });
            }

            public static class ExperimentConfig
            {
                public static ExperimentFrameworkBuilder Configure()
                {
                    return ExperimentFrameworkBuilder.Create()
                        .Define<IRepository<Entity>>(c => c
                            .UsingConfigurationKey("Repo")
                            .AddDefaultTrial<RepositoryV1>("v1")
                            .AddTrial<RepositoryV2>("v2"))
                        .UseSourceGenerators();
                }
            }
            """;

        // Act
        var (compilation, generatedTrees, diagnostics) = RunGenerator(source);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("GeneratorDiagnostic.g.cs"));
    }

    [Fact]
    public void Generator_DiagnosticFileContainsExpectedContent()
    {
        // Arrange
        var source = """
            using ExperimentFramework;

            namespace TestApp;

            public interface IMyService
            {
                string GetData();
            }

            public class ServiceImpl : IMyService
            {
                public string GetData() => "Data";
            }
            """;

        // Act
        var (compilation, generatedTrees, diagnostics) = RunGenerator(source);

        // Assert
        var diagnosticTree = generatedTrees.FirstOrDefault(t => t.FilePath.EndsWith("GeneratorDiagnostic.g.cs"));
        Assert.NotNull(diagnosticTree);

        var content = diagnosticTree.GetText().ToString();
        Assert.Contains("// <auto-generated />", content);
        Assert.Contains("ExperimentFramework Source Generator Diagnostic", content);
    }

    [Fact]
    public void Generator_WithEmptySource_DoesNotCrash()
    {
        // Arrange
        var source = """
            namespace TestApp;

            public class EmptyClass { }
            """;

        // Act
        var (compilation, generatedTrees, diagnostics) = RunGenerator(source);

        // Assert - should not throw and should produce diagnostic file
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("GeneratorDiagnostic.g.cs"));
    }

    [Fact]
    public void Generator_WithInvalidSyntax_HandlesGracefully()
    {
        // Arrange - valid C# that references non-existent types
        var source = """
            using ExperimentFramework;

            namespace TestApp;

            public class TestClass
            {
                public void Method()
                {
                    var builder = ExperimentFrameworkBuilder.Create();
                }
            }
            """;

        // Act
        var (compilation, generatedTrees, diagnostics) = RunGenerator(source);

        // Assert - generator should still run
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("GeneratorDiagnostic.g.cs"));
    }

    [Fact]
    public void Generator_WithMultipleFiles_GeneratesDiagnosticFile()
    {
        // Arrange - multiple source files
        var source1 = """
            namespace TestApp;

            public interface IService { void DoWork(); }
            """;

        var source2 = """
            using ExperimentFramework;

            namespace TestApp;

            public class ServiceImpl : IService
            {
                public void DoWork() { }
            }

            public static class Config
            {
                public static ExperimentFrameworkBuilder Configure()
                {
                    return ExperimentFrameworkBuilder.Create()
                        .Define<IService>(c => c
                            .UsingConfigurationKey("Service")
                            .AddDefaultTrial<ServiceImpl>("default"))
                        .UseSourceGenerators();
                }
            }
            """;

        // Act
        var (compilation, generatedTrees, diagnostics) = RunGenerator(source1, source2);

        // Assert
        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        Assert.Contains(generatedTrees, t => t.FilePath.EndsWith("GeneratorDiagnostic.g.cs"));
    }

    [Fact]
    public void Generator_ProducesValidCSharp()
    {
        // Arrange
        var source = """
            using ExperimentFramework;

            namespace TestApp;

            public interface IService { void DoWork(); }

            public class ServiceImpl : IService
            {
                public void DoWork() { }
            }
            """;

        // Act
        var (compilation, generatedTrees, diagnostics) = RunGenerator(source);

        // Assert - generated code should be valid C#
        foreach (var tree in generatedTrees)
        {
            var syntaxDiagnostics = tree.GetDiagnostics();
            Assert.Empty(syntaxDiagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        }
    }

    private static (Compilation, ImmutableArray<SyntaxTree>, ImmutableArray<Diagnostic>) RunGenerator(
        params string[] sources)
    {
        var syntaxTrees = sources.Select(s => CSharpSyntaxTree.ParseText(s)).ToArray();

        var references = new[]
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(ExperimentFramework.ExperimentFrameworkBuilder).Assembly.Location),
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

        var compilation = CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ExperimentProxyGenerator();

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var runResult = driver.GetRunResult();
        var generatedTrees = runResult.GeneratedTrees;

        return (outputCompilation, generatedTrees, diagnostics);
    }
}
