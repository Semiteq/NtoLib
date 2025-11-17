using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;

public sealed class RecipeViewModel
{
    public IReadOnlyList<StepViewModel> ViewModels => _viewModels;

    private readonly List<StepViewModel> _viewModels = new();
    private readonly IReadOnlyList<ColumnDefinition> _tableColumns;
    private readonly IRecipeFacade _recipeService;
    private readonly IComboboxDataProvider _comboboxDataProvider;
    private readonly PropertyStateProvider _propertyStateProvider;

    public RecipeViewModel(
        IRecipeFacade recipeService,
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

    public void OnRecipeStructureChanged() => RebuildViewModels();

    public void OnTimeRecalculated(int fromStepIndex)
    {
        if (fromStepIndex < 0 || fromStepIndex >= _viewModels.Count)
            return;

        var recipe = _recipeService.CurrentSnapshot.Recipe;
        var startTimes = _recipeService.CurrentSnapshot.StepStartTimes;

        for (var i = fromStepIndex; i < _viewModels.Count; i++)
        {
            var startTime = startTimes.TryGetValue(i, out var t) ? t : TimeSpan.Zero;
            _viewModels[i].UpdateInPlace(recipe.Steps[i], startTime);
        }
    }

    public Result<object?> GetCellValue(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= _viewModels.Count)
            return new ApplicationInvalidRowIndexError(rowIndex);

        if (columnIndex < 0 || columnIndex >= _tableColumns.Count)
            return new ApplicationInvalidColumnIndexError(columnIndex);

        var state = GetCellState(rowIndex, columnIndex);
        if (state is PropertyState.Disabled) return Result.Ok<object?>(null);

        var columnKey = _tableColumns[columnIndex].Key;
        return _viewModels[rowIndex].GetPropertyValue(columnKey);
    }

    public PropertyState GetCellState(int rowIndex, int columnIndex)
    {
        if (rowIndex < 0 || rowIndex >= _viewModels.Count)
            return PropertyState.Disabled;
        if (columnIndex < 0 || columnIndex >= _tableColumns.Count)
            return PropertyState.Disabled;

        var step = _recipeService.CurrentSnapshot.Recipe.Steps[rowIndex];
        var columnKey = _tableColumns[columnIndex].Key;

        return _propertyStateProvider.GetPropertyState(step, columnKey);
    }

    private void RebuildViewModels()
    {
        _viewModels.Clear();

        var recipe = _recipeService.CurrentSnapshot.Recipe;
        var startTimes = _recipeService.CurrentSnapshot.StepStartTimes;

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            var startTime = startTimes.TryGetValue(i, out var t) ? t : TimeSpan.Zero;
            _viewModels.Add(new StepViewModel(recipe.Steps[i], startTime, _comboboxDataProvider));
        }
    }
}