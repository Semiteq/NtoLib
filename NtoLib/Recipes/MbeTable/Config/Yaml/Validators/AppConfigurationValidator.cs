#nullable enable

using System;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Errors;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Validators;

/// <summary>
/// Performs high-level validation of the entire application configuration,
/// checking for consistency between different configuration parts.
/// </summary>
public sealed class AppConfigurationValidator : IAppConfigurationValidator
{
    /// <inheritdoc />
    public Result Validate(AppConfiguration config)
    {
        var hardwareConsistencyResult = ValidatePinGroupHardwareConsistency(config);
        if (hardwareConsistencyResult.IsFailed)
        {
            return hardwareConsistencyResult;
        }

        // Future cross-configuration checks can be added here.
        // For example, validating that CalculationRule mappings in ActionsDefs
        // point to existing columns.

        return Result.Ok();
    }

    /// <summary>
    /// Checks that any Pin Group referenced by an Action exists in the Pin Group definitions.
    /// This ensures that the logical configuration (actions) matches the hardware configuration (pin groups).
    /// </summary>
    private static Result ValidatePinGroupHardwareConsistency(AppConfiguration appConfig)
    {
        var definedHardwareGroups = appConfig.PinGroupData
            .Select(g => g.GroupName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var requiredTargetGroups = appConfig.Actions.Values
            .SelectMany(a => a.Columns)
            .Where(c => !string.IsNullOrWhiteSpace(c.GroupName))
            .Select(c => c.GroupName!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var requiredGroup in requiredTargetGroups)
        {
            if (!definedHardwareGroups.Contains(requiredGroup))
            {
                var error = new RecipeError(
                    "Configuration Integrity Error: ActionsDefs.yaml requires a GroupName " +
                    $"'{requiredGroup}', but no such group is defined in PinGroupDefs.yaml.",
                    RecipeErrorCodes.ConfigMissingReference);
                return Result.Fail(error);
            }
        }

        return Result.Ok();
    }
}