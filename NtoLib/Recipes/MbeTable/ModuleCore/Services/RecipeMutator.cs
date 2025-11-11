using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTartget;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Services;

/// <summary>
/// Handles all Recipe mutations: creating, updating, and removing Steps.
/// </summary>
public sealed class RecipeMutator
{
    private readonly IActionRepository _actionRepository;
    private readonly IActionTargetProvider _actionTargetProvider;
    private readonly PropertyDefinitionRegistry _propertyRegistry;
    private readonly IReadOnlyList<ColumnDefinition> _tableColumns;
    private readonly ILogger<RecipeMutator> _logger;

    public RecipeMutator(
        IActionRepository actionRepository,
        IActionTargetProvider actionTargetProvider,
        PropertyDefinitionRegistry propertyRegistry,
        IReadOnlyList<ColumnDefinition> tableColumns,
        ILogger<RecipeMutator> logger)
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
        return rowIndex < 0 || rowIndex >= recipe.Steps.Count
            ? new CoreIndexOutOfRangeError(rowIndex, recipe.Steps.Count)
            : new Recipe(recipe.Steps.RemoveAt(rowIndex));
    }

    public Result<Recipe> UpdateStepProperty(Recipe recipe, int rowIndex, ColumnIdentifier key, object value)
    {
        if (rowIndex < 0 || rowIndex >= recipe.Steps.Count)
            return new CoreIndexOutOfRangeError(rowIndex, recipe.Steps.Count);

        var step = recipe.Steps[rowIndex];

        if (!step.Properties.TryGetValue(key, out var property) || property == null)
            return new CoreStepPropertyNotFoundError(key.Value, rowIndex);

        var newPropertyResult = property.WithValue(value);
        if (newPropertyResult.IsFailed)
            return newPropertyResult.ToResult().WithError(new CoreStepPropertyUpdateFailedError(rowIndex, key.Value));

        var updatedProperties = step.Properties.SetItem(key, newPropertyResult.Value);
        var updatedStep = step with { Properties = updatedProperties };
        return new Recipe(recipe.Steps.SetItem(rowIndex, updatedStep));
    }

    public Result<Recipe> ReplaceStepAction(Recipe recipe, int rowIndex, short newActionId)
    {
        if (rowIndex < 0 || rowIndex >= recipe.Steps.Count) 
            return new CoreIndexOutOfRangeError(rowIndex, recipe.Steps.Count);

        var stepResult = CreateDefaultStep(newActionId);
        if (stepResult.IsFailed)
        {
            _logger.LogError(
                "Failed to create default step for action ID {ActionId} when replacing step at index {RowIndex}",
                newActionId,
                rowIndex);
            return stepResult.ToResult();
        }

        return new Recipe(recipe.Steps.SetItem(rowIndex, stepResult.Value));
    }


    private Result<Step> CreateDefaultStep(short actionId)
    {
        var actionResult = _actionRepository.GetActionDefinitionById(actionId);
        if (actionResult.IsFailed)
            return actionResult.ToResult();

        var builderResult = StepBuilder.Create(actionResult.Value, _propertyRegistry, _tableColumns);
        if (builderResult.IsFailed) return builderResult.ToResult();
        var builder = builderResult.Value;
        
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
                    _logger.LogError(new InvalidOperationException(setResult.Errors.First().Message), 
                        "Failed to set default target for column '{ColumnKey}'",
                        col.Key);
                    return new CoreStepFailedToSetDefaultTarget(col.Key);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get default target for column '{ColumnKey}'", col.Key);
            }
        }

        return Result.Ok(builder.Build());
    }
}