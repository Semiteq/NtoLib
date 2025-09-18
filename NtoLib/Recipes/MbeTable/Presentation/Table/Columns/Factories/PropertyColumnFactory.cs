#nullable enable

using System; 
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties; 
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

public class PropertyColumnFactory : BaseColumnFactory
{
    private readonly PropertyDefinitionRegistry _registry;

    public PropertyColumnFactory(PropertyDefinitionRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    /// <inheritdoc />
    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition colDef)
    {
        return new PropertyGridColumn();
    }

    /// <inheritdoc />
    protected override void ConfigureColumn(DataGridViewColumn column, ColumnDefinition colDef, ColorScheme colorScheme)
    {
        var propertyTypeId = colDef.PropertyTypeId;
        var propertyDefinition = _registry.GetDefinition(propertyTypeId);
        column.ValueType = propertyDefinition.SystemType;

    }
}