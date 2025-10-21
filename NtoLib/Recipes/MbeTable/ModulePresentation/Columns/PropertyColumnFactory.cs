using System.Windows.Forms;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties;
using NtoLib.Recipes.MbeTable.ModulePresentation.Style;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Columns;

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