namespace NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;

public sealed class YamlColumnUi
{
    public string Code { get; set; }
    public string UiName { get; set; }
    public string ColumnType { get; set; }
    public int MaxDropdownItems { get; set; } = 30;
    public int Width { get; set; } = 130;
    public int MinWidth { get; set; } = 50;
    public UiAlignment Alignment { get; set; } = UiAlignment.MiddleLeft;
}