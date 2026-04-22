; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
EF0001 | ExperimentFramework.Configuration | Error | Control type does not implement service type
EF0002 | ExperimentFramework.Configuration | Error | Condition type does not implement service type
EF0003 | ExperimentFramework.Configuration | Warning | Duplicate condition key in trial
EF0004 | ExperimentFramework.Configuration | Warning | Trial declared but not registered
EF0005 | ExperimentFramework.Configuration | Warning | Potential lifetime capture mismatch
