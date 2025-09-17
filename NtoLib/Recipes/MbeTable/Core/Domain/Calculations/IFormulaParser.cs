#nullable enable
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Calculations;

/// <summary>
/// Parses formulas and converts them for DataTable.Compute.
/// </summary>
public interface IFormulaParser
{
    /// <summary>
    /// Extracts unique (case-insensitive) column identifiers referenced in a formula.
    /// </summary>
    /// <param name="formula">Formula with [column-key] placeholders.</param>
    /// <returns>Read-only ordered list of unique identifiers in order of first appearance.</returns>
    IReadOnlyList<ColumnIdentifier> GetDependencies(string formula);

    /// <summary>
    /// Converts [column-key] placeholders to DataTable column names using provided map.
    /// </summary>
    /// <param name="formula">Original formula.</param>
    /// <param name="columnKeyToDataTableNameMap">Map ColumnIdentifier -> sanitized DataTable column name.</param>
    /// <returns>Converted formula.</returns>
    string ConvertFormulaToDataTableSyntax(
        string formula,
        IReadOnlyDictionary<ColumnIdentifier, string> columnKeyToDataTableNameMap);
}