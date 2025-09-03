#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Models.ActionTargets;

namespace NtoLib.Recipes.MbeTable.Config.Loaders;

/// <summary>
/// Default implementation for loading pin groups configuration from PinGroups.json.
/// </summary>
public sealed class PinGroupsDataDataLoader : IPinGroupsDataLoader
{
    /// <inheritdoc />
    public Result<List<PinGroupData>> LoadPinGroups(string configPath)
    {
        try
        {
            var jsonText = File.ReadAllText(configPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            };
            options.Converters.Add(new JsonStringEnumConverter());

            var pinGroups = JsonSerializer.Deserialize<List<PinGroupData>>(jsonText, options);

            if (pinGroups is null || pinGroups.Count == 0)
            {
                return Result.Fail("Failed to deserialize PinGroups.json. The file might be empty or invalid.");
            }

            return Result.Ok(pinGroups);
        }
        catch (FileNotFoundException)
        {
            return Result.Fail($"Pin group configuration file not found at: '{configPath}'");
        }
        catch (JsonException ex)
        {
            return Result.Fail($"Error parsing PinGroups.json: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result.Fail($"An unexpected error occurred while loading pin groups data: {ex.Message}");
        }
    }
}