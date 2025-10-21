using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Mapping;

/// <summary>
/// Maps YamlColumnDefinition to ColumnDefinition.
/// </summary>
public sealed class ColumnDefinitionMapper : IEntityMapper<YamlColumnDefinition, ColumnDefinition>
{
    public ColumnDefinition Map(YamlColumnDefinition source)
    {
        return new ColumnDefinition(
            Key: new ColumnIdentifier(source.Key),
            Code: source.Ui.Code,
            UiName: source.Ui.UiName,
            PropertyTypeId: source.BusinessLogic.PropertyTypeId,
            ColumnType: source.Ui.ColumnType,
            MaxDropdownItems: source.Ui.MaxDropdownItems,
            Width: source.Ui.Width,
            MinimalWidth: source.Ui.MinWidth,
            Alignment: source.Ui.Alignment,
            PlcMapping: source.BusinessLogic.PlcMapping,
            ReadOnly: source.BusinessLogic.ReadOnly);
    }

    public IReadOnlyList<ColumnDefinition> MapMany(IEnumerable<YamlColumnDefinition> sources)
    {
        return sources.Select(Map).ToList();
    }
}