using System.Linq;
using System.Text;
using ExperimentFramework.Generators.Models;
using Microsoft.CodeAnalysis;

namespace ExperimentFramework.Generators.CodeGen;

/// <summary>
/// Generates strongly-typed method implementations for proxy classes.
/// Handles all 5 return types: void, Task, Task&lt;T&gt;, ValueTask, ValueTask&lt;T&gt;.
/// </summary>
internal static class MethodGenerator
{
    // Symbol display format that preserves nullability annotations
    private static readonly SymbolDisplayFormat NullableAwareFormat = new SymbolDisplayFormat(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier |
                              SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    /// <summary>
    /// Generates a complete method implementation for a proxy class.
    /// </summary>
    public static void GenerateMethod(
        StringBuilder sb,
        IMethodSymbol method,
        ExperimentDefinitionModel experiment)
    {
        var returnType = DetermineReturnType(method);
        var serviceTypeName = experiment.ServiceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Method signature
        AppendMethodSignature(sb, method);
        sb.AppendLine("        {");

        // Method body depends on return type
        switch (returnType)
        {
            case ReturnTypeKind.Void:
                GenerateVoidMethodBody(sb, method, experiment, serviceTypeName);
                break;

            case ReturnTypeKind.Sync:
                GenerateSyncMethodBody(sb, method, experiment, serviceTypeName);
                break;

            case ReturnTypeKind.Task:
                GenerateTaskMethodBody(sb, method, experiment, serviceTypeName);
                break;

            case ReturnTypeKind.TaskOfT:
                GenerateTaskOfTMethodBody(sb, method, experiment, serviceTypeName);
                break;

            case ReturnTypeKind.ValueTask:
                GenerateValueTaskMethodBody(sb, method, experiment, serviceTypeName);
                break;

            case ReturnTypeKind.ValueTaskOfT:
                GenerateValueTaskOfTMethodBody(sb, method, experiment, serviceTypeName);
                break;
        }

        sb.AppendLine("        }");
    }

    private static void AppendMethodSignature(StringBuilder sb, IMethodSymbol method)
    {
        var returnTypeName = method.ReturnType.ToDisplayString(NullableAwareFormat);
        var methodName = method.Name;
        var parameters = string.Join(", ", method.Parameters.Select(FormatParameter));

        sb.AppendLine($"        public {returnTypeName} {methodName}({parameters})");
    }

    private static string FormatParameter(IParameterSymbol parameter)
    {
        var typeName = parameter.Type.ToDisplayString(NullableAwareFormat);
        var modifiers = "";

        if (parameter.RefKind == RefKind.Ref)
            modifiers = "ref ";
        else if (parameter.RefKind == RefKind.Out)
            modifiers = "out ";
        else if (parameter.RefKind == RefKind.In)
            modifiers = "in ";

        return $"{modifiers}{typeName} {parameter.Name}";
    }

    private static void GenerateVoidMethodBody(
        StringBuilder sb,
        IMethodSymbol method,
        ExperimentDefinitionModel experiment,
        string serviceTypeName)
    {
        var methodName = method.Name;
        var argumentList = string.Join(", ", method.Parameters.Select(p => p.Name));

        sb.AppendLine("            using var scope = _scopeFactory.CreateScope();");
        sb.AppendLine("            var sp = scope.ServiceProvider;");
        sb.AppendLine("            var pipeline = new global::ExperimentFramework.Decorators.DecoratorPipeline(_decoratorFactories, sp);");
        sb.AppendLine();
        sb.AppendLine("            var preferredKey = SelectTrialKey(sp);");
        sb.AppendLine("            var candidates = BuildCandidateKeys(preferredKey);");
        sb.AppendLine();
        sb.AppendLine($"            using (var telemetryScope = _telemetry.StartInvocation(typeof({serviceTypeName}), \"{methodName}\", _registration.SelectorName, preferredKey, candidates))");
        sb.AppendLine("            {");
        sb.AppendLine("                System.Exception? lastException = null;");
        sb.AppendLine();
        sb.AppendLine("                foreach (var key in candidates)");
        sb.AppendLine("                {");
        sb.AppendLine("                    try");
        sb.AppendLine("                    {");
        sb.AppendLine("                        var implType = _registration.Trials.TryGetValue(key, out var t) ? t : _registration.Trials[_registration.DefaultKey];");
        sb.AppendLine($"                        var impl = ({serviceTypeName})sp.GetRequiredService(implType);");
        sb.AppendLine();
        sb.AppendLine("                        object? Terminal()");
        sb.AppendLine("                        {");
        sb.AppendLine($"                            impl.{methodName}({argumentList});");
        sb.AppendLine("                            return null;");
        sb.AppendLine("                        }");
        sb.AppendLine();
        sb.AppendLine($"                        var ctx = new global::ExperimentFramework.Decorators.InvocationContext(typeof({serviceTypeName}), \"{methodName}\", key, new object?[] {{ {argumentList} }});");
        sb.AppendLine("                        pipeline.Invoke(ctx, Terminal);");
        sb.AppendLine();
        sb.AppendLine("                        telemetryScope.RecordSuccess();");
        sb.AppendLine("                        return;");
        sb.AppendLine("                    }");
        sb.AppendLine("                    catch (System.Exception ex)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        lastException = ex;");
        sb.AppendLine("                        if (_registration.OnErrorPolicy == global::ExperimentFramework.Models.OnErrorPolicy.Throw)");
        sb.AppendLine("                        {");
        sb.AppendLine("                            telemetryScope.RecordFailure(ex);");
        sb.AppendLine("                            throw;");
        sb.AppendLine("                        }");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                telemetryScope.RecordFailure(lastException!);");
        sb.AppendLine("                throw lastException!;");
        sb.AppendLine("            }");
    }

    private static void GenerateSyncMethodBody(
        StringBuilder sb,
        IMethodSymbol method,
        ExperimentDefinitionModel experiment,
        string serviceTypeName)
    {
        var methodName = method.Name;
        var argumentList = string.Join(", ", method.Parameters.Select(p => p.Name));
        var resultTypeName = method.ReturnType.ToDisplayString(NullableAwareFormat);

        sb.AppendLine("            using var scope = _scopeFactory.CreateScope();");
        sb.AppendLine("            var sp = scope.ServiceProvider;");
        sb.AppendLine("            var pipeline = new global::ExperimentFramework.Decorators.DecoratorPipeline(_decoratorFactories, sp);");
        sb.AppendLine();
        sb.AppendLine("            var preferredKey = SelectTrialKey(sp);");
        sb.AppendLine("            var candidates = BuildCandidateKeys(preferredKey);");
        sb.AppendLine();
        sb.AppendLine($"            using (var telemetryScope = _telemetry.StartInvocation(typeof({serviceTypeName}), \"{methodName}\", _registration.SelectorName, preferredKey, candidates))");
        sb.AppendLine("            {");
        sb.AppendLine("                System.Exception? lastException = null;");
        sb.AppendLine();
        sb.AppendLine("                foreach (var key in candidates)");
        sb.AppendLine("                {");
        sb.AppendLine("                    try");
        sb.AppendLine("                    {");
        sb.AppendLine("                        var implType = _registration.Trials.TryGetValue(key, out var t) ? t : _registration.Trials[_registration.DefaultKey];");
        sb.AppendLine($"                        var impl = ({serviceTypeName})sp.GetRequiredService(implType);");
        sb.AppendLine();
        sb.AppendLine("                        object? Terminal()");
        sb.AppendLine("                        {");
        sb.AppendLine($"                            var result = impl.{methodName}({argumentList});");
        sb.AppendLine("                            return result;");
        sb.AppendLine("                        }");
        sb.AppendLine();
        sb.AppendLine($"                        var ctx = new global::ExperimentFramework.Decorators.InvocationContext(typeof({serviceTypeName}), \"{methodName}\", key, new object?[] {{ {argumentList} }});");
        sb.AppendLine("                        var boxedResult = pipeline.Invoke(ctx, Terminal);");
        sb.AppendLine();
        sb.AppendLine("                        telemetryScope.RecordSuccess();");
        sb.AppendLine($"                        return ({resultTypeName})boxedResult!;");
        sb.AppendLine("                    }");
        sb.AppendLine("                    catch (System.Exception ex)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        lastException = ex;");
        sb.AppendLine("                        if (_registration.OnErrorPolicy == global::ExperimentFramework.Models.OnErrorPolicy.Throw)");
        sb.AppendLine("                        {");
        sb.AppendLine("                            telemetryScope.RecordFailure(ex);");
        sb.AppendLine("                            throw;");
        sb.AppendLine("                        }");
        sb.AppendLine("                    }");
        sb.AppendLine("                }");
        sb.AppendLine();
        sb.AppendLine("                telemetryScope.RecordFailure(lastException!);");
        sb.AppendLine("                throw lastException!;");
        sb.AppendLine("            }");
    }

    private static void GenerateTaskMethodBody(
        StringBuilder sb,
        IMethodSymbol method,
        ExperimentDefinitionModel experiment,
        string serviceTypeName)
    {
        var methodName = method.Name;
        var argumentList = string.Join(", ", method.Parameters.Select(p => p.Name));

        sb.AppendLine("            using var scope = _scopeFactory.CreateScope();");
        sb.AppendLine("            var sp = scope.ServiceProvider;");
        sb.AppendLine("            var pipeline = new global::ExperimentFramework.Decorators.DecoratorPipeline(_decoratorFactories, sp);");
        sb.AppendLine();
        sb.AppendLine("            var preferredKey = SelectTrialKey(sp);");
        sb.AppendLine("            var candidates = BuildCandidateKeys(preferredKey);");
        sb.AppendLine();
        sb.AppendLine($"            using (var telemetryScope = _telemetry.StartInvocation(typeof({serviceTypeName}), \"{methodName}\", _registration.SelectorName, preferredKey, candidates))");
        sb.AppendLine("            {");
        sb.AppendLine("                return InvokeTaskAsync();");
        sb.AppendLine();
        sb.AppendLine("                async System.Threading.Tasks.Task InvokeTaskAsync()");
        sb.AppendLine("                {");
        sb.AppendLine("                    System.Exception? lastException = null;");
        sb.AppendLine();
        sb.AppendLine("                    foreach (var key in candidates)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        try");
        sb.AppendLine("                        {");
        sb.AppendLine("                            var implType = _registration.Trials.TryGetValue(key, out var t) ? t : _registration.Trials[_registration.DefaultKey];");
        sb.AppendLine($"                            var impl = ({serviceTypeName})sp.GetRequiredService(implType);");
        sb.AppendLine();
        sb.AppendLine("                            async System.Threading.Tasks.ValueTask<object?> Terminal()");
        sb.AppendLine("                            {");
        sb.AppendLine($"                                await impl.{methodName}({argumentList}).ConfigureAwait(false);");
        sb.AppendLine("                                return null;");
        sb.AppendLine("                            }");
        sb.AppendLine();
        sb.AppendLine($"                            var ctx = new global::ExperimentFramework.Decorators.InvocationContext(typeof({serviceTypeName}), \"{methodName}\", key, new object?[] {{ {argumentList} }});");
        sb.AppendLine("                            await pipeline.InvokeAsync(ctx, Terminal).ConfigureAwait(false);");
        sb.AppendLine();
        sb.AppendLine("                            telemetryScope.RecordSuccess();");
        sb.AppendLine("                            return;");
        sb.AppendLine("                        }");
        sb.AppendLine("                        catch (System.Exception ex)");
        sb.AppendLine("                        {");
        sb.AppendLine("                            lastException = ex;");
        sb.AppendLine("                            if (_registration.OnErrorPolicy == global::ExperimentFramework.Models.OnErrorPolicy.Throw)");
        sb.AppendLine("                            {");
        sb.AppendLine("                                telemetryScope.RecordFailure(ex);");
        sb.AppendLine("                                throw;");
        sb.AppendLine("                            }");
        sb.AppendLine("                        }");
        sb.AppendLine("                    }");
        sb.AppendLine();
        sb.AppendLine("                    telemetryScope.RecordFailure(lastException!);");
        sb.AppendLine("                    throw lastException!;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
    }

    private static void GenerateTaskOfTMethodBody(
        StringBuilder sb,
        IMethodSymbol method,
        ExperimentDefinitionModel experiment,
        string serviceTypeName)
    {
        var methodName = method.Name;
        var argumentList = string.Join(", ", method.Parameters.Select(p => p.Name));

        // Extract T from Task<T>
        var namedReturnType = (INamedTypeSymbol)method.ReturnType;
        var resultType = namedReturnType.TypeArguments[0];
        var resultTypeName = resultType.ToDisplayString(NullableAwareFormat);

        sb.AppendLine("            using var scope = _scopeFactory.CreateScope();");
        sb.AppendLine("            var sp = scope.ServiceProvider;");
        sb.AppendLine("            var pipeline = new global::ExperimentFramework.Decorators.DecoratorPipeline(_decoratorFactories, sp);");
        sb.AppendLine();
        sb.AppendLine("            var preferredKey = SelectTrialKey(sp);");
        sb.AppendLine("            var candidates = BuildCandidateKeys(preferredKey);");
        sb.AppendLine();
        sb.AppendLine($"            using (var telemetryScope = _telemetry.StartInvocation(typeof({serviceTypeName}), \"{methodName}\", _registration.SelectorName, preferredKey, candidates))");
        sb.AppendLine("            {");
        sb.AppendLine("                return InvokeTaskAsync();");
        sb.AppendLine();
        sb.AppendLine($"                async System.Threading.Tasks.Task<{resultTypeName}> InvokeTaskAsync()");
        sb.AppendLine("                {");
        sb.AppendLine("                    System.Exception? lastException = null;");
        sb.AppendLine();
        sb.AppendLine("                    foreach (var key in candidates)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        try");
        sb.AppendLine("                        {");
        sb.AppendLine("                            var implType = _registration.Trials.TryGetValue(key, out var t) ? t : _registration.Trials[_registration.DefaultKey];");
        sb.AppendLine($"                            var impl = ({serviceTypeName})sp.GetRequiredService(implType);");
        sb.AppendLine();
        sb.AppendLine("                            async System.Threading.Tasks.ValueTask<object?> Terminal()");
        sb.AppendLine("                            {");
        sb.AppendLine($"                                var result = await impl.{methodName}({argumentList}).ConfigureAwait(false);");
        sb.AppendLine("                                return result;");
        sb.AppendLine("                            }");
        sb.AppendLine();
        sb.AppendLine($"                            var ctx = new global::ExperimentFramework.Decorators.InvocationContext(typeof({serviceTypeName}), \"{methodName}\", key, new object?[] {{ {argumentList} }});");
        sb.AppendLine("                            var boxedResult = await pipeline.InvokeAsync(ctx, Terminal).ConfigureAwait(false);");
        sb.AppendLine();
        sb.AppendLine("                            telemetryScope.RecordSuccess();");
        sb.AppendLine($"                            return ({resultTypeName})boxedResult!;");
        sb.AppendLine("                        }");
        sb.AppendLine("                        catch (System.Exception ex)");
        sb.AppendLine("                        {");
        sb.AppendLine("                            lastException = ex;");
        sb.AppendLine("                            if (_registration.OnErrorPolicy == global::ExperimentFramework.Models.OnErrorPolicy.Throw)");
        sb.AppendLine("                            {");
        sb.AppendLine("                                telemetryScope.RecordFailure(ex);");
        sb.AppendLine("                                throw;");
        sb.AppendLine("                            }");
        sb.AppendLine("                        }");
        sb.AppendLine("                    }");
        sb.AppendLine();
        sb.AppendLine("                    telemetryScope.RecordFailure(lastException!);");
        sb.AppendLine("                    throw lastException!;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
    }

    private static void GenerateValueTaskMethodBody(
        StringBuilder sb,
        IMethodSymbol method,
        ExperimentDefinitionModel experiment,
        string serviceTypeName)
    {
        var methodName = method.Name;
        var argumentList = string.Join(", ", method.Parameters.Select(p => p.Name));

        sb.AppendLine("            using var scope = _scopeFactory.CreateScope();");
        sb.AppendLine("            var sp = scope.ServiceProvider;");
        sb.AppendLine("            var pipeline = new global::ExperimentFramework.Decorators.DecoratorPipeline(_decoratorFactories, sp);");
        sb.AppendLine();
        sb.AppendLine("            var preferredKey = SelectTrialKey(sp);");
        sb.AppendLine("            var candidates = BuildCandidateKeys(preferredKey);");
        sb.AppendLine();
        sb.AppendLine($"            using (var telemetryScope = _telemetry.StartInvocation(typeof({serviceTypeName}), \"{methodName}\", _registration.SelectorName, preferredKey, candidates))");
        sb.AppendLine("            {");
        sb.AppendLine("                return new System.Threading.Tasks.ValueTask(InvokeTaskAsync());");
        sb.AppendLine();
        sb.AppendLine("                async System.Threading.Tasks.Task InvokeTaskAsync()");
        sb.AppendLine("                {");
        sb.AppendLine("                    System.Exception? lastException = null;");
        sb.AppendLine();
        sb.AppendLine("                    foreach (var key in candidates)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        try");
        sb.AppendLine("                        {");
        sb.AppendLine("                            var implType = _registration.Trials.TryGetValue(key, out var t) ? t : _registration.Trials[_registration.DefaultKey];");
        sb.AppendLine($"                            var impl = ({serviceTypeName})sp.GetRequiredService(implType);");
        sb.AppendLine();
        sb.AppendLine("                            async System.Threading.Tasks.ValueTask<object?> Terminal()");
        sb.AppendLine("                            {");
        sb.AppendLine($"                                await impl.{methodName}({argumentList}).ConfigureAwait(false);");
        sb.AppendLine("                                return null;");
        sb.AppendLine("                            }");
        sb.AppendLine();
        sb.AppendLine($"                            var ctx = new global::ExperimentFramework.Decorators.InvocationContext(typeof({serviceTypeName}), \"{methodName}\", key, new object?[] {{ {argumentList} }});");
        sb.AppendLine("                            await pipeline.InvokeAsync(ctx, Terminal).ConfigureAwait(false);");
        sb.AppendLine();
        sb.AppendLine("                            telemetryScope.RecordSuccess();");
        sb.AppendLine("                            return;");
        sb.AppendLine("                        }");
        sb.AppendLine("                        catch (System.Exception ex)");
        sb.AppendLine("                        {");
        sb.AppendLine("                            lastException = ex;");
        sb.AppendLine("                            if (_registration.OnErrorPolicy == global::ExperimentFramework.Models.OnErrorPolicy.Throw)");
        sb.AppendLine("                            {");
        sb.AppendLine("                                telemetryScope.RecordFailure(ex);");
        sb.AppendLine("                                throw;");
        sb.AppendLine("                            }");
        sb.AppendLine("                        }");
        sb.AppendLine("                    }");
        sb.AppendLine();
        sb.AppendLine("                    telemetryScope.RecordFailure(lastException!);");
        sb.AppendLine("                    throw lastException!;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
    }

    private static void GenerateValueTaskOfTMethodBody(
        StringBuilder sb,
        IMethodSymbol method,
        ExperimentDefinitionModel experiment,
        string serviceTypeName)
    {
        var methodName = method.Name;
        var argumentList = string.Join(", ", method.Parameters.Select(p => p.Name));

        // Extract T from ValueTask<T>
        var namedReturnType = (INamedTypeSymbol)method.ReturnType;
        var resultType = namedReturnType.TypeArguments[0];
        var resultTypeName = resultType.ToDisplayString(NullableAwareFormat);

        sb.AppendLine("            using var scope = _scopeFactory.CreateScope();");
        sb.AppendLine("            var sp = scope.ServiceProvider;");
        sb.AppendLine("            var pipeline = new global::ExperimentFramework.Decorators.DecoratorPipeline(_decoratorFactories, sp);");
        sb.AppendLine();
        sb.AppendLine("            var preferredKey = SelectTrialKey(sp);");
        sb.AppendLine("            var candidates = BuildCandidateKeys(preferredKey);");
        sb.AppendLine();
        sb.AppendLine($"            using (var telemetryScope = _telemetry.StartInvocation(typeof({serviceTypeName}), \"{methodName}\", _registration.SelectorName, preferredKey, candidates))");
        sb.AppendLine("            {");
        sb.AppendLine($"                return new System.Threading.Tasks.ValueTask<{resultTypeName}>(InvokeTaskAsync());");
        sb.AppendLine();
        sb.AppendLine($"                async System.Threading.Tasks.Task<{resultTypeName}> InvokeTaskAsync()");
        sb.AppendLine("                {");
        sb.AppendLine("                    System.Exception? lastException = null;");
        sb.AppendLine();
        sb.AppendLine("                    foreach (var key in candidates)");
        sb.AppendLine("                    {");
        sb.AppendLine("                        try");
        sb.AppendLine("                        {");
        sb.AppendLine("                            var implType = _registration.Trials.TryGetValue(key, out var t) ? t : _registration.Trials[_registration.DefaultKey];");
        sb.AppendLine($"                            var impl = ({serviceTypeName})sp.GetRequiredService(implType);");
        sb.AppendLine();
        sb.AppendLine("                            async System.Threading.Tasks.ValueTask<object?> Terminal()");
        sb.AppendLine("                            {");
        sb.AppendLine($"                                var result = await impl.{methodName}({argumentList}).ConfigureAwait(false);");
        sb.AppendLine("                                return result;");
        sb.AppendLine("                            }");
        sb.AppendLine();
        sb.AppendLine($"                            var ctx = new global::ExperimentFramework.Decorators.InvocationContext(typeof({serviceTypeName}), \"{methodName}\", key, new object?[] {{ {argumentList} }});");
        sb.AppendLine("                            var boxedResult = await pipeline.InvokeAsync(ctx, Terminal).ConfigureAwait(false);");
        sb.AppendLine();
        sb.AppendLine("                            telemetryScope.RecordSuccess();");
        sb.AppendLine($"                            return ({resultTypeName})boxedResult!;");
        sb.AppendLine("                        }");
        sb.AppendLine("                        catch (System.Exception ex)");
        sb.AppendLine("                        {");
        sb.AppendLine("                            lastException = ex;");
        sb.AppendLine("                            if (_registration.OnErrorPolicy == global::ExperimentFramework.Models.OnErrorPolicy.Throw)");
        sb.AppendLine("                            {");
        sb.AppendLine("                                telemetryScope.RecordFailure(ex);");
        sb.AppendLine("                                throw;");
        sb.AppendLine("                            }");
        sb.AppendLine("                        }");
        sb.AppendLine("                    }");
        sb.AppendLine();
        sb.AppendLine("                    telemetryScope.RecordFailure(lastException!);");
        sb.AppendLine("                    throw lastException!;");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
    }

    private static ReturnTypeKind DetermineReturnType(IMethodSymbol method)
    {
        var returnType = method.ReturnType;

        // Check for void
        if (returnType.SpecialType == SpecialType.System_Void)
        {
            return ReturnTypeKind.Void;
        }

        // Check for Task, Task<T>, ValueTask, ValueTask<T>
        if (returnType is INamedTypeSymbol namedType)
        {
            var fullName = namedType.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            if (fullName == "global::System.Threading.Tasks.Task")
            {
                return ReturnTypeKind.Task;
            }

            if (fullName == "global::System.Threading.Tasks.Task<TResult>")
            {
                return ReturnTypeKind.TaskOfT;
            }

            if (fullName == "global::System.Threading.Tasks.ValueTask")
            {
                return ReturnTypeKind.ValueTask;
            }

            if (fullName == "global::System.Threading.Tasks.ValueTask<TResult>")
            {
                return ReturnTypeKind.ValueTaskOfT;
            }
        }

        // Synchronous non-void return type
        return ReturnTypeKind.Sync;
    }

    private enum ReturnTypeKind
    {
        Void,
        Sync,
        Task,
        TaskOfT,
        ValueTask,
        ValueTaskOfT
    }
}
