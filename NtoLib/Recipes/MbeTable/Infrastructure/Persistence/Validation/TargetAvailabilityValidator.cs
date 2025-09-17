#nullable enable

using System;
using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Validation;

/// <summary>
/// Validates that columns bound to pin groups reference existing targets in the current environment.
/// </summary>
public sealed class TargetAvailabilityValidator
{
    public Result Validate(
        Recipe recipe,
        IActionRepository actionRepository,
        IActionTargetProvider targetProvider)
    {
        var errors = new List<string>();
        var snapshot = targetProvider.GetAllTargetsSnapshot();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            var step = recipe.Steps[i];
            var actionId = step.Properties[WellKnownColumns.Action]?.GetValue<int>() ?? 0;
            if (actionId == 0) continue;

            var action = actionRepository.GetActionById(actionId);

            // For each column that has GroupName (i.e., enum sourced from pin groups)
            foreach (var col in action.Columns)
            {
                if (string.Equals(col.PropertyTypeId, "Enum", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrWhiteSpace(col.GroupName)) continue;

                var keyId = new ColumnIdentifier(col.Key);
                if (!step.Properties.TryGetValue(keyId, out var prop) || prop is null)
                {
                    errors.Add($"row {i + 1}: actionId={actionId} ('{action.Name}') column '{col.Key}' requires a target from group '{col.GroupName}', but value is empty.");
                    continue;
                }

                var targetId = prop.GetValue<int>();
                if (!snapshot.TryGetValue(col.GroupName!, out var dict))
                {
                    errors.Add($"row {i + 1}: actionId={actionId} ('{action.Name}') column '{col.Key}' references group '{col.GroupName}', which is not available.");
                    continue;
                }

                if (dict.Count == 0)
                {
                    errors.Add($"row {i + 1}: actionId={actionId} ('{action.Name}') column '{col.Key}' group '{col.GroupName}' has no targets configured.");
                    continue;
                }

                if (!dict.ContainsKey(targetId))
                {
                    errors.Add($"row {i + 1}: actionId={actionId} ('{action.Name}') column '{col.Key}' targetId={targetId} not found in group '{col.GroupName}'.");
                }
            }
        }

        return errors.Count == 0
            ? Result.Ok()
            : Result.Fail("Missing or invalid targets in current environment: " + string.Join("; ", errors));
    }
}