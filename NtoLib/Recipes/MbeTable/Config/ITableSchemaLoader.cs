#nullable enable

using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Config;

/// <summary>
/// Defines a contract for a service that loads the table schema configuration.
/// </summary>
public interface ITableSchemaLoader
{
    /// <summary>
    /// Loads and parses the table schema.
    /// </summary>
    /// <returns>A read-only list of configured column definitions.</returns>
    IReadOnlyList<ColumnDefinition> LoadSchema();
}