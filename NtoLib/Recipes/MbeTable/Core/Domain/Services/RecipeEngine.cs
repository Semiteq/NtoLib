#nullable enable
using System;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Analysis;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;
using NtoLib.Recipes.MbeTable.Core.Domain.Steps;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Services;

/// <summary>
/// Main engine for recipe manipulations; YAML + PropertyTypeId aware.
/// </summary>
public sealed class RecipeEngine : IRecipeEngine
{
    private readonly IActionRepository _actionRepository;
    private readonly IStepFactory _stepFactory;
    private readonly IActionTargetProvider _actionTargetProvider;
    private readonly StepPropertyCalculator _stepPropertyCalculator;
    private readonly TableColumns _tableColumns;
    private readonly ILogger _debugLogger;

    public RecipeEngine(
        IActionRepository actionRepository,
        IStepFactory stepFactory,
        IActionTargetProvider actionTargetProvider,
        StepPropertyCalculator stepPropertyCalculator,
        TableColumns tableColumns,
        ILogger debugLogger)
    {
        _actionRepository = actionRepository ?? throw new ArgumentNullException(nameof(actionRepository));
        _stepFactory = stepFactory ?? throw new ArgumentNullException(nameof(stepFactory));
        _actionTargetProvider = actionTargetProvider ?? throw new ArgumentNullException(nameof(actionTargetProvider));
        _stepPropertyCalculator = stepPropertyCalculator ?? throw new ArgumentNullException(nameof(stepPropertyCalculator));
        _tableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
    }

    public Recipe CreateEmptyRecipe() => new(System.Collections.Immutable.ImmutableList<Step>.Empty);

    public Recipe AddDefaultStep(Recipe currentRecipe, int rowIndex)
    {
        _debugLogger.Log("Adding a new default step.");
        var defaultActionId = _actionRepository.GetAllActions().First().Key;
        var newStep = CreateDefaultStepForAction(defaultActionId);
        return new Recipe(Steps: currentRecipe.Steps.Insert(rowIndex, newStep));
    }

    public Recipe RemoveStep(Recipe currentRecipe, int rowIndex)
    {
        _debugLogger.Log($"Removing step at index {rowIndex}.");
        return new Recipe(Steps: currentRecipe.Steps.RemoveAt(rowIndex));
    }

    public Recipe ReplaceStepWithNewDefault(Recipe currentRecipe, int rowIndex, int newActionId)
    {
        if (rowIndex < 0 || rowIndex >= currentRecipe.Steps.Count) return currentRecipe;

        _debugLogger.Log($"Replacing step at index {rowIndex} with new action id {newActionId}.");
        var newDefaultStep = CreateDefaultStepForAction(newActionId);
        var newSteps = currentRecipe.Steps.SetItem(rowIndex, newDefaultStep);
        return new Recipe(Steps: newSteps);
    }

    public Result<Recipe> UpdateStepProperty(Recipe currentRecipe, int rowIndex, ColumnIdentifier columnKey, object value)
    {
        if (rowIndex < 0 || rowIndex >= currentRecipe.Steps.Count)
            return Result.Fail(new ValidationError("Row index is out of range."));

        var stepToUpdate = currentRecipe.Steps[rowIndex];
        var updateResult = ApplyUpdateToStep(stepToUpdate, columnKey, value);

        if (updateResult.IsFailed)
            return updateResult.ToResult<Recipe>();

        var newSteps = currentRecipe.Steps.SetItem(rowIndex, updateResult.Value);
        return Result.Ok(new Recipe(Steps: newSteps));
    }

    private Step CreateDefaultStepForAction(int actionId)
    {
        var actionDefinition = _actionRepository.GetActionById(actionId);
        var builder = _stepFactory.ForAction(actionId);

        foreach (var col in actionDefinition.Columns)
        {
            if (!string.Equals(col.PropertyTypeId, "Enum", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrWhiteSpace(col.GroupName)) continue;

            var key = new ColumnIdentifier(col.Key);
            if (!builder.Supports(key)) continue;

            try
            {
                var defaultTargetId = _actionTargetProvider.GetMinimalTargetId(col.GroupName!);
                builder.WithOptionalDynamic(key, defaultTargetId);
            }
            catch (InvalidOperationException ex)
            {
                _debugLogger.LogException(ex, $"Could not get a default target for column '{col.Key}' in group '{col.GroupName}'.");
            }
        }

        return builder.Build();
    }

    private Result<Step> ApplyUpdateToStep(Step currentStep, ColumnIdentifier columnKey, object value)
    {
        if (!currentStep.Properties.TryGetValue(columnKey, out var propertyToUpdate) || propertyToUpdate == null)
            return Result.Fail(new ValidationError($"Property {columnKey.Value} is not available."));

        var newPropertyResult = propertyToUpdate.WithValue(value);
        if (newPropertyResult.IsFailed)
            return Result.Fail(newPropertyResult.Errors);

        var newProperty = newPropertyResult.Value;
        return _stepPropertyCalculator.CalculateDependencies(currentStep, columnKey, newProperty);
    }
    
}