# CLI Tool

The ExperimentFramework CLI tool (`dotnet experimentframework`) provides commands for validating configurations, diagnosing issues, and exporting registration plans.

## Installation

Install the CLI tool as a global .NET tool:

```bash
dotnet tool install -g ExperimentFramework.Cli
```

Or install it locally in your project:

```bash
dotnet tool install ExperimentFramework.Cli
```

## Commands

### `doctor`

Validates experiment configuration and dependency injection wiring without starting the full application.

**Usage:**

```bash
dotnet experimentframework doctor [options]
```

**Options:**

- `--config <path>` - Path to the configuration file (JSON)
- `--assembly <path>` - Path to the application assembly containing the host configuration

**Examples:**

```bash
# Validate a configuration file
dotnet experimentframework doctor --config appsettings.json

# Validate an assembly
dotnet experimentframework doctor --assembly bin/Debug/net10.0/MyApp.dll

# Validate both configuration and assembly
dotnet experimentframework doctor --config appsettings.json --assembly bin/Debug/net10.0/MyApp.dll
```

**What it checks:**

- Configuration file syntax and schema compliance
- Trial definitions (control/condition compatibility)
- Duplicate condition keys
- Missing required fields (service types, implementation types)
- Selection provider registrations (when assembly is provided)

**Exit codes:**

- `0` - All checks passed
- `1` - Validation errors found

**Example output:**

```
ExperimentFramework Doctor
==========================

Checking configuration file: appsettings.json
✓ Configuration file is valid
✓ Trial definitions are valid

✓ All checks passed!
```

### `config validate`

Validates a configuration file against the ExperimentFramework schema.

**Usage:**

```bash
dotnet experimentframework config validate <path>
```

**Arguments:**

- `<path>` - Path to the configuration file to validate

**Examples:**

```bash
# Validate a configuration file
dotnet experimentframework config validate appsettings.json

# Validate a standalone experiment config
dotnet experimentframework config validate experiments.json
```

**What it checks:**

- JSON syntax
- Required fields (serviceType, selectionMode, control)
- Valid selection mode types
- Valid decorator types
- Duplicate condition keys
- Error policy references
- Activation settings

**Exit codes:**

- `0` - Configuration is valid
- `1` - Validation errors found or file not found

**Example output (valid):**

```
Validating configuration file: appsettings.json

✓ Configuration is valid
```

**Example output (invalid):**

```
Validating configuration file: appsettings.json

✗ Configuration validation failed with 2 error(s):

✗ [ERROR] trials[0].serviceType
  Service type is required

✗ [ERROR] trials[0].control.implementationType
  Implementation type is required
```

### `plan export`

Exports a human-readable or JSON plan of experiment registrations and configuration.

**Usage:**

```bash
dotnet experimentframework plan export --config <path> [options]
```

**Options:**

- `--config <path>` (required) - Path to the configuration file (JSON)
- `--format <json|text>` - Output format (default: text)
- `--out <path>` - Output file path (default: stdout)

**Examples:**

```bash
# Export plan to console (text format)
dotnet experimentframework plan export --config appsettings.json

# Export plan to file (JSON format)
dotnet experimentframework plan export --config appsettings.json --format json --out plan.json

# Export detailed text report
dotnet experimentframework plan export --config experiments.json --format text --out plan.txt
```

**Output includes:**

- Global decorators
- Standalone trials with selection modes
- Control and condition implementations
- Named experiments with their trials

**Exit codes:**

- `0` - Plan exported successfully
- `1` - Configuration file not found or parsing error

**Example output (text format):**

```
Loading configuration from: appsettings.json
✓ Configuration loaded successfully

=== ExperimentFramework Configuration Plan ===

Generated: 2026-01-15 04:07:27 UTC

--- Global Decorators (2) ---
  • logging
  • metrics

--- Standalone Trials (1) ---
  Service: IMyService
    Selection Mode: featureFlag
    Control: control -> MyControlService
    Conditions:
      - variant-a -> MyVariantAService
      - variant-b -> MyVariantBService

=== End of Plan ===
```

**Example output (JSON format):**

```json
{
  "generatedAt": "2026-01-15T04:07:27Z",
  "decorators": [
    {
      "type": "logging",
      "typeName": null
    },
    {
      "type": "metrics",
      "typeName": null
    }
  ],
  "trials": [
    {
      "serviceType": "IMyService",
      "selectionMode": "featureFlag",
      "control": {
        "key": "control",
        "implementation": "MyControlService"
      },
      "conditions": [
        {
          "key": "variant-a",
          "implementation": "MyVariantAService"
        },
        {
          "key": "variant-b",
          "implementation": "MyVariantBService"
        }
      ]
    }
  ],
  "experiments": []
}
```

## Common Use Cases

### CI/CD Pipeline Validation

Add configuration validation to your CI/CD pipeline:

```bash
# In your CI script
dotnet experimentframework config validate appsettings.json
if [ $? -ne 0 ]; then
  echo "Configuration validation failed!"
  exit 1
fi
```

### Pre-deployment Checks

Validate both configuration and assembly before deployment:

```bash
dotnet build
dotnet experimentframework doctor \
  --config appsettings.Production.json \
  --assembly bin/Release/net10.0/MyApp.dll
```

### Documentation Generation

Export configuration plans for documentation:

```bash
# Generate human-readable plan for documentation
dotnet experimentframework plan export \
  --config appsettings.json \
  --format text \
  --out docs/experiment-plan.txt

# Generate JSON for tooling/analysis
dotnet experimentframework plan export \
  --config appsettings.json \
  --format json \
  --out docs/experiment-plan.json
```

### Troubleshooting

Use the doctor command to diagnose configuration issues:

```bash
# Check what's wrong with your configuration
dotnet experimentframework doctor --config appsettings.json

# The output will show specific errors with paths to fix
```

## Tips

1. **Always validate before deployment** - Use `config validate` in your CI/CD pipeline to catch configuration errors early.

2. **Use deterministic output** - The `plan export` command generates deterministic output, making it suitable for diffing and version control.

3. **Check exit codes** - All commands exit with code 0 on success and 1 on failure, making them scriptable.

4. **Combine commands** - Use multiple commands in sequence for comprehensive validation:
   ```bash
   dotnet experimentframework config validate config.json && \
   dotnet experimentframework doctor --config config.json && \
   dotnet experimentframework plan export --config config.json --format json --out plan.json
   ```

## See Also

- [Configuration Guide](configuration.md) - Learn how to configure experiments
- [Getting Started](getting-started.md) - Quick start guide
- [Service Registration Safety](../SERVICE_REGISTRATION_SAFETY.md) - Understanding registration plans
