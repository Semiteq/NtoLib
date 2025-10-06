

using System;
using NtoLib.Recipes.MbeTable.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Entities;
using NtoLib.Recipes.MbeTable.Presentation.Models;
using NtoLib.Recipes.MbeTable.Presentation.StateProviders;
using NtoLib.Recipes.MbeTable.Presentation.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.State;

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