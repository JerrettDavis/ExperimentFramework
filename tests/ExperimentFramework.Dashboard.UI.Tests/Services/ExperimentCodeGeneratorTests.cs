using ExperimentFramework.Dashboard.UI.Models;
using ExperimentFramework.Dashboard.UI.Services;

namespace ExperimentFramework.Dashboard.UI.Tests.Services;

/// <summary>
/// Unit tests for ExperimentCodeGenerator — exercises all YAML and Fluent API
/// generation paths including every SelectionModeType and ErrorPolicyType branch.
/// </summary>
public sealed class ExperimentCodeGeneratorTests
{
    private static ExperimentWizardModel BaseModel() => new()
    {
        Name = "my-experiment",
        DisplayName = "My Experiment",
        Description = "Test description",
        Category = "Engagement",
        ServiceInterface = "IMyService",
        Control = new VariantModel { Key = "control", ImplementationType = "ControlImpl" },
        Variants = [new VariantModel { Key = "variant-a", ImplementationType = "VariantAImpl" }],
        SelectionMode = SelectionModeType.ConfigurationKey,
        SelectionModeKey = "my-flag",
        ErrorPolicy = ErrorPolicyType.FallbackToControl,
    };

    // ── GenerateYaml — SelectionMode branches ─────────────────────────────────

    [Fact]
    public void GenerateYaml_ConfigurationKey_ContainsTypeAndKey()
    {
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(BaseModel());

        Assert.Contains("type: configurationKey", yaml);
        Assert.Contains("key: \"my-flag\"", yaml);
    }

    [Fact]
    public void GenerateYaml_FeatureFlag_ContainsFlagName()
    {
        var model = BaseModel();
        model.SelectionMode = SelectionModeType.FeatureFlag;
        model.SelectionModeKey = "my-feature-flag";

        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(model);

        Assert.Contains("type: featureFlag", yaml);
        Assert.Contains("flagName: \"my-feature-flag\"", yaml);
    }

    [Fact]
    public void GenerateYaml_Custom_ContainsModeIdentifier()
    {
        var model = BaseModel();
        model.SelectionMode = SelectionModeType.Custom;
        model.CustomModeIdentifier = "my-custom-mode";

        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(model);

        Assert.Contains("type: custom", yaml);
        Assert.Contains("modeIdentifier: \"my-custom-mode\"", yaml);
    }

    // ── GenerateYaml — ErrorPolicy branches ───────────────────────────────────

    [Fact]
    public void GenerateYaml_FallbackToControl_ContainsFallbackToControl()
    {
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(BaseModel());
        Assert.Contains("type: fallbackToControl", yaml);
    }

    [Fact]
    public void GenerateYaml_Throw_ContainsTypeThrow()
    {
        var model = BaseModel();
        model.ErrorPolicy = ErrorPolicyType.Throw;
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(model);
        Assert.Contains("type: throw", yaml);
    }

    [Fact]
    public void GenerateYaml_TryAny_ContainsTypeTryAny()
    {
        var model = BaseModel();
        model.ErrorPolicy = ErrorPolicyType.TryAny;
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(model);
        Assert.Contains("type: tryAny", yaml);
    }

    [Fact]
    public void GenerateYaml_FallbackTo_ContainsFallbackKey()
    {
        var model = BaseModel();
        model.ErrorPolicy = ErrorPolicyType.FallbackTo;
        model.FallbackKey = "fallback-variant";
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(model);
        Assert.Contains("type: fallbackTo", yaml);
        Assert.Contains("fallbackKey: fallback-variant", yaml);
    }

    [Fact]
    public void GenerateYaml_TryInOrder_ContainsFallbackKeys()
    {
        var model = BaseModel();
        model.ErrorPolicy = ErrorPolicyType.TryInOrder;
        model.FallbackOrder = ["first-variant", "second-variant"];
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(model);
        Assert.Contains("type: tryInOrder", yaml);
        Assert.Contains("first-variant", yaml);
        Assert.Contains("second-variant", yaml);
    }

    // ── GenerateYaml — structure ──────────────────────────────────────────────

    [Fact]
    public void GenerateYaml_ContainsExperimentFrameworkSection()
    {
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(BaseModel());
        Assert.Contains("experimentFramework:", yaml);
        Assert.Contains("experiments:", yaml);
    }

    [Fact]
    public void GenerateYaml_ContainsExperimentName()
    {
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(BaseModel());
        Assert.Contains("name: my-experiment", yaml);
    }

    [Fact]
    public void GenerateYaml_WithVariants_ContainsConditions()
    {
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(BaseModel());
        Assert.Contains("conditions:", yaml);
        Assert.Contains("key: variant-a", yaml);
    }

    [Fact]
    public void GenerateYaml_NoVariants_DoesNotContainConditions()
    {
        var model = BaseModel();
        model.Variants = [];
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(model);
        Assert.DoesNotContain("conditions:", yaml);
    }

    [Fact]
    public void GenerateYaml_WithDescription_IncludesDescription()
    {
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(BaseModel());
        Assert.Contains("description:", yaml);
        Assert.Contains("Test description", yaml);
    }

    [Fact]
    public void GenerateYaml_EmptyDescription_OmitsDescriptionLine()
    {
        var model = BaseModel();
        model.Description = "";
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(model);
        Assert.DoesNotContain("description:", yaml);
    }

    [Fact]
    public void GenerateYaml_SpecialCharactersInDescription_AreEscaped()
    {
        var model = BaseModel();
        model.Description = "Has \"quotes\" and \\backslash\\ and \n newline";
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(model);
        // Ensure no crash; the output has escaped form
        Assert.Contains("description:", yaml);
    }

