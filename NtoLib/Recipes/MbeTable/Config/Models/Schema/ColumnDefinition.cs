#nullable enable

using System;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Config.Models.Schema;

public record ColumnDefinition(
    ColumnIdentifier Key,
    int Index,
    string Code,
    string UiName,
    string Role,
    Type SystemType,
    string ColumnType,
    int Width,
    bool ReadOnly,
    DataGridViewContentAlignment Alignment,
    PlcMapping? PlcMapping
);