using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;

namespace NtoLib.Recipes.MbeTable.Config.Loaders;

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
                PropertyNameCaseInsensitive = true
            };
            jsonOptions.Converters.Add(new JsonStringEnumConverter());

            var actionList = JsonSerializer.Deserialize<List<ActionDefinition>>(actionsJson, jsonOptions);

            if (actionList is null)
                return Result.Fail("Failed to deserialize actions configuration. The file might be empty or invalid.");

            var actionsDictionary = new Dictionary<int, ActionDefinition>();
            foreach (var action in actionList)
            {
                if (actionsDictionary.ContainsKey(action.Id))
                    return Result.Fail($"Duplicate Action ID found in configuration: {action.Id}");

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