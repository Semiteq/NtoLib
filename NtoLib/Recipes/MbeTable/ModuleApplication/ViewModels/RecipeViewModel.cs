using System;
using System.Collections.Generic;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;

public sealed class RecipeViewModel
{
    public event Action<int>? RowCountChanged;
    public event Action<int>? RowInvalidationRequested;

    public IReadOnlyList<StepViewModel> ViewModels => _viewModels;

    private readonly List<StepViewModel> _viewModels = new();
    private readonly IReadOnlyList<ColumnDefinition> _tableColumns;
    private readonly IRecipeService _recipeService;
    private readonly IComboboxDataProvider _comboboxDataProvider;
    private readonly PropertyStateProvider _propertyStateProvider;
    private readonly ILogger _logger;

    public RecipeViewModel(
        IRecipeService recipeService,
        IComboboxDataProvider comboboxDataProvider,
        PropertyStateProvider propertyStateProvider,
        IReadOnlyList<ColumnDefinition> tableColumns,
        ILogger logger)
    {
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
        _comboboxDataProvider = comboboxDataProvider ?? throw new ArgumentNullException(nameof(comboboxDataProvider));
        _propertyStateProvider = propertyStateProvider ?? throw new ArgumentNullException(nameof(propertyStateProvider));
        _tableColumns = tableColumns ?? throw new ArgumentNullException(nameof(tableColumns));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        RebuildViewModels();
    }

    public void OnRecipeStructureChanged()
    {
        RebuildViewModels();
        RowCountChanged?.Invoke(_viewModels.Count);
    }

    public void OnStepDataChanged(int stepIndex)
    {
        if (stepIndex >= 0 && stepIndex < _viewModels.Count)
        {
            UpdateStepViewModel(stepIndex);
            RowInvalidationRequested?.Invoke(stepIndex);
        }
    }

    public PropertyState GetCellState(int rowIndex, int columnIndex)
    {
        var recipe = _recipeService.GetCurrentRecipe();
        var step = recipe.Steps[rowIndex];
        var columnKey = _tableColumns[columnIndex].Key;

        return _propertyStateProvider.GetPropertyState(step, columnKey);
    }

    public Result<object?> GetCellValue(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= _viewModels.Count)
        {
            return Result.Fail(new Error("Invalid row index in GetCellValue")
                .WithMetadata("code", Codes.InvalidRowIndex)
                .WithMetadata("rowIndex", rowIndex)
                .WithMetadata("viewModelsCount", _viewModels.Count));
        }

        if (columnIndex < 0 || columnIndex >= _tableColumns.Count)
        {
            return Result.Fail(new Error("Invalid column index in GetCellValue")
                .WithMetadata("code", Codes.InvalidColumnIndex)
                .WithMetadata("columnIndex", columnIndex)
                .WithMetadata("columnsCount", _tableColumns.Count));
        }

        var vm = _viewModels[rowIndex];
        var columnKey = _tableColumns[columnIndex].Key;
        var columnDefinition = _tableColumns[columnIndex];

        if (columnKey == MandatoryColumns.StepStartTime)
            return Result.Ok<object?>(vm.StepStartTime);

        var propertyValueResult = vm.GetPropertyValue(columnKey);
        
        if (propertyValueResult.IsFailed)
        {
            var state = GetCellState(rowIndex, columnIndex);
            if (state == PropertyState.Disabled)
                return Result.Ok<object?>(null);

            if (columnDefinition.ReadOnly)
                return Result.Ok<object?>(null);
            

            _logger.LogDebug($"GetCellValue failed for row {rowIndex}, column '{columnKey.Value}': property not found but cell is not disabled");
            return propertyValueResult;
        }

        return propertyValueResult;
    }

    public int GetRowCount() => _viewModels.Count;

    private void RebuildViewModels()
    {
        _viewModels.Clear();
        var recipe = _recipeService.GetCurrentRecipe();

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            var getStartTimeResult = _recipeService.GetStepStartTime(i);
            var startTime = getStartTimeResult.IsSuccess ? getStartTimeResult.Value : TimeSpan.Zero;

            _viewModels.Add(new StepViewModel(recipe.Steps[i], i, startTime, _comboboxDataProvider));
        }
    }

    private void UpdateStepViewModel(int index)
    {
        var recipe = _recipeService.GetCurrentRecipe();
        var startTimeResult = _recipeService.GetStepStartTime(index);
        var startTime = startTimeResult.IsSuccess ? startTimeResult.Value : TimeSpan.Zero;

        _viewModels[index].UpdateInPlace(recipe.Steps[index], index, startTime);
    }
}