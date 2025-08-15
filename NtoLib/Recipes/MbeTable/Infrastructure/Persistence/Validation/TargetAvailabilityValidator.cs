#nullable enable
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Schema;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Persistence.Validation;

public class TargetAvailabilityValidator
{
    // Проверяем, что все targetId из файла существуют в текущем окружении,
    // используя контракт IActionTargetProvider (или аналогичный ваш сервис)
    public (bool Ok, string? Error) Validate(
        Recipe recipe,
        ActionManager actionManager,
        IActionTargetProvider targetProvider)
    {
        var missing = new List<string>();

        foreach (var (index, step) in recipe.Steps.Select((s, i) => (i, s)))
        {
            var actionId = step.Properties[ColumnKey.Action]?.GetValue<int>() ?? 0;
            var targetId = step.Properties[ColumnKey.ActionTarget]?.GetValue<int>() ?? 0;
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
                missing.Add($"row {index+1}: action={actionId}, target={targetId}");
        }

        return missing.Count == 0
            ? (true, null)
            : (false, "Missing targets in current environment: " + string.Join("; ", missing));
    }
}