using System;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModulePresentation.Models;
using NtoLib.Recipes.MbeTable.ModulePresentation.StateProviders;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.State;

/// <summary>
/// Resolves visual cell state combining execution state and property state from data layer.
/// Execution state (Current/Passed) takes priority - cells become readonly during execution.
/// </summary>
public sealed class CellStateResolver : ICellStateResolver
{
    private readonly IRowExecutionStateProvider _rowExecutionStateProvider;
    private readonly IColorSchemeProvider _colorSchemeProvider;

    public CellStateResolver(
        IRowExecutionStateProvider rowExecutionStateProvider,
        IColorSchemeProvider colorSchemeProvider)
    {
        _rowExecutionStateProvider = rowExecutionStateProvider ?? throw new ArgumentNullException(nameof(rowExecutionStateProvider));
        _colorSchemeProvider = colorSchemeProvider ?? throw new ArgumentNullException(nameof(colorSchemeProvider));
    }

    public CellVisualState Resolve(int rowIndex, int columnIndex, RecipeViewModel viewModel)
    {
        var rowState = _rowExecutionStateProvider.GetState(rowIndex);
        var propertyState = viewModel.GetCellState(rowIndex, columnIndex);
        var dataState = MapPropertyStateToDataState(propertyState);

        return _colorSchemeProvider.Current.GetStyleForState(rowState, dataState);
    }

    private static CellDataState MapPropertyStateToDataState(PropertyState propertyState)
    {
        return propertyState switch
        {
            PropertyState.Disabled => CellDataState.Disabled,
            PropertyState.Readonly => CellDataState.ReadOnly,
            PropertyState.Enabled => CellDataState.Normal,
            _ => CellDataState.Disabled
        };
    }
}