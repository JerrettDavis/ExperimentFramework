using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExperimentFramework.Science.Reporting;

/// <summary>
/// Generates experiment reports in JSON format.
/// </summary>
/// <remarks>
/// Produces machine-readable reports suitable for:
/// <list type="bullet">
/// <item><description>API responses</description></item>
/// <item><description>Data pipelines</description></item>
/// <item><description>Dashboard integrations</description></item>
/// <item><description>Long-term storage</description></item>
/// </list>
/// </remarks>
public sealed class JsonReporter : IExperimentReporter
{
    private readonly JsonSerializerOptions _jsonOptions;

    /// <summary>
    /// Creates a new JSON reporter with default options.
    /// </summary>
    public JsonReporter() : this(indented: true)
    {
    }

    /// <summary>
    /// Creates a new JSON reporter.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    public JsonReporter(bool indented)
    {
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            }
        };
    }

    /// <summary>
    /// Creates a new JSON reporter with custom serializer options.
    /// </summary>
    /// <param name="options">The JSON serializer options to use.</param>
    public JsonReporter(JsonSerializerOptions options)
    {
        _jsonOptions = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task<string> GenerateAsync(ExperimentReport report, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(report);

        var json = JsonSerializer.Serialize(report, _jsonOptions);
        return Task.FromResult(json);
    }
}
