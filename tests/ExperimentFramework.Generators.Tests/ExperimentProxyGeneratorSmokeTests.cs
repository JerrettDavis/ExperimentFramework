using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ExperimentFramework.Generators.Tests;

/// <summary>
/// Smoke tests for ExperimentProxyGenerator - verifies generator runs without errors.
/// </summary>
public class ExperimentProxyGeneratorSmokeTests
{
    [Fact]
    public void Generator_WithFluentApi_RunsWithoutErrors()
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

            public static class ExperimentConfig
            {
                public static ExperimentFrameworkBuilder Configure()
                {
                    return ExperimentFrameworkBuilder.Create()
                        .Define<IMyService>(c => c
                            .UsingConfigurationKey("Service")
                            .AddDefaultTrial<ServiceA>("A"))
                        .UseSourceGenerators();
                }
            }
            """;

        // Act
        var (diagnostics, generatedSources) = RunGenerator(source);

        // Assert - generator ran without errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);

        // Assert - generator produced output
        Assert.NotEmpty(generatedSources);
    }

    [Fact]
    public void Generator_WithAttribute_RunsWithoutErrors()
    {
        // Arrange
        var source = """
            using ExperimentFramework;

            namespace TestApp;

            public interface IDatabase
            {
                string GetName();
            }

            public class LocalDb : IDatabase
            {
                public string GetName() => "Local";
            }

            public static class ExperimentConfig
            {
                [ExperimentCompositionRoot]
                public static ExperimentFrameworkBuilder Configure()
                {
                    return ExperimentFrameworkBuilder.Create()
                        .Define<IDatabase>(c => c
                            .UsingConfigurationKey("Database")
                            .AddDefaultTrial<LocalDb>("local"));
                }
            }
            """;

        // Act
        var (diagnostics, generatedSources) = RunGenerator(source);

        // Assert
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
        Assert.NotEmpty(generatedSources);
    }

    [Fact]
    public void Generator_WithVoidMethod_RunsWithoutErrors()
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

            public static class ExperimentConfig
            {
                [ExperimentCompositionRoot]
                public static ExperimentFrameworkBuilder Configure()
                {
                    return ExperimentFrameworkBuilder.Create()
                        .Define<ILogger>(c => c
                            .UsingConfigurationKey("Logger")
                            .AddDefaultTrial<ConsoleLogger>("console"));
                }
            }
            """;

        // Act
        var (diagnostics, generatedSources) = RunGenerator(source);

        // Assert
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
        Assert.NotEmpty(generatedSources);
    }

    [Fact]
    public void Generator_WithMultipleServices_RunsWithoutErrors()
    {
        // Arrange
        var source = """
            using ExperimentFramework;
            using System.Threading.Tasks;

            namespace TestApp;

            public interface IServiceA { Task DoWorkAsync(); }
            public interface IServiceB { int Calculate(); }
            public class ServiceA1 : IServiceA { public Task DoWorkAsync() => Task.CompletedTask; }
            public class ServiceB1 : IServiceB { public int Calculate() => 42; }

            public static class ExperimentConfig
            {
                public static ExperimentFrameworkBuilder Configure()
                {
                    return ExperimentFrameworkBuilder.Create()
                        .Define<IServiceA>(c => c.UsingConfigurationKey("A").AddDefaultTrial<ServiceA1>("1"))
                        .Define<IServiceB>(c => c.UsingConfigurationKey("B").AddDefaultTrial<ServiceB1>("1"))
                        .UseSourceGenerators();
                }
            }
            """;

        // Act
        var (diagnostics, generatedSources) = RunGenerator(source);

        // Assert
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
        Assert.NotEmpty(generatedSources);
    }

    [Fact]
    public void Generator_WithNoTrigger_ProducesMinimalOutput()
    {
        // Arrange - no UseSourceGenerators() or [ExperimentCompositionRoot]
        var source = """
            using ExperimentFramework;

            namespace TestApp;

            public interface IMyService { string GetData(); }
            public class ServiceImpl : IMyService { public string GetData() => "Data"; }
            """;

        // Act
        var (diagnostics, generatedSources) = RunGenerator(source);

        // Assert - should run without errors
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Generator_WithGenericInterface_RunsWithoutErrors()
    {
        // Arrange
        var source = """
            using ExperimentFramework;
            using System.Threading.Tasks;

            namespace TestApp;

            public interface IRepository<T> { Task<T> GetByIdAsync(int id); }
            public class Entity { public int Id { get; set; } }
            public class RepositoryV1 : IRepository<Entity>
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
                            .AddDefaultTrial<RepositoryV1>("v1"))
                        .UseSourceGenerators();
                }
            }
            """;

        // Act
        var (diagnostics, generatedSources) = RunGenerator(source);

        // Assert
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        Assert.Empty(errors);
        Assert.NotEmpty(generatedSources);
    }

    /// <summary>
    /// Runs the generator and returns diagnostics and generated sources.
    /// </summary>
    private static (Diagnostic[] diagnostics, string[] generatedSources) RunGenerator(string source)
    {
        // Create compilation
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .ToList();

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        // Run generator
        var generator = new ExperimentProxyGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver = (CSharpGeneratorDriver)driver.RunGenerators(compilation);

        // Get results
        var runResult = driver.GetRunResult();
        var diagnostics = runResult.Diagnostics;
        var generatedSources = runResult.GeneratedTrees.Select(t => t.ToString()).ToArray();

        return (diagnostics.ToArray(), generatedSources);
    }
}
