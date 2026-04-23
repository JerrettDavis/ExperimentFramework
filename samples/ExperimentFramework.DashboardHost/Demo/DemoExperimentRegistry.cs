using ExperimentFramework.Admin;
using ExperimentFramework.DashboardHost.DemoServices;
using AdminExperimentInfo = ExperimentFramework.Admin.ExperimentInfo;
using AdminTrialInfo = ExperimentFramework.Admin.TrialInfo;

namespace ExperimentFramework.DashboardHost.Demo;

/// <summary>
/// Implements <see cref="IExperimentRegistry"/> for the docs-demo mode.
/// The ExperimentFramework core registry is internal, so this adapter exposes
/// the same five experiments to the Dashboard API layer.
/// </summary>
internal sealed class DemoExperimentRegistry : IExperimentRegistry
{
    private readonly IReadOnlyList<AdminExperimentInfo> _experiments =
    [
        new AdminExperimentInfo
        {
            Name        = "checkout-button-v2",
            ServiceType = typeof(ICheckoutButtonService),
            IsActive    = true,
            Metadata    = new Dictionary<string, object>
            {
                ["DisplayName"]   = "Checkout Button — High-Contrast Variant",
                ["Description"]   = "Tests a high-contrast primary CTA on the checkout page to improve conversion.",
                ["Category"]      = "Revenue",
                ["ActiveVariant"] = "variant-a",
                ["LastModified"]  = new DateTime(2026, 3, 15, 10, 0, 0, DateTimeKind.Utc),
            },
            Trials      =
            [
                new AdminTrialInfo { Key = "control",   ImplementationType = typeof(CheckoutButtonControl),  IsControl = true  },
                new AdminTrialInfo { Key = "variant-a", ImplementationType = typeof(CheckoutButtonVariantA), IsControl = false },
            ],
        },
        new AdminExperimentInfo
        {
            Name        = "search-ranker-ml",
            ServiceType = typeof(ISearchRankerService),
            IsActive    = true,
            Metadata    = new Dictionary<string, object>
            {
                ["DisplayName"]   = "Search Ranker — ML Re-ranker",
                ["Description"]   = "Compares a learned ranking model against the deterministic baseline on the search results page.",
                ["Category"]      = "Engagement",
                ["ActiveVariant"] = "ml-v1",
                ["LastModified"]  = new DateTime(2026, 3, 22, 15, 30, 0, DateTimeKind.Utc),
            },
            Trials      =
            [
                new AdminTrialInfo { Key = "baseline", ImplementationType = typeof(SearchBaseline), IsControl = true  },
                new AdminTrialInfo { Key = "ml-v1",    ImplementationType = typeof(SearchMlV1),     IsControl = false },
                new AdminTrialInfo { Key = "ml-v2",    ImplementationType = typeof(SearchMlV2),     IsControl = false },
            ],
        },
        new AdminExperimentInfo
        {
            Name        = "homepage-layout-fall2026",
            ServiceType = typeof(IHomepageLayoutService),
            IsActive    = false,    // PendingApproval in governance data
            Metadata    = new Dictionary<string, object>
            {
                ["DisplayName"]   = "Homepage Layout — Fall 2026 Hero",
                ["Description"]   = "A seasonal hero layout for the fall 2026 marketing campaign. Currently awaiting approval.",
                ["Category"]      = "UX",
                ["ActiveVariant"] = "control",
                ["LastModified"]  = new DateTime(2026, 3, 28, 9, 45, 0, DateTimeKind.Utc),
            },
            Trials      =
            [
                new AdminTrialInfo { Key = "control",   ImplementationType = typeof(HomepageLayoutControl),  IsControl = true  },
                new AdminTrialInfo { Key = "fall-hero",  ImplementationType = typeof(HomepageLayoutFallHero), IsControl = false },
            ],
        },
        new AdminExperimentInfo
        {
            Name        = "pricing-page-copy",
            ServiceType = typeof(IPricingCopyService),
            IsActive    = false,    // Paused in governance data
            Metadata    = new Dictionary<string, object>
            {
                ["DisplayName"]   = "Pricing Page — Benefits-Led Copy",
                ["Description"]   = "Replaces feature-led pricing copy with benefits-led headlines. Currently paused pending legal review.",
                ["Category"]      = "Revenue",
                ["ActiveVariant"] = "control",
                ["LastModified"]  = new DateTime(2026, 3, 12, 17, 0, 0, DateTimeKind.Utc),
            },
            Trials      =
            [
                new AdminTrialInfo { Key = "control",  ImplementationType = typeof(PricingCopyOriginal),    IsControl = true  },
                new AdminTrialInfo { Key = "benefits", ImplementationType = typeof(PricingCopyBenefitsLed), IsControl = false },
            ],
        },
        new AdminExperimentInfo
        {
            Name        = "legacy-api-cutover",
            ServiceType = typeof(ILegacyApiService),
            IsActive    = false,    // Archived in governance data
            Metadata    = new Dictionary<string, object>
            {
                ["DisplayName"]   = "Legacy API Cutover — v1 → v2",
                ["Description"]   = "Gradual cutover from v1 to v2 of the legacy integration API. Now archived after successful rollout.",
                ["Category"]      = "Plugins",
                ["ActiveVariant"] = "v2-api",
                ["LastModified"]  = new DateTime(2026, 2, 18, 11, 15, 0, DateTimeKind.Utc),
            },
            Trials      =
            [
                new AdminTrialInfo { Key = "v1-api", ImplementationType = typeof(LegacyApiV1), IsControl = true  },
                new AdminTrialInfo { Key = "v2-api", ImplementationType = typeof(LegacyApiV2), IsControl = false },
            ],
        },
    ];

    public IEnumerable<AdminExperimentInfo> GetAllExperiments() => _experiments;

    public AdminExperimentInfo? GetExperiment(string name) =>
        _experiments.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase));
}
