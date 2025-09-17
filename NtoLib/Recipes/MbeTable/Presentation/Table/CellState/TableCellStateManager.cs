using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Infrastructure.PinDataManager;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.CellState;

/// <summary>
/// Provides a visual state for a cell based on ViewModel rules.
/// </summary>
public class TableCellStateManager
{
    private IReadOnlyDictionary<TableCellState, CellStatusDescription> _states;
    private readonly IPlcRecipeStatusProvider _plcRecipeStatusProvider;

    public TableCellStateManager(IPlcRecipeStatusProvider plcRecipeStatusProvider)
    {
        _plcRecipeStatusProvider = plcRecipeStatusProvider ??
                                   throw new System.ArgumentNullException(nameof(plcRecipeStatusProvider));
    }

    public void UpdateColorScheme(ColorScheme colorScheme)
    {
        _states = UpdateStates(colorScheme);
    }

    private Dictionary<TableCellState, CellStatusDescription> UpdateStates(ColorScheme colorScheme)
    {
        return new Dictionary<TableCellState, CellStatusDescription>()
        {
            {
                TableCellState.Default, new CellStatusDescription(
                    IsReadonly: false,
                    Font: colorScheme.LineFont,
                    ForeColor: colorScheme.LineTextColor,
                    BackColor: colorScheme.LineBgColor)
            },
            {
                TableCellState.ReadOnly, new CellStatusDescription(
                    IsReadonly: true,
                    Font: colorScheme.LineFont,
                    ForeColor: colorScheme.LineTextColor,
                    BackColor: colorScheme.LineBgColor)
            },
            {
                TableCellState.Disabled, new CellStatusDescription(
                    IsReadonly: true,
                    Font: colorScheme.BlockedFont,
                    ForeColor: colorScheme.BlockedTextColor,
                    BackColor: colorScheme.BlockedBgColor)
            },
            {
                TableCellState.Passed, new CellStatusDescription(
                    IsReadonly: true,
                    Font: colorScheme.PassedLineFont,
                    ForeColor: colorScheme.PassedLineTextColor,
                    BackColor: colorScheme.PassedLineBgColor)
            },
            {
                TableCellState.Current, new CellStatusDescription(
                    IsReadonly: true,
                    Font: colorScheme.SelectedLineFont,
                    ForeColor: colorScheme.SelectedLineTextColor,
                    BackColor: colorScheme.SelectedLineBgColor)
            },
            {
                TableCellState.Upcoming, new CellStatusDescription(
                    IsReadonly: true,
                    Font: colorScheme.LineFont,
                    ForeColor: colorScheme.LineTextColor,
                    BackColor: colorScheme.LineBgColor)
            }
        };
    }

    public CellStatusDescription GetStateForCell(StepViewModel stepViewModel, ColumnIdentifier columnKey, int rowIndex)
    {
        var status = _plcRecipeStatusProvider.GetStatus();
        var attribute = GetCellState(stepViewModel, columnKey, rowIndex, status);

        if (status.IsRecipeActive)
            return _states[attribute] with { IsReadonly = true };

        return _states[attribute];
    }

    private TableCellState GetCellState(StepViewModel stepViewModel, ColumnIdentifier columnKey, int rowIndex,
        PlcRecipeStatus status)
    {
        if (rowIndex < status.CurrentLine)
            return TableCellState.Passed;

        if (rowIndex == status.CurrentLine)
            return TableCellState.Current;

        if (stepViewModel.IsPropertyDisabled(columnKey))
            return TableCellState.Disabled;

        if (stepViewModel.IsPropertyReadonly(columnKey))
            return TableCellState.ReadOnly;

        return TableCellState.Default;
    }
}