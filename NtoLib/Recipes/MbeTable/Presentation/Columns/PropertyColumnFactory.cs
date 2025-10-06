using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Core.Properties;
using NtoLib.Recipes.MbeTable.Presentation.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Columns;

/// <summary>
/// Numeric / string property field.
/// </summary>
public sealed class PropertyColumnFactory : BaseColumnFactory
{
    private readonly PropertyDefinitionRegistry _registry;

    public PropertyColumnFactory(PropertyDefinitionRegistry registry) => _registry = registry;

    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition definition) =>
        new PropertyGridColumn
        {
            DataPropertyName = definition.Key.Value
        };

    protected override void ConfigureColumn(
        DataGridViewColumn column,
        ColumnDefinition   definition,
        ColorScheme        scheme)
    {
        var def = _registry.GetPropertyDefinition(definition.PropertyTypeId);
        column.ValueType = def.SystemType;
        column.DataPropertyName = definition.Key.Value;
    }
}