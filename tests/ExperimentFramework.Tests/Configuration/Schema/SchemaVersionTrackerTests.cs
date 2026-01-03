using ExperimentFramework.Configuration.Schema;
using TinyBDD;
using TinyBDD.Xunit;
using Xunit.Abstractions;

namespace ExperimentFramework.Tests.Configuration.Schema;

[Feature("Schema version tracker manages version history based on hash changes")]
public class SchemaVersionTrackerTests : TinyBddXunitBase, IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly List<string> _tempFiles = new();

    public SchemaVersionTrackerTests(ITestOutputHelper output) : base(output)
    {
        _output = output;
    }
    
    private string GetTempHistoryFile()
    {
        var file = Path.Combine(Path.GetTempPath(), $"schema-history-{Guid.NewGuid()}.json");
        _tempFiles.Add(file);
        return file;
    }

    public new void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            try
            {
                if (File.Exists(file)) File.Delete(file);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
        GC.SuppressFinalize(this);
    }

    [Scenario("First time seeing extension returns version 1.0.0")]
    [Fact]
    public Task GetVersionForHash_first_time_returns_1_0_0()
        => Given("a new version tracker and new extension", () =>
            {
                var historyFile = GetTempHistoryFile();
                var tracker = new SchemaVersionTracker(historyFile);
                return tracker;
            })
            .When("getting version for first hash", tracker =>
                tracker.GetVersionForHash("TestExtension", "abc123"))
            .Then("version should be 1.0.0", version => version == "1.0.0")
            .AssertPassed();

    [Scenario("Same hash returns same version")]
    [Fact]
    public Task GetVersionForHash_same_hash_same_version()
        => Given("a version tracker with existing extension", () =>
            {
                var historyFile = GetTempHistoryFile();
                var tracker = new SchemaVersionTracker(historyFile);
                tracker.GetVersionForHash("TestExtension", "hash123");
                return tracker;
            })
            .When("getting version for same hash again", tracker =>
            {
                var version1 = tracker.GetVersionForHash("TestExtension", "hash123");
                var version2 = tracker.GetVersionForHash("TestExtension", "hash123");
                return (version1, version2);
            })
            .Then("versions should match", result => result.version1 == result.version2)
            .And("version should be 1.0.0", result => result.version1 == "1.0.0")
            .AssertPassed();

    [Scenario("Different hash increments version")]
    [Fact]
    public Task GetVersionForHash_different_hash_increments_version()
        => Given("a version tracker with existing extension", () =>
            {
                var historyFile = GetTempHistoryFile();
                var tracker = new SchemaVersionTracker(historyFile);
                var version1 = tracker.GetVersionForHash("TestExtension", "hash1");
                return (tracker, version1);
            })
            .When("getting version for different hash", state =>
                state.tracker.GetVersionForHash("TestExtension", "hash2"))
            .Then("version should be incremented", version => version == "1.0.1")
            .AssertPassed();

    [Scenario("Multiple hash changes increment version sequentially")]
    [Fact]
    public Task GetVersionForHash_multiple_changes_increment_sequentially()
        => Given("a version tracker", () =>
            {
                var historyFile = GetTempHistoryFile();
                var tracker = new SchemaVersionTracker(historyFile);
                return tracker;
            })
            .When("changing hash multiple times", tracker =>
            {
                var v1 = tracker.GetVersionForHash("TestExt", "hash1");
                var v2 = tracker.GetVersionForHash("TestExt", "hash2");
                var v3 = tracker.GetVersionForHash("TestExt", "hash3");
                var v4 = tracker.GetVersionForHash("TestExt", "hash4");
                return new[] { v1, v2, v3, v4 };
            })
            .Then("versions should increment", versions =>
                versions[0] == "1.0.0" &&
                versions[1] == "1.0.1" &&
                versions[2] == "1.0.2" &&
                versions[3] == "1.0.3")
            .AssertPassed();

    [Scenario("Different extensions have independent versions")]
    [Fact]
    public Task GetVersionForHash_different_extensions_independent()
        => Given("a version tracker", () =>
            {
                var historyFile = GetTempHistoryFile();
                var tracker = new SchemaVersionTracker(historyFile);
                return tracker;
            })
            .When("getting versions for different extensions", tracker =>
            {
                var vExt1 = tracker.GetVersionForHash("Extension1", "hash1");
                var vExt2 = tracker.GetVersionForHash("Extension2", "hash2");
                var vExt1Again = tracker.GetVersionForHash("Extension1", "hash1");
                return (vExt1, vExt2, vExt1Again);
            })
            .Then("both extensions start at 1.0.0", result =>
                result.vExt1 == "1.0.0" && result.vExt2 == "1.0.0")
            .And("extension 1 version unchanged", result => result.vExt1Again == "1.0.0")
            .AssertPassed();

    [Scenario("SaveHistory persists version information")]
    [Fact]
    public Task SaveHistory_persists_data()
        => Given("a version tracker with data", () =>
            {
                var historyFile = GetTempHistoryFile();
                var tracker = new SchemaVersionTracker(historyFile);
                tracker.GetVersionForHash("TestExt", "hash1");
                tracker.GetVersionForHash("TestExt", "hash2");
                tracker.SaveHistory();
                return historyFile;
            })
            .When("loading in new tracker", historyFile =>
            {
                var newTracker = new SchemaVersionTracker(historyFile);
                return newTracker;
            })
            .Then("version should be persisted", tracker =>
                tracker.GetVersionForHash("TestExt", "hash2") == "1.0.1")
            .AssertPassed();

    [Scenario("GetHistory returns complete version history")]
    [Fact]
    public Task GetHistory_returns_complete_history()
        => Given("a version tracker with changes", () =>
            {
                var historyFile = GetTempHistoryFile();
                var tracker = new SchemaVersionTracker(historyFile);
                tracker.GetVersionForHash("Ext1", "hash1");
                tracker.GetVersionForHash("Ext1", "hash2");
                tracker.GetVersionForHash("Ext2", "hashA");
                return tracker;
            })
            .When("getting history", tracker => tracker.GetHistory())
            .Then("history contains both extensions", history =>
                history.Extensions.ContainsKey("Ext1") &&
                history.Extensions.ContainsKey("Ext2"))
            .And("Ext1 has version 1.0.1", history =>
                history.Extensions["Ext1"].CurrentVersion == "1.0.1")
            .And("Ext1 has one history entry", history =>
                history.Extensions["Ext1"].VersionHistory.Count == 1)
            .And("Ext2 has version 1.0.0", history =>
                history.Extensions["Ext2"].CurrentVersion == "1.0.0")
            .AssertPassed();

    [Scenario("Corrupted history file starts fresh")]
    [Fact]
    public Task Corrupted_history_starts_fresh()
        => Given("a corrupted history file", () =>
            {
                var historyFile = GetTempHistoryFile();
                File.WriteAllText(historyFile, "{ invalid json !");
                return historyFile;
            })
            .When("creating tracker with corrupted file", historyFile =>
            {
                var tracker = new SchemaVersionTracker(historyFile);
                return tracker;
            })
            .Then("should start with fresh history", tracker =>
                tracker.GetHistory().Extensions.Count == 0)
            .And("should handle new extensions", tracker =>
                tracker.GetVersionForHash("NewExt", "hash1") == "1.0.0")
            .AssertPassed();
}
