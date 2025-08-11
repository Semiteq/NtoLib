using System.Collections.Generic;
using System.Drawing;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Presentation.Table
{
    public class TableCellStateManager
    {
        private readonly IReadOnlyDictionary<CellVisualState, CellState> _states;

        public TableCellStateManager(ColorScheme colorScheme)
        {
            // Для ReadOnly/Disabled используем Blocked* из ColorScheme (унификация)
            var blockedFont = colorScheme.BlockedFont ?? colorScheme.LineFont;
            var blockedText = colorScheme.BlockedTextColor.IsEmpty ? Color.DarkGray : colorScheme.BlockedTextColor;
            var blockedBg = colorScheme.BlockedBgColor.IsEmpty ? Color.LightGray : colorScheme.BlockedBgColor;

            _states = new Dictionary<CellVisualState, CellState>
            {
                {
                    CellVisualState.Default, new CellState(
                        IsReadonly: false,
                        Font: colorScheme.LineFont,
                        ForeColor: colorScheme.LineTextColor,
                        BackColor: colorScheme.LineBgColor)
                },
                {
                    CellVisualState.ReadOnly, new CellState(
                        IsReadonly: true,
                        Font: blockedFont,
                        ForeColor: blockedText,
                        BackColor: blockedBg)
                },
                {
                    CellVisualState.Disabled, new CellState(
                        IsReadonly: true,
                        Font: blockedFont,
                        ForeColor: blockedText,
                        BackColor: blockedBg)
                }
            };
        }

        public CellState GetStateForCell(StepViewModel viewModel, ColumnKey columnKey)
        {
            // Приоритет: ReadOnly (например StepStartTime), затем Disabled, иначе Default
            if (viewModel.IsPropertyReadonly(columnKey))
                return _states[CellVisualState.ReadOnly];

            if (viewModel.IsPropertyDisabled(columnKey))
                return _states[CellVisualState.Disabled];

            return _states[CellVisualState.Default];
        }
    }
}