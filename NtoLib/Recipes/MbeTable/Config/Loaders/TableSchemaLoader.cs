#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;

namespace NtoLib.Recipes.MbeTable.Config.Loaders;

/// <summary>
/// Loads the table schema definition from a JSON configuration file.
/// </summary>
public class TableSchemaLoader : ITableSchemaLoader
{
    /// <summary>
    /// Loads, parses, and validates the table schema from the JSON file.
    /// </summary>
    /// <param name="schemaPath">The full path to the schema configuration file.</param>
    /// <returns>A Result object containing the read-only list of column definitions if successful, or an error otherwise.</returns>
    public Result<IReadOnlyList<ColumnDefinition>> LoadSchema(string schemaPath)
    {
        try
        {
            var jsonText = File.ReadAllText(schemaPath);
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var jsonDefinitions = JsonSerializer.Deserialize<List<JsonColumnDefinition>>(jsonText, jsonOptions);

            if (jsonDefinitions == null || jsonDefinitions.Count == 0)
            {
                return Result.Fail("Failed to deserialize table schema. The JSON file might be empty or invalid.");
            }

            var columnDefinitions = new List<ColumnDefinition>();

            foreach (var jsonDef in jsonDefinitions)
            {
                var systemType = Type.GetType(jsonDef.SystemType, throwOnError: false, ignoreCase: true);
                if (systemType == null)
                {
                    return Result.Fail($"Invalid system type specified in schema: '{jsonDef.SystemType}'");
                }

                if (!Enum.TryParse(jsonDef.Alignment, ignoreCase: true, out DataGridViewContentAlignment alignment))
                {
                    return Result.Fail($"Invalid alignment specified in schema: '{jsonDef.Alignment}'");
                }

                var columnDefinition = new ColumnDefinition(
                    Key: new ColumnIdentifier(jsonDef.Key),
                    Index: jsonDef.Index,
                    Code: jsonDef.Code,
                    UiName: jsonDef.UiName,
                    Role: jsonDef.Role,
                    SystemType: systemType,
                    ColumnType: jsonDef.ColumnType,
                    Width: jsonDef.Width,
                    ReadOnly: jsonDef.ReadOnly,
                    Alignment: alignment,
                    PlcMapping: jsonDef.PlcMapping
                );

                columnDefinitions.Add(columnDefinition);
            }

            var sortedDefinitions = columnDefinitions.OrderBy(c => c.Index).ToList().AsReadOnly();
            return Result.Ok<IReadOnlyList<ColumnDefinition>>(sortedDefinitions);
        }
        catch (FileNotFoundException)
        {
            return Result.Fail($"Configuration file not found at: '{schemaPath}'");
        }
        catch (JsonException ex)
        {
            return Result.Fail($"Error parsing JSON file: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"An unexpected error occurred while loading the schema: {ex.Message}");
        }
    }
}