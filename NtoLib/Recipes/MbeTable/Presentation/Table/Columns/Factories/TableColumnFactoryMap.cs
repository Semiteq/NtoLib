using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Config;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

public class TableColumnFactoryMap
{
    private readonly Dictionary<ColumnIdentifier, IColumnFactory> _map;
    
    public TableColumnFactoryMap(IComboboxDataProvider dataProvider)
    {
        _map = new Dictionary<ColumnIdentifier, IColumnFactory>()
        {
            { WellKnownColumns.Action, new ActionComboBoxColumnFactory(dataProvider) },
            { WellKnownColumns.ActionTarget , new ActionTargetComboBoxColumnFactory() },
            { WellKnownColumns.InitialValue, new TextBoxColumnFactory() },
            { WellKnownColumns.Setpoint, new TextBoxColumnFactory() },
            { WellKnownColumns.Speed, new TextBoxColumnFactory() },
            { WellKnownColumns.StepDuration, new TextBoxColumnFactory() },
            { WellKnownColumns.StepStartTime, new StepStartTimeColumnFactory() },
            { WellKnownColumns.Comment, new TextBoxColumnFactory() }
        };
    }

    public IReadOnlyDictionary<ColumnIdentifier, IColumnFactory> GetMap => _map;
}