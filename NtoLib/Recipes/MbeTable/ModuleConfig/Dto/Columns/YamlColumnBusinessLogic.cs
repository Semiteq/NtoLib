

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;

public sealed class YamlColumnBusinessLogic
{
    public string PropertyTypeId { get; set; } = string.Empty;
    public bool ReadOnly { get; set; }
    public PlcMapping? PlcMapping { get; set; }
    public YamlColumnCalculation? Calculation { get; set; }
}