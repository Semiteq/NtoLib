#nullable enable

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;

public sealed class YamlColumnBusinessLogic
{
    public string PropertyTypeId { get; set; } = string.Empty;
    public bool ReadOnly { get; set; }
    public PlcMapping? PlcMapping { get; set; }
    public YamlColumnCalculation? Calculation { get; set; }
}