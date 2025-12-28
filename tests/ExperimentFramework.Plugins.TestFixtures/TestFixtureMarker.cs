namespace ExperimentFramework.Plugins.TestFixtures;

/// <summary>
/// Marker class for the test fixtures assembly.
/// This assembly contains embedded resources and attributes for testing ManifestLoader.
/// </summary>
public static class TestFixtureMarker
{
    /// <summary>
    /// Gets the assembly containing test fixtures.
    /// </summary>
    public static System.Reflection.Assembly Assembly => typeof(TestFixtureMarker).Assembly;
}
