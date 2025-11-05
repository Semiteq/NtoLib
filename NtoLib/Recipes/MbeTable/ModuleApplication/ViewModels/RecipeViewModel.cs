using System;
using System.Collections.Generic;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;

public sealed class RecipeViewModel
{
    public IReadOnlyList<StepViewModel> ViewModels => _viewModels;

    private readonly List<StepViewModel> _viewModels = new();
    private readonly IReadOnlyList<ColumnDefinition> _tableColumns;
    private readonly IRecipeService _recipeService;
    private readonly IComboboxDataProvider _comboboxDataProvider;
    private readonly PropertyStateProvider _propertyStateProvider;

    public RecipeViewModel(
        IRecipeService recipeService,
        IComboboxDataProvider comboboxDataProvider,
        PropertyStateProvider propertyStateProvider,
        IReadOnlyList<ColumnDefinition> tableColumns)
    {
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        _comboboxDataProvider = comboboxDataProvider ?? throw new ArgumentNullException(nameof(comboboxDataProvider));
        _propertyStateProvider = propertyStateProvider ?? throw new ArgumentNullException(nameof(propertyStateProvider));
        _tableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));

        RebuildViewModels();
    }

    public int GetRowCount() => _viewModels.Count;
    
    public void OnRecipeStructureChanged() => RebuildViewModels();
    

    public void OnStepDataChanged(int stepIndex)
    {
        if (stepIndex >= 0 && stepIndex < _viewModels.Count) 
            UpdateStepViewModel(stepIndex);
    }

    public void OnTimeRecalculated(int fromStepIndex)
    {
        if (fromStepIndex < 0 || fromStepIndex >= _viewModels.Count)
            return;

        var recipe = _recipeService.GetCurrentRecipe();

        for (var i = fromStepIndex; i < _viewModels.Count; i++)
        {
            var startTimeResult = _recipeService.GetStepStartTime(i);
            var startTime = startTimeResult.IsSuccess ? startTimeResult.Value : TimeSpan.Zero;

            _viewModels[i].UpdateInPlace(recipe.Steps[i], startTime);
        }
    }

    public Result<object?> GetCellValue(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= _viewModels.Count) return InvalidColumnIndexError(rowIndex);
        if (columnIndex < 0 || columnIndex >= _tableColumns.Count) return InvalidColumnIndexError(columnIndex);

        var state = GetCellState(rowIndex, columnIndex);
        if (state is PropertyState.Disabled) return Result.Ok<object?>(null);
        
        var columnKey = _tableColumns[columnIndex].Key;
        return _viewModels[rowIndex].GetPropertyValue(columnKey);
    }
    
    public PropertyState GetCellState(int rowIndex, int columnIndex)
    {
        var step = _recipeService.GetCurrentRecipe().Steps[rowIndex];
        var columnKey = _tableColumns[columnIndex].Key;

        return _propertyStateProvider.GetPropertyState(step, columnKey);
    }

    private void RebuildViewModels()
    {
        _viewModels.Clear();
        var recipe = _recipeService.GetCurrentRecipe();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            var getStartTimeResult = _recipeService.GetStepStartTime(i);
            var startTime = getStartTimeResult.IsSuccess ? getStartTimeResult.Value : TimeSpan.Zero;

            _viewModels.Add(new StepViewModel(recipe.Steps[i], startTime, _comboboxDataProvider));
        }
    }

    private void UpdateStepViewModel(int index)
    {
        var recipe = _recipeService.GetCurrentRecipe();
        var startTimeResult = _recipeService.GetStepStartTime(index);
        var startTime = startTimeResult.IsSuccess ? startTimeResult.Value : TimeSpan.Zero;

        _viewModels[index].UpdateInPlace(recipe.Steps[index], startTime);
    }
    
    private static Result InvalidColumnIndexError(int columnIndex)
    {
        return Result.Fail(new Error("Invalid column index")
            .WithMetadata(nameof(Codes), Codes.CoreIndexOutOfRange)
            .WithMetadata("columnIndex", columnIndex));
    }
}