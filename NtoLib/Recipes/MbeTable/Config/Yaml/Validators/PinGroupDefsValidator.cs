#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.PinGroups;
using NtoLib.Recipes.MbeTable.Errors;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Validators;

/// <summary>
/// Validates pin groups configuration, returning FluentResults.
/// </summary>
public sealed class PinGroupDefsValidator : IPinGroupDefsValidator
{
    /// <summary>
    /// Validates the provided groups.
    /// </summary>
    public Result Validate(IReadOnlyCollection<PinGroupData> groups)
    {
        var nonEmptyResult = ValidateNonEmptyCollection(groups);
        if (nonEmptyResult.IsFailed)
            return nonEmptyResult;

        var groupFieldsResult = ValidateGroupContentIds(groups);
        if (groupFieldsResult.IsFailed)
            return groupFieldsResult;

        var uniqueNamesResult = ValidateUniqueGroupNames(groups);
        if (uniqueNamesResult.IsFailed)
            return uniqueNamesResult;

        var uniqueIdsResult = ValidateUniqueGroupIds(groups);
        if (uniqueIdsResult.IsFailed)
            return uniqueIdsResult;

        var rangesResult = ValidateNonOverlappingPinRanges(groups);
        if (rangesResult.IsFailed)
            return rangesResult;

        return Result.Ok();
    }

    private Result ValidateNonEmptyCollection(IReadOnlyCollection<PinGroupData> groups)
    {
        if (groups.Count == 0)
            return Result.Fail(new RecipeError("PinGroupDefs.yaml is empty or invalid.", RecipeErrorCodes.ConfigInvalidSchema));
        
        return Result.Ok();
    }

    private Result ValidateGroupContentIds(IReadOnlyCollection<PinGroupData> groups)
    {
        foreach (var g in groups)
        {
            if (string.IsNullOrWhiteSpace(g.GroupName))
                return Result.Fail(new RecipeError("In PinGroupDefs.yaml, a group has an empty GroupName.", RecipeErrorCodes.ConfigInvalidSchema));
        
            if (g.PinQuantity <= 0)
                return Result.Fail(new RecipeError($"In PinGroupDefs.yaml, group '{g.GroupName}' has invalid PinQuantity={g.PinQuantity}. Must be > 0.", RecipeErrorCodes.ConfigInvalidSchema));
        
            if (g.FirstPinId <= 0)
                return Result.Fail(new RecipeError($"In PinGroupDefs.yaml, group '{g.GroupName}' has invalid FirstPinId={g.FirstPinId}. Must be > 0.", RecipeErrorCodes.ConfigInvalidSchema));
        
            if (g.PinGroupId <= 0)
                return Result.Fail(new RecipeError($"In PinGroupDefs.yaml, group '{g.GroupName}' has invalid PinGroupId={g.PinGroupId}. Must be > 0.", RecipeErrorCodes.ConfigInvalidSchema));
        
            if (g.FirstPinId <= g.PinGroupId)
                return Result.Fail(new RecipeError($"In PinGroupDefs.yaml, group '{g.GroupName}' has invalid FirstPinId={g.FirstPinId}. Must be > PinGroupId={g.PinGroupId}.", RecipeErrorCodes.ConfigInvalidSchema));
        }
        
        return Result.Ok();
    }

    private Result ValidateUniqueGroupNames(IReadOnlyCollection<PinGroupData> groups)
    {
        var dupName = groups
            .GroupBy(g => g.GroupName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(gr => gr.Count() > 1)?.Key;
        if (dupName != null)
            return Result.Fail(new RecipeError($"In PinGroupDefs.yaml, duplicate GroupName detected: '{dupName}'.", RecipeErrorCodes.ConfigInvalidSchema));
        return Result.Ok();
    }

    private Result ValidateUniqueGroupIds(IReadOnlyCollection<PinGroupData> groups)
    {
        var dupGroupId = groups
            .GroupBy(g => g.PinGroupId)
            .FirstOrDefault(gr => gr.Count() > 1)?.Key;
        if (dupGroupId.HasValue)
            return Result.Fail(new RecipeError($"In PinGroupDefs.yaml, duplicate PinGroupId detected: '{dupGroupId.Value}'.", RecipeErrorCodes.ConfigInvalidSchema));
        
        return Result.Ok();
    }

    private Result ValidateNonOverlappingPinRanges(IReadOnlyCollection<PinGroupData> groups)
    {
        var ranges = groups
            .Select(g => new { g.GroupName, Start = g.FirstPinId, End = g.FirstPinId + g.PinQuantity - 1 })
            .OrderBy(r => r.Start)
            .ToArray();

        for (var i = 1; i < ranges.Length; i++)
        {
            var prev = ranges[i - 1];
            var curr = ranges[i];
            if (curr.Start <= prev.End)
            {
                return Result.Fail(new RecipeError(
                    $"In PinGroupDefs.yaml, pin id ranges overlap: '{prev.GroupName}' [{prev.Start}..{prev.End}] and '{curr.GroupName}' [{curr.Start}..{curr.End}].",
                    RecipeErrorCodes.ConfigInvalidSchema));
            }
        }

        return Result.Ok();
    }
}