#nullable enable
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;

public sealed class YamlColumnUi
{
    public string Code { get; set; } = string.Empty;
    public string UiName { get; set; } = string.Empty;
    public string ColumnType { get; set; } = string.Empty;
    public int MaxDropdownItems { get; set; } = 30;
    public int Width { get; set; } = 130;
    public int MinWidth { get; set; } = 50;
    
    public DataGridViewContentAlignment Alignment { get; set; } = DataGridViewContentAlignment.MiddleLeft;
}