#nullable enable
using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Validation;

/// <summary>
/// Validates that targets referenced by steps exist in the current environment,
/// according to the configuration-driven TargetGroup for each action.
/// Used during recipe file read (CSV deserialization).
/// </summary>
public sealed class TargetAvailabilityValidator
{
    /// <summary>
    /// Checks that every step which requires a target (based on Action.TargetGroup) references
    /// an existing target in the current hardware snapshot provided by <see cref="IActionTargetProvider"/>.
    /// </summary>
    /// <param name="recipe">Recipe to validate.</param>
    /// <param name="actionRepository">Repository to resolve action definitions.</param>
    /// <param name="targetProvider">Provider of current hardware targets grouped by group name.</param>
    /// <returns>Ok if all targets are available; Fail with details otherwise.</returns>
    public Result Validate(
        Recipe recipe,
        IActionRepository actionRepository,
        IActionTargetProvider targetProvider)
    {
        var errors = new List<string>();

        // Take a single snapshot to avoid per-row provider calls and ensure consistency during validation
        var snapshot = targetProvider.GetAllTargetsSnapshot();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            var step = recipe.Steps[i];

            // Action is mandatory for every step by schema, fall back to 0 if something is wrong
            var actionId = step.Properties[WellKnownColumns.Action]?.GetValue<int>() ?? 0;
            var action = actionRepository.GetActionById(actionId);
            var groupName = action.TargetGroup;

            // Actions without TargetGroup do not require a target
            if (string.IsNullOrWhiteSpace(groupName))
                continue;

            // If action requires a target, the property must be present (non-null)
            if (!step.Properties.TryGetValue(WellKnownColumns.ActionTarget, out var targetProp) || targetProp is null)
            {
                errors.Add($"row {i + 1}: actionId={actionId} ('{action.Name}') requires target from group '{groupName}', but 'action-target' is empty.");
                continue;
            }

            // Note: 0 is a valid target id (zero-based indexing). Do not skip 0.
            var targetId = targetProp.GetValue<int>();

            // Group must exist in the current environment (already checked at startup, but validate defensively)
            if (!snapshot.TryGetValue(groupName!, out var targetsInGroup))
            {
                errors.Add($"row {i + 1}: actionId={actionId} ('{action.Name}') references group '{groupName}', which is not available in current environment.");
                continue;
            }

            // Group must have at least one target
            if (targetsInGroup.Count == 0)
            {
                errors.Add($"row {i + 1}: actionId={actionId} ('{action.Name}') references group '{groupName}', but this group has no targets configured.");
                continue;
            }

            // Target id must exist within the group's ids (0..N-1)
            if (!targetsInGroup.ContainsKey(targetId))
            {
                errors.Add($"row {i + 1}: actionId={actionId} ('{action.Name}') references targetId={targetId} not found in group '{groupName}'.");
            }
        }

        return errors.Count == 0
            ? Result.Ok()
            : Result.Fail("Missing or invalid targets in current environment: " + string.Join("; ", errors));
    }
}