using ExperimentFramework.Simulation.Comparators;

namespace ExperimentFramework.Simulation.Tests;

public class ComparatorTests
{
    [Fact]
    public void EqualityComparator_DetectsNoDifferencesForEqualValues()
    {
        // Arrange
        var comparator = new EqualityComparator<int>();

        // Act
        var differences = comparator.Compare(42, 42, "test");

        // Assert
        Assert.Empty(differences);
    }

    [Fact]
    public void EqualityComparator_DetectsDifferencesForUnequalValues()
    {
        // Arrange
        var comparator = new EqualityComparator<int>();

        // Act
        var differences = comparator.Compare(42, 100, "test");

        // Assert
        Assert.NotEmpty(differences);
        Assert.Contains("test", differences[0]);
    }

    [Fact]
    public void EqualityComparator_HandlesNullValues()
    {
        // Arrange
        var comparator = new EqualityComparator<string>();

        // Act
        var bothNull = comparator.Compare(null, null, "test");
        var controlNull = comparator.Compare(null, "value", "test");
        var conditionNull = comparator.Compare("value", null, "test");

        // Assert
        Assert.Empty(bothNull);
        Assert.NotEmpty(controlNull);
        Assert.NotEmpty(conditionNull);
    }

    [Fact]
    public void JsonComparator_DetectsNoDifferencesForEqualObjects()
    {
        // Arrange
        var comparator = new JsonComparator<TestObject>();
        var obj1 = new TestObject { Name = "Test", Value = 42 };
        var obj2 = new TestObject { Name = "Test", Value = 42 };

        // Act
        var differences = comparator.Compare(obj1, obj2, "test");

        // Assert
        Assert.Empty(differences);
    }

    [Fact]
    public void JsonComparator_DetectsDifferencesForUnequalObjects()
    {
        // Arrange
        var comparator = new JsonComparator<TestObject>();
        var obj1 = new TestObject { Name = "Test", Value = 42 };
        var obj2 = new TestObject { Name = "Test", Value = 100 };

        // Act
        var differences = comparator.Compare(obj1, obj2, "test");

        // Assert
        Assert.NotEmpty(differences);
        Assert.Contains("test", differences[0]);
    }

    [Fact]
    public void JsonComparator_HandlesNullValues()
    {
        // Arrange
        var comparator = new JsonComparator<TestObject>();

        // Act
        var bothNull = comparator.Compare(null, null, "test");
        var controlNull = comparator.Compare(null, new TestObject { Name = "Test", Value = 42 }, "test");
        var conditionNull = comparator.Compare(new TestObject { Name = "Test", Value = 42 }, null, "test");

        // Assert
        Assert.Empty(bothNull);
        Assert.NotEmpty(controlNull);
        Assert.NotEmpty(conditionNull);
    }

    [Fact]
    public void SimulationComparators_CreateEqualityComparator()
    {
        // Act
        var comparator = SimulationComparators.Equality<int>();

        // Assert
        Assert.NotNull(comparator);
        Assert.IsType<EqualityComparator<int>>(comparator);
    }

    [Fact]
    public void SimulationComparators_CreateJsonComparator()
    {
        // Act
        var comparator = SimulationComparators.Json<TestObject>();

        // Assert
        Assert.NotNull(comparator);
        Assert.IsType<JsonComparator<TestObject>>(comparator);
    }

    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
        public int Value { get; set; }
    }
}
