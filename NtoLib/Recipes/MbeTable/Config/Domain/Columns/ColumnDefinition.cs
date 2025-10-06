

using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Config.Domain.Columns;

/// <summary>
/// Domain column definition with calculation support.
/// </summary>
public sealed record ColumnDefinition(
    ColumnIdentifier Key,
    string Code,
    string UiName,
    string PropertyTypeId,
    string ColumnType,
    int MaxDropdownItems,
    int Width,
    int MinimalWidth,
    DataGridViewContentAlignment Alignment,
    PlcMapping? PlcMapping,
    bool ReadOnly);