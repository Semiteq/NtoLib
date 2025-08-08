using System.Collections.Generic;
using System.Drawing;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Presentation.Table;

public class TableCellStateManager
{
    private readonly IReadOnlyDictionary<string, CellState> _states;

    public TableCellStateManager(ColorScheme colorScheme)
    {
        _states = new Dictionary<string, CellState>
        {
            { "Default", new CellState(false, colorScheme.LineFont, colorScheme.LineTextColor, colorScheme.LineBgColor) },
            { "Disabled", new CellState(true, colorScheme.LineFont, Color.DarkGray, Color.LightGray) }
        };
    }
    
    public CellState GetStateForCell(StepViewModel viewModel, ColumnKey columnKey)
    {
        if (!viewModel.IsPropertyAvailable(columnKey))
        {
            return _states["Disabled"];
        }
        
        return _states["Default"];
    }
}