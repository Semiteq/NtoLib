#nullable enable

using System;
using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Presentation.Context;
using NtoLib.Recipes.MbeTable.Presentation.Table.Style;

namespace NtoLib.Recipes.MbeTable.Presentation.Table.Columns.Factories;

/// <summary>
/// Creates DataGridView columns for property fields (numeric/string values with units).
/// Uses PropertyGridCell for advanced parsing and formatting.
/// </summary>
public sealed class PropertyColumnFactory : BaseColumnFactory
{
    private readonly PropertyDefinitionRegistry _propertyDefinitionRegistry;

    public PropertyColumnFactory(IComboBoxContext comboBoxContext, PropertyDefinitionRegistry propertyDefinitionRegistry)
        : base(comboBoxContext)
    {
        _propertyDefinitionRegistry = propertyDefinitionRegistry ?? throw new ArgumentNullException(nameof(propertyDefinitionRegistry));
    }

    protected override DataGridViewColumn CreateColumnInstance(ColumnDefinition columnDefinition)
    {
        return new PropertyGridColumn();
    }

    protected override void ConfigureColumn(DataGridViewColumn column, ColumnDefinition columnDefinition, ColorScheme colorScheme)
    {
        var propertyTypeId = columnDefinition.PropertyTypeId;
        var propertyDefinition = _propertyDefinitionRegistry.GetDefinition(propertyTypeId);
        column.ValueType = propertyDefinition.SystemType;
    }
}