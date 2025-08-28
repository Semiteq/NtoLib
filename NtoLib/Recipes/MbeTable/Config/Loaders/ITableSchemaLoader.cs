#nullable enable

using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;

namespace NtoLib.Recipes.MbeTable.Config.Loaders;

/// <summary>
/// Defines a contract for a service that loads the table schema configuration.
/// </summary>
public interface ITableSchemaLoader
{
    /// <summary>
    /// Loads and parses the table schema from a specified file path.
    /// </summary>
    /// <param name="schemaPath">The full path to the schema configuration file.</param>
    /// <returns>A read-only list of configured column definitions.</returns>
    Result<IReadOnlyList<ColumnDefinition>> LoadSchema(string schemaPath);
}