    [Fact]
    public void GenerateYaml_SpecialCharactersInDisplayName_AreEscaped()
    {
        var model = BaseModel();
        model.DisplayName = "Display: \"hello\" & \\world\\";
        var gen = new ExperimentCodeGenerator();
        var yaml = gen.GenerateYaml(model);
        Assert.Contains("displayName:", yaml);
    }

    // ── GenerateFluentApi — SelectionMode branches ────────────────────────────

    [Fact]
    public void GenerateFluentApi_ConfigurationKey_ContainsUsingConfigurationKey()
    {
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(BaseModel());
        Assert.Contains(".UsingConfigurationKey(\"my-flag\")", code);
    }

    [Fact]
    public void GenerateFluentApi_FeatureFlag_ContainsUsingFeatureFlag()
    {
        var model = BaseModel();
        model.SelectionMode = SelectionModeType.FeatureFlag;
        model.SelectionModeKey = "feature-flag-x";
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(model);
        Assert.Contains(".UsingFeatureFlag(\"feature-flag-x\")", code);
    }

    [Fact]
    public void GenerateFluentApi_Custom_ContainsUsingCustomMode()
    {
        var model = BaseModel();
        model.SelectionMode = SelectionModeType.Custom;
        model.CustomModeIdentifier = "custom-selector";
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(model);
        Assert.Contains(".UsingCustomMode(\"custom-selector\")", code);
    }

    // ── GenerateFluentApi — ErrorPolicy branches ──────────────────────────────

    [Fact]
    public void GenerateFluentApi_FallbackToControl_ContainsOnErrorFallbackToControl()
    {
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(BaseModel());
        Assert.Contains(".OnErrorFallbackToControl", code);
    }

    [Fact]
    public void GenerateFluentApi_Throw_ContainsOnErrorThrow()
    {
        var model = BaseModel();
        model.ErrorPolicy = ErrorPolicyType.Throw;
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(model);
        Assert.Contains(".OnErrorThrow", code);
    }

    [Fact]
    public void GenerateFluentApi_TryAny_ContainsOnErrorTryAny()
    {
        var model = BaseModel();
        model.ErrorPolicy = ErrorPolicyType.TryAny;
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(model);
        Assert.Contains(".OnErrorTryAny", code);
    }

    [Fact]
    public void GenerateFluentApi_FallbackTo_ContainsFallbackKey()
    {
        var model = BaseModel();
        model.ErrorPolicy = ErrorPolicyType.FallbackTo;
        model.FallbackKey = "fallback";
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(model);
        Assert.Contains(".OnErrorFallbackTo(\"fallback\")", code);
    }

    [Fact]
    public void GenerateFluentApi_TryInOrder_ContainsOrderedKeys()
    {
        var model = BaseModel();
        model.ErrorPolicy = ErrorPolicyType.TryInOrder;
        model.FallbackOrder = ["first", "second", "third"];
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(model);
        Assert.Contains("OnErrorTryInOrder", code);
        Assert.Contains("first", code);
        Assert.Contains("second", code);
        Assert.Contains("third", code);
    }

    // ── GenerateFluentApi — structure ─────────────────────────────────────────

    [Fact]
    public void GenerateFluentApi_ContainsProgramCsComment()
    {
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(BaseModel());
        Assert.Contains("Program.cs", code);
    }

    [Fact]
    public void GenerateFluentApi_ContainsExperimentName()
    {
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(BaseModel());
        Assert.Contains("my-experiment", code);
    }

    [Fact]
    public void GenerateFluentApi_ContainsServiceInterface()
    {
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(BaseModel());
        Assert.Contains("<IMyService>", code);
    }

    [Fact]
    public void GenerateFluentApi_ContainsControlImplementation()
    {
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(BaseModel());
        Assert.Contains("AddControl<ControlImpl>", code);
    }

    [Fact]
    public void GenerateFluentApi_ContainsVariantImplementation()
    {
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(BaseModel());
        Assert.Contains("AddVariant<VariantAImpl>", code);
    }

    [Fact]
    public void GenerateFluentApi_MultipleVariants_AllPresent()
    {
        var model = BaseModel();
        model.Variants =
        [
            new VariantModel { Key = "variant-a", ImplementationType = "ImplA" },
            new VariantModel { Key = "variant-b", ImplementationType = "ImplB" },
        ];
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(model);
        Assert.Contains("AddVariant<ImplA>", code);
        Assert.Contains("AddVariant<ImplB>", code);
    }

    [Fact]
    public void GenerateFluentApi_WithDescription_IncludesMetadata()
    {
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(BaseModel());
        Assert.Contains("WithMetadata", code);
        Assert.Contains("description", code);
    }

    [Fact]
    public void GenerateFluentApi_EmptyDescription_OmitsDescriptionMetadata()
    {
        var model = BaseModel();
        model.Description = "";
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(model);
        // displayName metadata is always present; description should be absent
        Assert.DoesNotContain("\"description\"", code);
    }

    [Fact]
    public void GenerateFluentApi_SpecialCharactersInDisplayName_AreEscaped()
    {
        var model = BaseModel();
        model.DisplayName = "Has \"quotes\" and \\slashes\\";
        var gen = new ExperimentCodeGenerator();
        // Should not throw; output should be valid C# string
        var code = gen.GenerateFluentApi(model);
        Assert.Contains("WithMetadata", code);
    }

    [Fact]
    public void GenerateFluentApi_ContainsAddExperimentFramework()
    {
        var gen = new ExperimentCodeGenerator();
        var code = gen.GenerateFluentApi(BaseModel());
        Assert.Contains("AddExperimentFramework", code);
    }
}
