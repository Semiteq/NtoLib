using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

public class TableColumnFactoryMap
{
    private Dictionary<ColumnKey, IColumnFactory> _map;
    
    public TableColumnFactoryMap(ComboboxDataProvider dataProvider)
    {
        _map = new Dictionary<ColumnKey, IColumnFactory>()
        {
            { ColumnKey.Action, new ActionComboBoxColumnFactory(dataProvider) },
            { ColumnKey.ActionTarget , new ActionTargetComboBoxColumnFactory() },
            { ColumnKey.InitialValue, new TextBoxColumnFactory() },
            { ColumnKey.Setpoint, new TextBoxColumnFactory() },
            { ColumnKey.Speed, new TextBoxColumnFactory() },
            { ColumnKey.StepDuration, new TextBoxColumnFactory() },
            { ColumnKey.StepStartTime, new StepStartTimeColumnFactory() },
            { ColumnKey.Comment, new TextBoxColumnFactory() }
        };
    }

    public IReadOnlyDictionary<ColumnKey, IColumnFactory> GetMap => _map;
}