using ExperimentFramework.Dashboard.UI.Models;

namespace ExperimentFramework.Dashboard.UI.Tests.Services;

/// <summary>
/// Unit tests for ExperimentWizardModel validation and reset logic.
/// </summary>
public sealed class ExperimentWizardModelTests
{
    // ── ValidateStep1 ─────────────────────────────────────────────────────────

    [Fact]
    public void ValidateStep1_ValidModel_IsValid()
    {
        var model = new ExperimentWizardModel
        {
            Name = "my-experiment",
            DisplayName = "My Experiment",
            Category = "Engagement"
        };
        var (isValid, errors) = model.ValidateStep1();
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateStep1_EmptyName_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            Name = "",
            DisplayName = "Display",
            Category = "Engagement"
        };
        var (isValid, errors) = model.ValidateStep1();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("name"));
    }

    [Fact]
    public void ValidateStep1_InvalidNameFormat_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            Name = "My Experiment",  // spaces not allowed
            DisplayName = "My Experiment",
            Category = "Engagement"
        };
        var (isValid, errors) = model.ValidateStep1();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("kebab-case"));
    }

    [Fact]
    public void ValidateStep1_InvalidNameWithUpperCase_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            Name = "MyExperiment",
            DisplayName = "My Experiment",
            Category = "Engagement"
        };
        var (isValid, errors) = model.ValidateStep1();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("kebab-case"));
    }

    [Fact]
    public void ValidateStep1_ValidKebabCase_IsValid()
    {
        var names = new[] { "a", "a-b", "a-b-c", "exp-001", "my-exp-2024" };
        foreach (var name in names)
        {
            var model = new ExperimentWizardModel { Name = name, DisplayName = "X", Category = "Y" };
            var (isValid, _) = model.ValidateStep1();
            Assert.True(isValid, $"Expected '{name}' to be valid");
        }
    }

    [Fact]
    public void ValidateStep1_EmptyDisplayName_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            Name = "my-exp",
            DisplayName = "",
            Category = "Engagement"
        };
        var (isValid, errors) = model.ValidateStep1();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Display name"));
    }

    [Fact]
    public void ValidateStep1_EmptyCategory_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            Name = "my-exp",
            DisplayName = "My Exp",
            Category = ""
        };
        var (isValid, errors) = model.ValidateStep1();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Category"));
    }

    [Fact]
    public void ValidateStep1_MultipleErrors_AllReported()
    {
        var model = new ExperimentWizardModel { Name = "", DisplayName = "", Category = "" };
        var (isValid, errors) = model.ValidateStep1();
        Assert.False(isValid);
        Assert.True(errors.Count >= 2);  // at least name + display name errors
    }

    // ── ValidateStep2 ─────────────────────────────────────────────────────────

    [Fact]
    public void ValidateStep2_ValidModel_IsValid()
    {
        var model = new ExperimentWizardModel
        {
            ServiceInterface = "IMyService",
            Control = new VariantModel { Key = "control", ImplementationType = "ControlImpl" },
            Variants = [new VariantModel { Key = "variant-a", ImplementationType = "ImplA" }]
        };
        var (isValid, errors) = model.ValidateStep2();
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateStep2_EmptyServiceInterface_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            ServiceInterface = "",
            Control = new VariantModel { Key = "control", ImplementationType = "Impl" },
            Variants = [new VariantModel { Key = "v1", ImplementationType = "ImplV" }]
        };
        var (isValid, errors) = model.ValidateStep2();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Service interface"));
    }

    [Fact]
    public void ValidateStep2_EmptyControlKey_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            ServiceInterface = "IFoo",
            Control = new VariantModel { Key = "", ImplementationType = "Impl" },
            Variants = [new VariantModel { Key = "v1", ImplementationType = "ImplV" }]
        };
        var (isValid, errors) = model.ValidateStep2();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Control variant key"));
    }

    [Fact]
    public void ValidateStep2_EmptyControlImplementationType_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            ServiceInterface = "IFoo",
            Control = new VariantModel { Key = "control", ImplementationType = "" },
            Variants = [new VariantModel { Key = "v1", ImplementationType = "ImplV" }]
        };
        var (isValid, errors) = model.ValidateStep2();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Control implementation type"));
    }

    [Fact]
    public void ValidateStep2_NoVariants_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            ServiceInterface = "IFoo",
            Control = new VariantModel { Key = "control", ImplementationType = "Impl" },
            Variants = []
        };
        var (isValid, errors) = model.ValidateStep2();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("variant"));
    }

    [Fact]
    public void ValidateStep2_VariantMissingKey_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            ServiceInterface = "IFoo",
            Control = new VariantModel { Key = "control", ImplementationType = "Impl" },
            Variants = [new VariantModel { Key = "", ImplementationType = "ImplV" }]
        };
        var (isValid, errors) = model.ValidateStep2();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Key is required"));
    }

    [Fact]
    public void ValidateStep2_VariantMissingImplementationType_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            ServiceInterface = "IFoo",
            Control = new VariantModel { Key = "control", ImplementationType = "Impl" },
            Variants = [new VariantModel { Key = "v1", ImplementationType = "" }]
        };
        var (isValid, errors) = model.ValidateStep2();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Implementation type"));
    }

    [Fact]
    public void ValidateStep2_DuplicateKeys_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            ServiceInterface = "IFoo",
            Control = new VariantModel { Key = "shared-key", ImplementationType = "ImplA" },
            Variants = [new VariantModel { Key = "shared-key", ImplementationType = "ImplB" }]
        };
        var (isValid, errors) = model.ValidateStep2();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Duplicate"));
    }

    [Fact]
    public void ValidateStep2_MultipleVariantsWithDuplicates_ReportsDuplicate()
    {
        var model = new ExperimentWizardModel
        {
            ServiceInterface = "IFoo",
            Control = new VariantModel { Key = "control", ImplementationType = "Impl" },
            Variants =
            [
                new VariantModel { Key = "variant-a", ImplementationType = "ImplA" },
                new VariantModel { Key = "variant-a", ImplementationType = "ImplB" }  // duplicate
            ]
        };
        var (isValid, errors) = model.ValidateStep2();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("variant-a"));
    }

    // ── ValidateStep3 ─────────────────────────────────────────────────────────

    [Fact]
    public void ValidateStep3_ConfigurationKey_Valid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.ConfigurationKey,
            SelectionModeKey = "my-config-key",
            ErrorPolicy = ErrorPolicyType.FallbackToControl
        };
        var (isValid, errors) = model.ValidateStep3();
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    [Fact]
    public void ValidateStep3_ConfigurationKey_EmptyKey_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.ConfigurationKey,
            SelectionModeKey = "",
            ErrorPolicy = ErrorPolicyType.FallbackToControl
        };
        var (isValid, errors) = model.ValidateStep3();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Configuration key"));
    }

    [Fact]
    public void ValidateStep3_FeatureFlag_EmptyFlagName_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.FeatureFlag,
            SelectionModeKey = "",
            ErrorPolicy = ErrorPolicyType.FallbackToControl
        };
        var (isValid, errors) = model.ValidateStep3();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Feature flag name"));
    }

    [Fact]
    public void ValidateStep3_FeatureFlag_ValidFlagName_IsValid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.FeatureFlag,
            SelectionModeKey = "my-feature-flag",
            ErrorPolicy = ErrorPolicyType.FallbackToControl
        };
        var (isValid, _) = model.ValidateStep3();
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateStep3_Custom_EmptyModeIdentifier_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.Custom,
            CustomModeIdentifier = "",
            ErrorPolicy = ErrorPolicyType.FallbackToControl
        };
        var (isValid, errors) = model.ValidateStep3();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Custom mode identifier"));
    }

    [Fact]
    public void ValidateStep3_Custom_ValidIdentifier_IsValid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.Custom,
            CustomModeIdentifier = "my-custom",
            ErrorPolicy = ErrorPolicyType.FallbackToControl
        };
        var (isValid, _) = model.ValidateStep3();
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateStep3_FallbackTo_EmptyFallbackKey_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.ConfigurationKey,
            SelectionModeKey = "key",
            ErrorPolicy = ErrorPolicyType.FallbackTo,
            FallbackKey = ""
        };
        var (isValid, errors) = model.ValidateStep3();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Fallback key"));
    }

    [Fact]
    public void ValidateStep3_FallbackTo_WithFallbackKey_IsValid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.ConfigurationKey,
            SelectionModeKey = "key",
            ErrorPolicy = ErrorPolicyType.FallbackTo,
            FallbackKey = "my-fallback"
        };
        var (isValid, _) = model.ValidateStep3();
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateStep3_TryInOrder_EmptyFallbackOrder_IsInvalid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.ConfigurationKey,
            SelectionModeKey = "key",
            ErrorPolicy = ErrorPolicyType.TryInOrder,
            FallbackOrder = []
        };
        var (isValid, errors) = model.ValidateStep3();
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("fallback key"));
    }

    [Fact]
    public void ValidateStep3_TryInOrder_WithFallbackOrder_IsValid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.ConfigurationKey,
            SelectionModeKey = "key",
            ErrorPolicy = ErrorPolicyType.TryInOrder,
            FallbackOrder = ["first", "second"]
        };
        var (isValid, _) = model.ValidateStep3();
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateStep3_Throw_IsValid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.ConfigurationKey,
            SelectionModeKey = "key",
            ErrorPolicy = ErrorPolicyType.Throw
        };
        var (isValid, _) = model.ValidateStep3();
        Assert.True(isValid);
    }

    [Fact]
    public void ValidateStep3_TryAny_IsValid()
    {
        var model = new ExperimentWizardModel
        {
            SelectionMode = SelectionModeType.ConfigurationKey,
            SelectionModeKey = "key",
            ErrorPolicy = ErrorPolicyType.TryAny
        };
        var (isValid, _) = model.ValidateStep3();
        Assert.True(isValid);
    }

    // ── Reset ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Reset_ClearsAllFields()
    {
        var model = new ExperimentWizardModel
        {
            Name = "some-exp",
            DisplayName = "Some Exp",
            Description = "Some desc",
            Category = "Custom",
            ServiceInterface = "IFoo",
            Control = new VariantModel { Key = "control", ImplementationType = "ControlImpl" },
            Variants = [new VariantModel { Key = "v1", ImplementationType = "Impl1" }],
            SelectionMode = SelectionModeType.FeatureFlag,
            SelectionModeKey = "feature-flag",
            CustomModeIdentifier = "custom-mode",
            ErrorPolicy = ErrorPolicyType.Throw,
            FallbackKey = "fallback",
            FallbackOrder = ["first"]
        };

        model.Reset();

        Assert.Equal("", model.Name);
        Assert.Equal("", model.DisplayName);
        Assert.Equal("", model.Description);
        Assert.Equal("Engagement", model.Category);
        Assert.Equal("", model.ServiceInterface);
        Assert.Equal("", model.Control.Key);
        Assert.Equal("", model.Control.ImplementationType);
        Assert.Single(model.Variants);  // reset creates one empty variant
        Assert.Equal(SelectionModeType.ConfigurationKey, model.SelectionMode);
        Assert.Equal("", model.SelectionModeKey);
        Assert.Equal("", model.CustomModeIdentifier);
        Assert.Equal(ErrorPolicyType.FallbackToControl, model.ErrorPolicy);
        Assert.Equal("", model.FallbackKey);
        Assert.Empty(model.FallbackOrder);
    }

    [Fact]
    public void Reset_NewVariant_IsEmpty()
    {
        var model = new ExperimentWizardModel
        {
            Variants = [new VariantModel { Key = "v1", ImplementationType = "Impl" }]
        };
        model.Reset();
        Assert.Equal("", model.Variants[0].Key);
        Assert.Equal("", model.Variants[0].ImplementationType);
    }

    // ── Defaults ──────────────────────────────────────────────────────────────

    [Fact]
    public void DefaultModel_HasExpectedDefaults()
    {
        var model = new ExperimentWizardModel();
        Assert.Equal("Engagement", model.Category);
        Assert.Equal(SelectionModeType.ConfigurationKey, model.SelectionMode);
        Assert.Equal(ErrorPolicyType.FallbackToControl, model.ErrorPolicy);
        Assert.Single(model.Variants);
        Assert.Empty(model.FallbackOrder);
    }
}
