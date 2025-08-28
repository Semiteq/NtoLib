#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace NtoLib.Recipes.MbeTable.Config;

/// <summary>
/// Loads the table schema definition from a JSON configuration file.
/// </summary>
public class TableSchemaLoader : ITableSchemaLoader
{
    private readonly string _configPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="TableSchemaLoader"/> class.
    /// </summary>
    /// <param name="configFileName">The name of the configuration file to load.</param>
    public TableSchemaLoader(string configFileName = "TableSchema.json")
    {
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFileName);
    }

    /// <summary>
    /// A temporary class used for deserializing the JSON schema configuration.
    /// Must be a class with a parameterless constructor for System.Text.Json compatibility.
    /// </summary>
    private class JsonColumnDefinition
    {
        public string Key { get; set; }
        public int Index { get; set; }
        public string Code { get; set; }
        public string UiName { get; set; }
        public string SystemType { get; set; }
        public int Width { get; set; }
        public bool ReadOnly { get; set; }
        public string Alignment { get; set; }
        public PlcMapping? PlcMapping { get; set; }
    }

    public IReadOnlyList<ColumnDefinition> LoadSchema()
    {
        if (!File.Exists(_configPath))
        {
            throw new FileNotFoundException("Schema configuration file not found.", _configPath);
        }

        var jsonText = File.ReadAllText(_configPath);
        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var jsonDefinitions = JsonSerializer.Deserialize<List<JsonColumnDefinition>>(jsonText, jsonOptions);

        if (jsonDefinitions == null)
        {
            throw new InvalidOperationException("Failed to deserialize table schema. The JSON might be empty or invalid.");
        }

        return jsonDefinitions.Select(jsonDef =>
        {
            var systemType = Type.GetType(jsonDef.SystemType, throwOnError: true, ignoreCase: true);
            var alignment = (DataGridViewContentAlignment)Enum.Parse(typeof(DataGridViewContentAlignment), jsonDef.Alignment, ignoreCase: true);

            return new ColumnDefinition(
                Key: new ColumnIdentifier(jsonDef.Key),
                Index: jsonDef.Index,
                Code: jsonDef.Code,
                UiName: jsonDef.UiName,
                SystemType: systemType!,
                Width: jsonDef.Width,
                ReadOnly: jsonDef.ReadOnly,
                Alignment: alignment,
                PlcMapping: jsonDef.PlcMapping
            );
        }).OrderBy(c => c.Index).ToList().AsReadOnly();
    }
}