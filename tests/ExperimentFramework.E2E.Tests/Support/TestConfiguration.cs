using System.Text.Json;

namespace ExperimentFramework.E2E.Tests.Support;

/// <summary>
/// Reads E2E test settings from environment variables (highest priority) or
/// appsettings.e2e.json (fallback). No extra NuGet packages required.
/// </summary>
public class TestConfiguration
{
    // Parsed once at construction and cached.
    private readonly Dictionary<string, string> _json;

    public TestConfiguration()
    {
        _json = LoadJson();
    }

    public string BaseUrl =>
        Env("E2E__BaseUrl") ?? Env("E2E_BASEURL") ?? Json("BaseUrl") ?? "https://localhost:7201";

    public bool Headless
    {
        get
        {
            var raw = Env("E2E__Headless") ?? Env("E2E_HEADLESS") ?? Json("Headless");
            return bool.TryParse(raw, out var v) ? v : true;
        }
    }

    public int DefaultTimeoutMs
    {
        get
        {
            var raw = Env("E2E__DefaultTimeoutMs") ?? Env("E2E_DEFAULTTIMEOUTMS") ?? Json("DefaultTimeoutMs");
            return int.TryParse(raw, out var v) ? v : 30000;
        }
    }

    public int SlowMo
    {
        get
        {
            var raw = Env("E2E__SlowMo") ?? Env("E2E_SLOWMO") ?? Json("SlowMo");
            return int.TryParse(raw, out var v) ? v : 0;
        }
    }

    // -------------------------------------------------------------------------

    private static string? Env(string key) =>
        Environment.GetEnvironmentVariable(key) is { Length: > 0 } v ? v : null;

    private string? Json(string key) =>
        _json.TryGetValue(key, out var v) ? v : null;

    private static Dictionary<string, string> LoadJson()
    {
        // Look beside the assembly, then the CWD.
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "appsettings.e2e.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "appsettings.e2e.json")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            try
            {
                var text = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty("E2E", out var section))
                {
                    return section.EnumerateObject()
                        .ToDictionary(p => p.Name, p => p.Value.ToString());
                }
            }
            catch { /* malformed JSON — fall through to defaults */ }
        }

        return new Dictionary<string, string>();
    }
}
