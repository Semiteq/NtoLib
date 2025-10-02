using System;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.State;

/// <summary>
/// Composes row execution state and cell data state to produce final visual state.
/// Priority: RowExecutionState (Current/Passed) overrides CellDataState styling.
/// </summary>
public sealed class CellStateResolver : ICellStateResolver
{
    private readonly IRowExecutionStateProvider _rowExecutionStateProvider;
    private readonly IPlcRecipeStatusProvider _plcRecipeStatusProvider;
    private readonly IColorSchemeProvider _colorSchemeProvider;

    public CellStateResolver(
        IRowExecutionStateProvider rowExecutionStateProvider,
        IPlcRecipeStatusProvider plcRecipeStatusProvider,
        IColorSchemeProvider colorSchemeProvider)
    {
        _rowExecutionStateProvider = rowExecutionStateProvider ?? throw new ArgumentNullException(nameof(rowExecutionStateProvider));
        _plcRecipeStatusProvider = plcRecipeStatusProvider ?? throw new ArgumentNullException(nameof(plcRecipeStatusProvider));
        _colorSchemeProvider = colorSchemeProvider ?? throw new ArgumentNullException(nameof(colorSchemeProvider));
    }

    public CellVisualState Resolve(int rowIndex, StepViewModel viewModel, ColumnIdentifier columnKey)
    {
        var rowState = _rowExecutionStateProvider.GetState(rowIndex);
        var dataState = viewModel.GetDataState(columnKey);
        var isRecipeActive = _plcRecipeStatusProvider.GetStatus().IsRecipeActive;

        var visualState = _colorSchemeProvider.Current.GetStyleForState(rowState, dataState);

        if (isRecipeActive)
        {
            return visualState with { IsReadOnly = true };
        }

        return visualState;
    }
}