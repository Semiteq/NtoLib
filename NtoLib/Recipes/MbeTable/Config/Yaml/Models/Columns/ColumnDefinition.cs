#nullable enable

using System.Windows.Forms;
using NtoLib.Recipes.MbeTable.Core.Domain.Calculations;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;

/// <summary>
/// Domain column definition with calculation support.
/// </summary>
public sealed record ColumnDefinition(
    ColumnIdentifier Key,
    string Code,
    string UiName,
    string PropertyTypeId,
    string ColumnType,
    int Width,
    DataGridViewContentAlignment Alignment,
    PlcMapping? PlcMapping,
    bool ReadOnly,
    CalculationDefinition? Calculation
);