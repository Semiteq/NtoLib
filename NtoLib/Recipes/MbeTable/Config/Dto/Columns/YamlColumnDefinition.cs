

namespace NtoLib.Recipes.MbeTable.Config.Dto.Columns;

public sealed class YamlColumnDefinition
{
    public string Key { get; set; } = string.Empty;
    public YamlColumnBusinessLogic BusinessLogic { get; set; }
    public YamlColumnUi Ui { get; set; }
}