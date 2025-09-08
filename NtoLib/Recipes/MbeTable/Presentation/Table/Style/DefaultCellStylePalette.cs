#nullable enable

using System.Collections.Generic;
using System.Drawing;
using NtoLib.Recipes.MbeTable.Presentation.Table.CellState;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Style;

/// <summary>
/// Default palette implementation mapping TableCellState to CellStatusDescription
/// based on the provided ColorScheme.
/// </summary>
public sealed class DefaultCellStylePalette : ICellStylePalette
{
    private IReadOnlyDictionary<TableCellState, CellStatusDescription> _map =
        new Dictionary<TableCellState, CellStatusDescription>();

    public void UpdateColorScheme(ColorScheme scheme)
    {
        var dict = new Dictionary<TableCellState, CellStatusDescription>
        {
            {
                TableCellState.Default, new CellStatusDescription(
                    IsReadonly: false,
                    Font: scheme.LineFont,
                    ForeColor: scheme.LineTextColor,
                    BackColor: scheme.LineBgColor)
            },
            {
                TableCellState.ReadOnly, new CellStatusDescription(
                    IsReadonly: true,
                    Font: scheme.LineFont,
                    ForeColor: scheme.LineTextColor,
                    BackColor: scheme.LineBgColor)
            },
            {
                TableCellState.Disabled, new CellStatusDescription(
                    IsReadonly: true,
                    Font: scheme.BlockedFont,
                    ForeColor: scheme.BlockedTextColor,
                    BackColor: scheme.BlockedBgColor)
            },
            {
                TableCellState.Passed, new CellStatusDescription(
                    IsReadonly: true,
                    Font: scheme.PassedLineFont,
                    ForeColor: scheme.PassedLineTextColor,
                    BackColor: scheme.PassedLineBgColor)
            },
            {
                TableCellState.Current, new CellStatusDescription(
                    IsReadonly: true,
                    Font: scheme.SelectedLineFont,
                    ForeColor: scheme.SelectedLineTextColor,
                    BackColor: scheme.SelectedLineBgColor)
            },
            {
                TableCellState.Upcoming, new CellStatusDescription(
                    IsReadonly: true,
                    Font: scheme.LineFont,
                    ForeColor: scheme.LineTextColor,
                    BackColor: scheme.LineBgColor)
            }
        };

        _map = dict;
    }

    public CellStatusDescription Resolve(TableCellState state)
    {
        if (_map.Count == 0)
            return new CellStatusDescription(false, SystemFonts.DefaultFont, SystemColors.WindowText, SystemColors.Window);

        return _map.TryGetValue(state, out var s) ? s : _map[TableCellState.Default];
    }
}