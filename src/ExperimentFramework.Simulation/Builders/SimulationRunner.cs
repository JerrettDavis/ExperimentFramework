using System.Diagnostics;
using ExperimentFramework.Simulation.Comparators;
using ExperimentFramework.Simulation.Models;
using Microsoft.Extensions.DependencyInjection;

namespace ExperimentFramework.Simulation.Builders;

/// <summary>
/// Builder for configuring and running simulations.
/// </summary>
/// <typeparam name="TService">The service interface type.</typeparam>
/// <typeparam name="TResult">The result type.</typeparam>
public sealed class SimulationRunner<TService, TResult>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly List<string> _conditionNames = new();
    private ISimulationComparator<TResult> _comparator = new EqualityComparator<TResult>();
    private readonly ShadowModeOptions _options = new();
    private string _controlName = "control";

    internal SimulationRunner(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Configures the control implementation.
    /// </summary>
    /// <param name="name">Optional control name (defaults to "control").</param>
    /// <returns>This builder for chaining.</returns>
    public SimulationRunner<TService, TResult> Control(string? name = null)
    {
        _controlName = name ?? "control";
        return this;
    }

    /// <summary>
    /// Adds a condition to evaluate.
    /// </summary>
    /// <param name="name">The condition name (typically matches the experiment selection key).</param>
    /// <returns>This builder for chaining.</returns>
    public SimulationRunner<TService, TResult> Condition(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Condition name cannot be null or empty.", nameof(name));

        _conditionNames.Add(name);
        return this;
    }

    /// <summary>
    /// Sets the comparator to use for comparing results.
    /// </summary>
    /// <param name="comparator">The comparator instance.</param>
    /// <returns>This builder for chaining.</returns>
    public SimulationRunner<TService, TResult> WithComparator(ISimulationComparator<TResult> comparator)
    {
        _comparator = comparator ?? throw new ArgumentNullException(nameof(comparator));
        return this;
    }

    /// <summary>
    /// Configures the runner to return the control result.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public SimulationRunner<TService, TResult> ReturnControlResult()
    {
        _options.ReturnMode = ResultReturnMode.Control;
        return this;
    }

    /// <summary>
    /// Configures the runner to return the selected result.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public SimulationRunner<TService, TResult> ReturnSelectedResult()
    {
        _options.ReturnMode = ResultReturnMode.Selected;
        return this;
    }

    /// <summary>
    /// Configures the runner to fail if differences are detected.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    public SimulationRunner<TService, TResult> FailIfDifferent()
    {
        _options.FailOnDifference = true;
        return this;
    }

    /// <summary>
    /// Runs the simulation with the provided scenarios.
    /// </summary>
    /// <param name="scenarios">The scenarios to execute.</param>
    /// <returns>A simulation report containing all results.</returns>
    public async Task<SimulationReport> RunAsync(IEnumerable<Scenario<TService, TResult>> scenarios)
    {
        if (scenarios == null) throw new ArgumentNullException(nameof(scenarios));

        var scenarioList = scenarios.ToList();
        if (!scenarioList.Any())
            throw new ArgumentException("At least one scenario must be provided.", nameof(scenarios));

        var scenarioResults = new List<ScenarioResult<TResult>>();

        foreach (var scenario in scenarioList)
        {
            var result = await ExecuteScenarioAsync(scenario);
            scenarioResults.Add(result);
        }

        var allPassed = scenarioResults.All(r => r.AllSucceeded && (!_options.FailOnDifference || !r.HasDifferences));
        var summary = GenerateSummary(scenarioResults, allPassed);

        return new SimulationReport(
            typeof(TService).Name,
            _controlName,
            _conditionNames,
            scenarioResults.Cast<IScenarioResult>().ToList(),
            allPassed,
            summary);
    }

    private async Task<ScenarioResult<TResult>> ExecuteScenarioAsync(Scenario<TService, TResult> scenario)
    {
        using var scope = _serviceProvider.CreateScope();

        // Execute control
        var controlResult = await ExecuteImplementationAsync(scope, scenario, _controlName);

        // Execute conditions
        var conditionResults = new List<ImplementationResult<TResult>>();
        foreach (var conditionName in _conditionNames)
        {
            var conditionResult = await ExecuteImplementationAsync(scope, scenario, conditionName);
            conditionResults.Add(conditionResult);
        }

        // Compare results
        var differences = new List<string>();
        if (controlResult.Success)
        {
            foreach (var condition in conditionResults)
            {
                if (condition.Success)
                {
                    var diffs = _comparator.Compare(controlResult.Result, condition.Result, condition.ImplementationName);
                    differences.AddRange(diffs);
                }
                else
                {
                    differences.Add($"{condition.ImplementationName}: Execution failed - {condition.Exception?.Message}");
                }
            }
        }
        else
        {
            differences.Add($"Control execution failed - {controlResult.Exception?.Message}");
        }

        // Determine selected implementation based on ReturnMode
        var selectedImplementation = _options.ReturnMode == ResultReturnMode.Control 
            ? _controlName 
            : (_conditionNames.FirstOrDefault() ?? _controlName);

        return new ScenarioResult<TResult>(
            scenario.Name,
            controlResult,
            conditionResults,
            selectedImplementation,
            differences);
    }

    private async Task<ImplementationResult<TResult>> ExecuteImplementationAsync(
        IServiceScope scope,
        Scenario<TService, TResult> scenario,
        string implementationName)
    {
        var stopwatch = Stopwatch.StartNew();
        TResult? result = default;
        Exception? exception = null;

        try
        {
            TService service;
            try
            {
                // Try to resolve a keyed implementation first, using the implementation name as the key.
                service = scope.ServiceProvider.GetRequiredKeyedService<TService>(implementationName);
            }
            catch (InvalidOperationException)
            {
                // Fallback to the default implementation if no keyed service is registered.
                service = scope.ServiceProvider.GetRequiredService<TService>();
            }
            
            result = await scenario.Execute(service);
        }
        catch (Exception ex)
        {
            exception = ex;
        }
        finally
        {
            stopwatch.Stop();
        }

        return new ImplementationResult<TResult>(implementationName, result, exception, stopwatch.Elapsed);
    }

    private string GenerateSummary(List<ScenarioResult<TResult>> scenarioResults, bool allPassed)
    {
        var totalScenarios = scenarioResults.Count;
        var successfulScenarios = scenarioResults.Count(r => r.AllSucceeded);
        var scenariosWithDifferences = scenarioResults.Count(r => r.HasDifferences);

        return $"Total Scenarios: {totalScenarios}, " +
               $"Successful: {successfulScenarios}, " +
               $"With Differences: {scenariosWithDifferences}, " +
               $"Overall: {(allPassed ? "PASSED" : "FAILED")}";
    }
}
