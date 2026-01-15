using System.CommandLine;
using ExperimentFramework.Cli.Commands;

namespace ExperimentFramework.Cli;

/// <summary>
/// Command-line tool for validating and diagnosing ExperimentFramework configurations.
/// </summary>
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("ExperimentFramework CLI - Validate configurations and diagnose issues")
        {
            DoctorCommand.Create(),
            ConfigCommand.Create(),
            PlanCommand.Create()
        };

        return await rootCommand.InvokeAsync(args);
    }
}
