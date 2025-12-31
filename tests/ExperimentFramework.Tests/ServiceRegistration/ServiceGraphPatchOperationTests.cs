using ExperimentFramework.ServiceRegistration;
using Microsoft.Extensions.DependencyInjection;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.ServiceRegistration;

[Feature("Patch operations mutate service collection safely")]
public class ServiceGraphPatchOperationTests(ITestOutputHelper output) : TinyBddXunitBase(output)
{
    [Scenario("Replace operation removes and adds descriptors")]
    [Fact]
    public Task Replace_operation_removes_and_adds()
        => Given("a service collection with one registration", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, OriginalImpl>();
                return services;
            })
            .And("a replace operation", services =>
            {
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[]
                    {
                        ServiceDescriptor.Singleton<ITestService, NewImpl>()
                    },
                    expectedMatchCount: 1
                );
                return (services, operation);
            })
            .When("executing the operation", context => 
            {
                var result = context.operation.Execute(context.services);
                return (context.services, result);
            })
            .Then("operation should succeed", context => context.result.Success)
            .And("should have matched one descriptor", context => context.result.MatchCount == 1)
            .And("should have removed one descriptor", context => context.result.RemovedDescriptors.Count == 1)
            .And("should have added one descriptor", context => context.result.AddedDescriptors.Count == 1)
            .And("service should now resolve to new implementation", context =>
            {
                var provider = context.services.BuildServiceProvider();
                var service = provider.GetService<ITestService>();
                return service is NewImpl;
            })
            .AssertPassed();

    [Scenario("Insert operation adds before matched descriptor")]
    [Fact]
    public Task Insert_operation_adds_before_match()
        => Given("a service collection with multiple registrations", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, FirstImpl>();
                services.AddSingleton<ITestService, SecondImpl>();
                services.AddSingleton<ITestService, ThirdImpl>();
                return services;
            })
            .And("an insert operation", services =>
            {
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Insert,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[]
                    {
                        ServiceDescriptor.Singleton<ITestService, InsertedImpl>()
                    }
                );
                return (services, operation);
            })
            .When("executing the operation", context =>
            {
                var result = context.operation.Execute(context.services);
                return (context.services, result);
            })
            .Then("operation should succeed", context => context.result.Success)
            .And("total count should increase", context => 
                context.services.Count(d => d.ServiceType == typeof(ITestService)) == 4)
            .And("inserted service should be first", context =>
            {
                var firstDescriptor = context.services.First(d => d.ServiceType == typeof(ITestService));
                return firstDescriptor.ImplementationType == typeof(InsertedImpl);
            })
            .AssertPassed();

    [Scenario("Append operation adds after matched descriptors")]
    [Fact]
    public Task Append_operation_adds_after_matches()
        => Given("a service collection with multiple registrations", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, FirstImpl>();
                services.AddSingleton<ITestService, SecondImpl>();
                return services;
            })
            .And("an append operation", services =>
            {
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Append,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[]
                    {
                        ServiceDescriptor.Singleton<ITestService, AppendedImpl>()
                    }
                );
                return (services, operation);
            })
            .When("executing the operation", context =>
            {
                var result = context.operation.Execute(context.services);
                return (context.services, result);
            })
            .Then("operation should succeed", context => context.result.Success)
            .And("total count should increase", context => 
                context.services.Count(d => d.ServiceType == typeof(ITestService)) == 3)
            .And("appended service should be last", context =>
            {
                var lastDescriptor = context.services.Last(d => d.ServiceType == typeof(ITestService));
                return lastDescriptor.ImplementationType == typeof(AppendedImpl);
            })
            .AssertPassed();

    [Scenario("Merge operation replaces all matches")]
    [Fact]
    public Task Merge_operation_replaces_all()
        => Given("a service collection with multiple registrations", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, FirstImpl>();
                services.AddSingleton<ITestService, SecondImpl>();
                services.AddSingleton<ITestService, ThirdImpl>();
                return services;
            })
            .And("a merge operation", services =>
            {
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Merge,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[]
                    {
                        ServiceDescriptor.Singleton<ITestService, MergedImpl>()
                    }
                );
                return (services, operation);
            })
            .When("executing the operation", context =>
            {
                var result = context.operation.Execute(context.services);
                return (context.services, result);
            })
            .Then("operation should succeed", context => context.result.Success)
            .And("should match multiple descriptors", context => context.result.MatchCount == 3)
            .And("should have only one registration remaining", context =>
                context.services.Count(d => d.ServiceType == typeof(ITestService)) == 1)
            .And("remaining service should be merged impl", context =>
            {
                var descriptor = context.services.First(d => d.ServiceType == typeof(ITestService));
                return descriptor.ImplementationType == typeof(MergedImpl);
            })
            .AssertPassed();

    [Scenario("Operation fails with wrong match count")]
    [Fact]
    public Task Operation_fails_with_wrong_match_count()
        => Given("a service collection with two registrations", () =>
            {
                var services = new ServiceCollection();
                services.AddSingleton<ITestService, FirstImpl>();
                services.AddSingleton<ITestService, SecondImpl>();
                return services;
            })
            .And("an operation expecting exactly one match", services =>
            {
                var operation = new ServiceGraphPatchOperation(
                    operationId: "test",
                    operationType: MultiRegistrationBehavior.Replace,
                    serviceType: typeof(ITestService),
                    matchPredicate: d => d.ServiceType == typeof(ITestService),
                    newDescriptors: new[]
                    {
                        ServiceDescriptor.Singleton<ITestService, NewImpl>()
                    },
                    expectedMatchCount: 1 // But there are 2!
                );
                return (services, operation);
            })
            .When("executing the operation", context => context.operation.Execute(context.services))
            .Then("operation should fail", result => !result.Success)
            .And("error message should mention match count", result => 
                result.ErrorMessage?.Contains("match") == true)
            .AssertPassed();

    // Test interfaces and implementations
    private interface ITestService { }
    private class OriginalImpl : ITestService { }
    private class NewImpl : ITestService { }
    private class FirstImpl : ITestService { }
    private class SecondImpl : ITestService { }
    private class ThirdImpl : ITestService { }
    private class InsertedImpl : ITestService { }
    private class AppendedImpl : ITestService { }
    private class MergedImpl : ITestService { }
}
