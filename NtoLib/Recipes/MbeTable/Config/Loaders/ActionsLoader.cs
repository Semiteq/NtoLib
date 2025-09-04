#nullable enable
namespace NtoLib.Recipes.MbeTable.Config.Loaders;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;

public class ActionsLoader : IActionsLoader
{
    /// <inheritdoc />   
    public Result<Dictionary<int, ActionDefinition>> LoadActions(string configPath)
    {
        try
        {
            var actionsJson = File.ReadAllText(configPath);
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            jsonOptions.Converters.Add(new JsonStringEnumConverter());

            var actionList = JsonSerializer.Deserialize<List<ActionDefinition>>(actionsJson, jsonOptions);

            if (actionList is null || actionList.Count == 0)
                return Result.Fail("Failed to deserialize actions configuration. The file might be empty or invalid.");

            // Basic validations:
            // - Unique action Id
            // - For each action: unique column keys
            // - If PropertyType==Enum and GroupName is required by your rules, ensure it is provided
            var actionsDictionary = new Dictionary<int, ActionDefinition>();
            foreach (var action in actionList)
            {
                if (actionsDictionary.ContainsKey(action.Id))
                    return Result.Fail($"Duplicate Action ID found in configuration: {action.Id}");

                if (action.Columns is null)
                    return Result.Fail($"Action {action.Id} ('{action.Name}') has null Columns array.");

                var dupKey = action.Columns
                    .GroupBy(c => c.Key, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault(g => g.Key != null && g.Count() > 1)?.Key;

                if (!string.IsNullOrWhiteSpace(dupKey))
                    return Result.Fail($"Action {action.Id} ('{action.Name}') has duplicate column key '{dupKey}'.");

                foreach (var col in action.Columns)
                {
                    if (col.PropertyType == PropertyType.Enum && string.IsNullOrWhiteSpace(col.GroupName))
                        return Result.Fail($"Action {action.Id} ('{action.Name}'): column '{col.Key}' is Enum but has no GroupName.");
                }

                actionsDictionary[action.Id] = action;
            }

            return Result.Ok(actionsDictionary);
        }
        catch (FileNotFoundException)
        {
            return Result.Fail($"Actions configuration file not found at: '{configPath}'");
        }
        catch (JsonException ex)
        {
            return Result.Fail($"Error parsing actions JSON file: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"An unexpected error occurred while loading actions: {ex.Message}");
        }
    }
}