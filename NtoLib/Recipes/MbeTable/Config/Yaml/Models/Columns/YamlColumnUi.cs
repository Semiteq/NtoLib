#nullable enable
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;

public sealed class YamlColumnUi
{
    public string Code { get; set; } = string.Empty;
    public string UiName { get; set; } = string.Empty;
    public string ColumnType { get; set; } = string.Empty;
    public int Width { get; set; }
    public DataGridViewContentAlignment Alignment { get; set; } = DataGridViewContentAlignment.MiddleLeft;
}