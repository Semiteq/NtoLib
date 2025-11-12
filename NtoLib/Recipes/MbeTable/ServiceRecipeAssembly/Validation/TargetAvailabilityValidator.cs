using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTarget;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Errors;

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
        var snapshot = targetProvider.GetAllTargetsFilteredSnapshot();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            var stepValidationResult = ValidateStep(
                recipe.Steps[i],
                i,
                actionRepository,
                snapshot,
                errors);

            if (stepValidationResult.IsFailed)
                return stepValidationResult;
        }

        return BuildValidationResult(errors);
    }

    private static Result ValidateStep(
        Step step,
        int stepIndex,
        IActionRepository actionRepository,
        IReadOnlyDictionary<string, IReadOnlyDictionary<short, string>> snapshot,
        List<string> errors)
    {
        var actionIdResult = ExtractActionId(step);
        if (actionIdResult.IsFailed)
            return actionIdResult.ToResult();

        var actionId = actionIdResult.Value;
        if (actionId == 0)
            return Result.Ok();

        var actionResult = actionRepository.GetActionDefinitionById(actionId);
        if (actionResult.IsFailed)
            return Result.Ok();

        var action = actionResult.Value;

        foreach (var column in action.Columns)
        {
            var columnValidationResult = ValidateColumn(
                step,
                stepIndex,
                actionId,
                action.Name,
                column,
                snapshot,
                errors);

            if (columnValidationResult.IsFailed)
                return columnValidationResult;
        }

        return Result.Ok();
    }

    private static Result<short> ExtractActionId(Step step)
    {
        var actionProperty = step.Properties[MandatoryColumns.Action];
        if (actionProperty == null)
            return Result.Ok((short)0);

        return actionProperty.GetValue<short>();
    }

    private static Result ValidateColumn(
        Step step,
        int stepIndex,
        short actionId,
        string actionName,
        PropertyConfig column,
        IReadOnlyDictionary<string, IReadOnlyDictionary<short, string>> snapshot,
        List<string> errors)
    {
        if (ShouldSkipColumn(column))
            return Result.Ok();

        var keyId = new ColumnIdentifier(column.Key);

        if (!TryGetPropertyValue(step, keyId, out var property))
        {
            errors.Add(new ValidationTargetValueEmptyError(
                stepIndex,
                actionId,
                actionName,
                column.Key,
                column.GroupName!).Message);
            return Result.Ok();
        }

        var targetIdResult = property.GetValue<short>();
        if (targetIdResult.IsFailed)
            return targetIdResult.ToResult();

        var targetId = targetIdResult.Value;

        return ValidateTargetExists(
            stepIndex,
            actionId,
            actionName,
            column.Key,
            column.GroupName!,
            targetId,
            snapshot,
            errors);
    }

    private static bool ShouldSkipColumn(PropertyConfig column)
    {
        if (string.Equals(column.PropertyTypeId, "Enum", StringComparison.OrdinalIgnoreCase))
            return true;

        if (string.IsNullOrWhiteSpace(column.GroupName))
            return true;

        return false;
    }

    private static bool TryGetPropertyValue(Step step, ColumnIdentifier keyId, out Property property)
    {
        if (step.Properties.TryGetValue(keyId, out var prop) && prop != null)
        {
            property = prop;
            return true;
        }

        property = null!;
        return false;
    }

    private static Result ValidateTargetExists(
        int stepIndex,
        short actionId,
        string actionName,
        string columnKey,
        string groupName,
        short targetId,
        IReadOnlyDictionary<string, IReadOnlyDictionary<short, string>> snapshot,
        List<string> errors)
    {
        if (!snapshot.TryGetValue(groupName, out var targetDictionary))
        {
            errors.Add(new ValidationTargetGroupNotAvailableError(
                stepIndex,
                actionId,
                actionName,
                columnKey,
                groupName).Message);
            return Result.Ok();
        }

        if (targetDictionary.Count == 0)
        {
            errors.Add(new ValidationTargetGroupEmptyError(
                stepIndex,
                actionId,
                actionName,
                columnKey,
                groupName).Message);
            return Result.Ok();
        }

        if (!targetDictionary.ContainsKey(targetId))
        {
            errors.Add(new ValidationTargetNotFoundError(
                stepIndex,
                actionId,
                actionName,
                columnKey,
                targetId,
                groupName).Message);
        }

        return Result.Ok();
    }

    private static Result BuildValidationResult(List<string> errors)
    {
        return errors.Count == 0
            ? Result.Ok()
            : new AssemblyMissingOrInvalidTarget(errors);
    }
}