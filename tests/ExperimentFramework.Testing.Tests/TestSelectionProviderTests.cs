using ExperimentFramework.Naming;
using ExperimentFramework.Selection;

namespace ExperimentFramework.Testing.Tests;

public class TestSelectionProviderTests
{
    [Fact]
    public async Task SelectTrialKeyAsync_WithForcedSelection_ReturnsSelectedKey()
    {
        // Arrange
        var provider = new TestSelectionProvider();
        var context = new SelectionContext
        {
            ServiceProvider = null!,
            SelectorName = "Test",
            TrialKeys = new[] { "control", "true" },
            DefaultKey = "control",
            ServiceType = typeof(IMyDatabase)
        };

        using var scope = ExperimentTestScope.Begin()
            .ForceCondition<IMyDatabase>("true");

        // Act
        var result = await provider.SelectTrialKeyAsync(context);

        // Assert
        Assert.Equal("true", result);
    }

    [Fact]
    public async Task SelectTrialKeyAsync_WithInvalidKey_ReturnsDefault()
    {
        // Arrange
        var provider = new TestSelectionProvider();
        var context = new SelectionContext
        {
            ServiceProvider = null!,
            SelectorName = "Test",
            TrialKeys = new[] { "control", "true" },
            DefaultKey = "control",
            ServiceType = typeof(IMyDatabase)
        };

        using var scope = ExperimentTestScope.Begin()
            .ForceCondition<IMyDatabase>("invalid-key");

        // Act
        var result = await provider.SelectTrialKeyAsync(context);

        // Assert
        Assert.Equal("control", result);
    }

    [Fact]
    public async Task SelectTrialKeyAsync_WithFrozenSelection_MaintainsSelection()
    {
        // Arrange
        var provider = new TestSelectionProvider();
        var context = new SelectionContext
        {
            ServiceProvider = null!,
            SelectorName = "Test",
            TrialKeys = new[] { "control", "true" },
            DefaultKey = "control",
            ServiceType = typeof(IMyDatabase)
        };

        using var scope = ExperimentTestScope.Begin()
            .ForceCondition<IMyDatabase>("true")
            .FreezeSelection();

        // Act - First call
        var result1 = await provider.SelectTrialKeyAsync(context);
        
        // Act - Second call should return the same frozen selection
        var result2 = await provider.SelectTrialKeyAsync(context);

        // Assert
        Assert.Equal("true", result1);
        Assert.Equal("true", result2);
    }

    [Fact]
    public async Task SelectTrialKeyAsync_WithoutForcedSelection_ReturnsDefault()
    {
        // Arrange
        var provider = new TestSelectionProvider();
        var context = new SelectionContext
        {
            ServiceProvider = null!,
            SelectorName = "Test",
            TrialKeys = new[] { "control", "true" },
            DefaultKey = "control",
            ServiceType = typeof(IMyDatabase)
        };

        // Act (no scope, no forced selection)
        var result = await provider.SelectTrialKeyAsync(context);

        // Assert
        Assert.Equal("control", result);
    }

    [Fact]
    public void GetDefaultSelectorName_ReturnsExpectedFormat()
    {
        // Arrange
        var provider = new TestSelectionProvider();
        var convention = new DefaultExperimentNamingConvention();

        // Act
        var result = provider.GetDefaultSelectorName(typeof(IMyDatabase), convention);

        // Assert
        Assert.Equal("Test_IMyDatabase", result);
    }
}
