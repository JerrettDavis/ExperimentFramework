using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using System;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace ExperimentFramework.Generators.Tests;

/// <summary>
/// Tests for the ExperimentProxyGenerator source generator.
/// </summary>
public class ExperimentProxyGeneratorTests
{
    [Fact]
    public async Task Generator_WithFluentApi_GeneratesProxy()
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

        // Act & Assert - verify generator runs without errors
        await new ExperimentProxyGeneratorVerifier
        {
            TestState =
            {
                Sources = { source },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                AdditionalReferences = { GetExperimentFrameworkReference() },
                GeneratedSources =
                {
                    // Verify diagnostic file is generated
                    (typeof(ExperimentProxyGenerator), "GeneratorDiagnostic.g.cs", ""),
                    // Verify proxy is generated
                    (typeof(ExperimentProxyGenerator), "MyServiceExperimentProxy.g.cs", "")
                }
            }
        }.RunAsync();
    }

    [Fact]
    public async Task Generator_WithAttribute_GeneratesProxy()
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

        // Act & Assert
        await new ExperimentProxyGeneratorVerifier
        {
            TestState =
            {
                Sources = { source },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                AdditionalReferences = { GetExperimentFrameworkReference() },
                GeneratedSources =
                {
                    (typeof(ExperimentProxyGenerator), "GeneratorDiagnostic.g.cs", ""),
                    (typeof(ExperimentProxyGenerator), "DatabaseExperimentProxy.g.cs", "")
                }
            }
        }.RunAsync();
    }

    [Fact]
    public async Task Generator_WithMultipleServices_GeneratesMultipleProxies()
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

        // Act & Assert
        await new ExperimentProxyGeneratorVerifier
        {
            TestState =
            {
                Sources = { source },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                AdditionalReferences = { GetExperimentFrameworkReference() },
                GeneratedSources =
                {
                    (typeof(ExperimentProxyGenerator), "GeneratorDiagnostic.g.cs", ""),
                    (typeof(ExperimentProxyGenerator), "ServiceAExperimentProxy.g.cs", ""),
                    (typeof(ExperimentProxyGenerator), "ServiceBExperimentProxy.g.cs", "")
                }
            }
        }.RunAsync();
    }

    [Fact]
    public async Task Generator_WithNoTrigger_GeneratesNothing()
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

        // Act & Assert - only diagnostic file, no proxies
        await new ExperimentProxyGeneratorVerifier
        {
            TestState =
            {
                Sources = { source },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                AdditionalReferences = { GetExperimentFrameworkReference() },
                GeneratedSources =
                {
                    (typeof(ExperimentProxyGenerator), "GeneratorDiagnostic.g.cs", "")
                    // No proxy files should be generated
                }
            }
        }.RunAsync();
    }

    [Fact]
    public async Task Generator_WithVoidMethod_GeneratesCorrectProxy()
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

        // Act & Assert
        await new ExperimentProxyGeneratorVerifier
        {
            TestState =
            {
                Sources = { source },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                AdditionalReferences = { GetExperimentFrameworkReference() },
                GeneratedSources =
                {
                    (typeof(ExperimentProxyGenerator), "GeneratorDiagnostic.g.cs", ""),
                    (typeof(ExperimentProxyGenerator), "LoggerExperimentProxy.g.cs", "")
                }
            }
        }.RunAsync();
    }

    [Fact]
    public async Task Generator_WithGenericInterface_GeneratesCorrectProxy()
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

        // Act & Assert
        await new ExperimentProxyGeneratorVerifier
        {
            TestState =
            {
                Sources = { source },
                ReferenceAssemblies = ReferenceAssemblies.Net.Net80,
                AdditionalReferences = { GetExperimentFrameworkReference() },
                GeneratedSources =
                {
                    (typeof(ExperimentProxyGenerator), "GeneratorDiagnostic.g.cs", ""),
                    // Generic interfaces get special naming
                    (typeof(ExperimentProxyGenerator), "RepositoryExperimentProxy.g.cs", "")
                }
            }
        }.RunAsync();
    }

    /// <summary>
    /// Gets a reference to the ExperimentFramework assembly for testing.
    /// </summary>
    private static MetadataReference GetExperimentFrameworkReference()
    {
        // Reference the actual ExperimentFramework assembly
        var assemblyPath = typeof(ExperimentFramework.ExperimentFrameworkBuilder).Assembly.Location;
        return MetadataReference.CreateFromFile(assemblyPath);
    }
}

/// <summary>
/// Custom test class for source generator testing with proper configuration.
/// </summary>
file class ExperimentProxyGeneratorVerifier : CSharpSourceGeneratorTest<ExperimentProxyGenerator, XUnitVerifier>
{
    protected override CompilationOptions CreateCompilationOptions()
    {
        var options = base.CreateCompilationOptions();
        return options.WithSpecificDiagnosticOptions(
            options.SpecificDiagnosticOptions.SetItems(GetNullableWarningsFromCompiler()));
    }

    private static ImmutableDictionary<string, ReportDiagnostic> GetNullableWarningsFromCompiler()
    {
        string[] args = { "/warnaserror:nullable" };
        var commandLineArguments = CSharpCommandLineParser.Default.Parse(args, baseDirectory: Environment.CurrentDirectory, sdkDirectory: Environment.CurrentDirectory);
        var nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions;

        return nullableWarnings;
    }

    protected override ParseOptions CreateParseOptions()
    {
        return new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose);
    }
}
