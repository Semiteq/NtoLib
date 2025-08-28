#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config;

namespace NtoLib.Recipes.MbeTable.Composition;

/// <inheritdoc />
public sealed class ConfigurationLoader : IConfigurationLoader
{
    private const string TableSchemaFileName = "TableSchema.json";
    private const string ActionsConfigFileName = "ActionSchema.json"; // Используем ваш существующий файл
    private readonly ITableSchemaLoader _tableSchemaLoader;

    public ConfigurationLoader()
    {
        _tableSchemaLoader = new TableSchemaLoader(TableSchemaFileName);
    }

    /// <inheritdoc />
    public Result<AppConfiguration> LoadConfiguration()
    {
        try
        {
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // 1. Load Table Schema
            var tableSchemaPath = Path.Combine(baseDirectory, TableSchemaFileName);
            if (!File.Exists(tableSchemaPath))
                return Result.Fail($"Configuration file not found: {tableSchemaPath}");

            var columns = _tableSchemaLoader.LoadSchema();
            var tableSchema = new TableSchema(columns);

            // 2. Load Actions Configuration
            var actionsConfigPath = Path.Combine(baseDirectory, ActionsConfigFileName);
            if (!File.Exists(actionsConfigPath))
                return Result.Fail($"Configuration file not found: {actionsConfigPath}");

            var actionsJson = File.ReadAllText(actionsConfigPath);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            jsonOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());

            var actionList = JsonSerializer.Deserialize<List<ActionDefinition>>(actionsJson, jsonOptions);

            if (actionList is null)
                return Result.Fail("Failed to deserialize actions configuration. The file might be empty or invalid.");

            // 3. Validate and build dictionary
            var actionsDictionary = new Dictionary<int, ActionDefinition>();
            foreach (var action in actionList)
            {
                if (actionsDictionary.ContainsKey(action.Id))
                    return Result.Fail($"Duplicate Action ID found in configuration: {action.Id}");

                actionsDictionary[action.Id] = action;
            }

            var appConfiguration = new AppConfiguration(tableSchema, actionsDictionary);
            return Result.Ok(appConfiguration);
        }
        catch (Exception ex)
        {
            return Result.Fail(new Error("A critical error occurred while loading configuration.").CausedBy(ex));
        }
    }
}