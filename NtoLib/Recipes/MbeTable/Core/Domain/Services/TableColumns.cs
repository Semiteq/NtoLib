#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Services;

/// <summary>
/// Provides access to the configured schema of the recipe table.
/// </summary>
public sealed class TableColumns
{
    private readonly IReadOnlyList<ColumnDefinition> _columns;
    private readonly Dictionary<ColumnIdentifier, ColumnDefinition> _columnsByKey;

    /// <summary>
    /// Initializes a new instance with column definitions.
    /// </summary>
    public TableColumns(IReadOnlyList<ColumnDefinition> columns)
    {
        _columns = columns ?? throw new ArgumentNullException(nameof(columns));
        _columnsByKey = _columns.ToDictionary(c => c.Key);
    }

    /// <summary>
    /// Gets the complete list of column definitions for the table.
    /// </summary>
    public IReadOnlyList<ColumnDefinition> GetColumns() => _columns;

    /// <summary>
    /// Gets a specific column definition by its zero-based index.
    /// </summary>
    public ColumnDefinition GetColumnDefinition(int index)
    {
        if (index < 0 || index >= _columns.Count)
            throw new IndexOutOfRangeException("Invalid column index.");

        return _columns[index];
    }

    /// <summary>
    /// Gets a specific column definition by its unique identifier.
    /// </summary>
    public ColumnDefinition GetColumnDefinition(ColumnIdentifier key)
    {
        return _columnsByKey.TryGetValue(key, out var definition)
            ? definition
            : throw new KeyNotFoundException($"Column with key '{key.Value}' not found in schema.");
    }

    /// <summary>
    /// Gets all column definitions that have a specific semantic role.
    /// </summary>
    public IEnumerable<ColumnDefinition> GetColumnsByRole(string role)
    {
        return _columns.Where(c =>
            string.Equals(c.Role, role, StringComparison.OrdinalIgnoreCase));
    }
}