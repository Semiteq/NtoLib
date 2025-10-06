using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Core.Properties;
using NtoLib.Recipes.MbeTable.Infrastructure.ActionTartget;
using NtoLib.Recipes.MbeTable.Journaling.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Services;

/// <summary>
/// Handles all Recipe mutations: creating, updating, and removing Steps.
/// </summary>
public sealed class RecipeMutator
{
    private readonly IActionRepository _actionRepository;
    private readonly IActionTargetProvider _actionTargetProvider;
    private readonly PropertyDefinitionRegistry _propertyRegistry;
    private readonly IReadOnlyList<ColumnDefinition> _tableColumns;
    private readonly ILogger _logger;

    public RecipeMutator(
        IActionRepository actionRepository,
        IActionTargetProvider actionTargetProvider,
        PropertyDefinitionRegistry propertyRegistry,
        IReadOnlyList<ColumnDefinition> tableColumns,
        ILogger logger)
    {
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _actionTargetProvider = actionTargetProvider ?? throw new ArgumentNullException(nameof(actionTargetProvider));
        _propertyRegistry = propertyRegistry ?? throw new ArgumentNullException(nameof(propertyRegistry));
        _tableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Result<Recipe> AddDefaultStep(Recipe recipe, int rowIndex)
    {
        var actionResult = _actionRepository.GetResultDefaultActionId();
        if (actionResult.IsFailed)
            return actionResult.ToResult();

        var stepResult = CreateDefaultStep(actionResult.Value);
        if (stepResult.IsFailed)
            return stepResult.ToResult();

        var clampedIndex = Math.Max(0, Math.Min(rowIndex, recipe.Steps.Count));
        return Result.Ok(new Recipe(recipe.Steps.Insert(clampedIndex, stepResult.Value)));
    }

    public Result<Recipe> RemoveStep(Recipe recipe, int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= recipe.Steps.Count)
        {
            return Result.Fail(new Error("Row index is out of range")
                .WithMetadata("code", ErrorCode.CoreIndexOutOfRange)
                .WithMetadata("rowIndex", rowIndex)
                .WithMetadata("stepCount", recipe.Steps.Count));
        }

        return Result.Ok(new Recipe(recipe.Steps.RemoveAt(rowIndex)));
    }

    public Result<Recipe> UpdateStepProperty(
        Recipe recipe,
        int rowIndex,
        ColumnIdentifier key,
        object value)
    {
        if (rowIndex < 0 || rowIndex >= recipe.Steps.Count)
        {
            return Result.Fail(new Error("Row index is out of range")
                .WithMetadata("code", ErrorCode.CoreIndexOutOfRange)
                .WithMetadata("rowIndex", rowIndex)
                .WithMetadata("stepCount", recipe.Steps.Count));
        }

        var step = recipe.Steps[rowIndex];

        if (!step.Properties.TryGetValue(key, out var property) || property == null)
        {
            return Result.Fail(new Error($"Property '{key.Value}' not found in step")
                .WithMetadata("code", ErrorCode.CorePropertyNotFound)
                .WithMetadata("rowIndex", rowIndex)
                .WithMetadata("propertyKey", key.Value));
        }

        var newPropertyResult = property.WithValue(value);
        if (newPropertyResult.IsFailed)
            return newPropertyResult.ToResult();

        var updatedProperties = step.Properties.SetItem(key, newPropertyResult.Value);
        var updatedStep = step with { Properties = updatedProperties };
        return Result.Ok(new Recipe(recipe.Steps.SetItem(rowIndex, updatedStep)));
    }

    public Result<Recipe> ReplaceStepAction(Recipe recipe, int rowIndex, short newActionId)
    {
        if (rowIndex < 0 || rowIndex >= recipe.Steps.Count)
            return IndexOutOfRange(rowIndex, recipe.Steps.Count);

        var stepResult = CreateDefaultStep(newActionId);
        if (stepResult.IsFailed)
            return stepResult.ToResult();

        return Result.Ok(new Recipe(recipe.Steps.SetItem(rowIndex, stepResult.Value)));
    }


    private Result<Step> CreateDefaultStep(short actionId)
    {
        var actionResult = _actionRepository.GetResultActionDefinitionById(actionId);
        if (actionResult.IsFailed)
            return actionResult.ToResult();

        var builder = new StepBuilder(actionResult.Value, _propertyRegistry, _tableColumns);

        foreach (var col in actionResult.Value.Columns.Where(c =>
            c.PropertyTypeId.Equals("Enum", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(c.GroupName)))
        {
            var key = new ColumnIdentifier(col.Key);
            if (!builder.Supports(key)) continue;

            try
            {
                var targetId = _actionTargetProvider.GetMinimalTargetId(col.GroupName!);
                var setResult = builder.WithOptionalDynamic(key, targetId);
                if (setResult.IsFailed)
                {
                    _logger.LogCritical(
                        new InvalidOperationException(setResult.Errors.First().Message),
                        "Failed to set default target for column '{ColumnKey}'",
                        col.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to get default target for column '{ColumnKey}'", col.Key);
            }
        }

        return Result.Ok(builder.Build());
    }
    
    private static Result<Recipe> IndexOutOfRange(int rowIndex, int count) =>
        Result.Fail<Recipe>(new Error("Row index is out of range")
            .WithMetadata("code", ErrorCode.CoreIndexOutOfRange)
            .WithMetadata("rowIndex", rowIndex)
            .WithMetadata("stepCount", count));
}