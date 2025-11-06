using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTartget;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Validation;

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
            var getValueResult = step.Properties[MandatoryColumns.Action]?.GetValue<short>() ?? 0;
            if (getValueResult.IsFailed) return getValueResult.ToResult();

            var actionId = getValueResult.Value;
            if (actionId == 0) continue;

            var actionResult = actionRepository.GetActionDefinitionById(actionId);
            if (actionResult.IsFailed) continue;

            var action = actionResult.Value;

            foreach (var col in action.Columns)
            {
                if (string.Equals(col.PropertyTypeId, "Enum", StringComparison.OrdinalIgnoreCase)) continue;
                if (string.IsNullOrWhiteSpace(col.GroupName)) continue;

                var keyId = new ColumnIdentifier(col.Key);
                if (!step.Properties.TryGetValue(keyId, out var prop) || prop is null)
                {
                    errors.Add(
                        $"row {i + 1}: actionId={actionId} ('{action.Name}') column '{col.Key}' requires a target from group '{col.GroupName}', but value is empty.");
                    continue;
                }
                
                getValueResult = prop.GetValue<short>();
                if (getValueResult.IsFailed) return getValueResult.ToResult();
                var targetId = getValueResult.Value;
                
                if (!snapshot.TryGetValue(col.GroupName!, out var dict))
                {
                    errors.Add(
                        $"row {i + 1}: actionId={actionId} ('{action.Name}') column '{col.Key}' references group '{col.GroupName}', which is not available.");
                    continue;
                }

                if (dict.Count == 0)
                {
                    errors.Add(
                        $"row {i + 1}: actionId={actionId} ('{action.Name}') column '{col.Key}' group '{col.GroupName}' has no targets configured.");
                    continue;
                }

                if (!dict.ContainsKey(targetId))
                {
                    errors.Add(
                        $"row {i + 1}: actionId={actionId} ('{action.Name}') column '{col.Key}' targetId={targetId} not found in group '{col.GroupName}'.");
                }
            }
        }

        return errors.Count == 0
            ? Result.Ok()
            : Result.Fail(
                new Error("Missing or invalid targets in current environment: " + string.Join("; ", errors))
                    .WithMetadata(nameof(Codes), Codes.CoreTargetNotFound));
    }
}