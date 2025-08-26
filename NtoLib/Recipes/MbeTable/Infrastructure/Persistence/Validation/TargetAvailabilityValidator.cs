#nullable enable

using System.Collections.Generic;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Validation;

public class TargetAvailabilityValidator
{
    /// <summary>
    /// Validates that all target IDs in the recipe steps exist in the current environment.
    /// </summary>
    /// <param name="recipe">The recipe to validate.</param>
    /// <param name="actionManager">The action manager to resolve action types.</param>
    /// <param name="targetProvider">The provider for available targets.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure.</returns>
    public Result Validate(
        Recipe recipe,
        ActionManager actionManager,
        IActionTargetProvider targetProvider)
    {
        var missing = new List<string>();

        foreach (var (index, step) in recipe.Steps.Select((s, i) => (i, s)))
        {
            var actionId = step.Properties[WellKnownColumns.Action]?.GetValue<int>() ?? 0;
            var targetId = step.Properties[WellKnownColumns.ActionTarget]?.GetValue<int>() ?? 0;
            if (targetId == 0) continue;

            var actionType = actionManager.GetActionTypeById(actionId);
            var ok = actionType switch
            {
                ActionType.Heater => targetProvider.GetHeaterNames().ContainsKey(targetId),
                ActionType.Shutter => targetProvider.GetShutterNames().ContainsKey(targetId),
                ActionType.NitrogenSource => targetProvider.GetNitrogenSourceNames().ContainsKey(targetId),
                _ => true
            };

            if (!ok)
            {
                missing.Add($"row {index + 1}: action={actionId}, target={targetId}");
            }
        }

        return missing.Count == 0
            ? Result.Ok()
            : Result.Fail("Missing targets in current environment: " + string.Join("; ", missing));
    }
}