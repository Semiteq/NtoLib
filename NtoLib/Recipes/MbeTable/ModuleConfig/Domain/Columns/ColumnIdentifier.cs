

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

/// <summary>
/// Represents a type-safe identifier for a table column, encapsulating a string key.
/// </summary>
/// <param name="Value">The string representation of the column key.</param>
public sealed record ColumnIdentifier(string Value);