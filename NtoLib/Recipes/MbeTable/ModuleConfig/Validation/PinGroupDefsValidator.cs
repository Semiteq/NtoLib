using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.Errors;
using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.PinGroups;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

/// <summary>
/// Validates pin group definitions from PinGroupDefs.yaml.
/// Checks structure, uniqueness, and non-overlapping pin ranges.
/// </summary>
public sealed class PinGroupDefsValidator : ISectionValidator<YamlPinGroupDefinition>
{
    public Result Validate(IReadOnlyList<YamlPinGroupDefinition> items)
    {
        var checkEmpty = ValidationCheck.NotEmpty(items, "PinGroupDefs.yaml");
        if (checkEmpty.IsFailed)
            return checkEmpty;

        var structureResult = ValidateEachStructure(items);
        if (structureResult.IsFailed)
            return structureResult;

        return ValidateNonOverlappingRanges(items);
    }

    private static Result ValidateEachStructure(IReadOnlyList<YamlPinGroupDefinition> items)
    {
        var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueIds = new HashSet<int>();

        foreach (var group in items)
        {
            var context = $"PinGroupDefs.yaml, GroupName='{group.GroupName}'";

            var nameCheck = ValidationCheck.NotEmpty(group.GroupName, context, "group_name");
            if (nameCheck.IsFailed)
                return nameCheck;

            var groupIdCheck = ValidationCheck.Positive(group.PinGroupId, context, "pin_group_id");
            if (groupIdCheck.IsFailed)
                return groupIdCheck;

            var firstPinCheck = ValidationCheck.Positive(group.FirstPinId, context, "first_pin_id");
            if (firstPinCheck.IsFailed)
                return firstPinCheck;

            var quantityCheck = ValidationCheck.Positive(group.PinQuantity, context, "pin_quantity");
            if (quantityCheck.IsFailed)
                return quantityCheck;

            var nameUniqueCheck = ValidationCheck.Unique(group.GroupName, uniqueNames, "PinGroupDefs.yaml", "group_name");
            if (nameUniqueCheck.IsFailed)
                return nameUniqueCheck;

            var idUniqueCheck = ValidationCheck.Unique(group.PinGroupId, uniqueIds, context, "pin_group_id");
            if (idUniqueCheck.IsFailed)
                return idUniqueCheck;

            var orderCheck = ValidatePinIdOrder(group, context);
            if (orderCheck.IsFailed)
                return orderCheck;
        }

        return Result.Ok();
    }

    private static Result ValidatePinIdOrder(YamlPinGroupDefinition group, string context)
    {
        if (group.PinGroupId > group.FirstPinId)
        {
            return Result.Fail(new Error($"{context}: FirstPinId ({group.FirstPinId}) must be >= PinGroupId ({group.PinGroupId}).")
                .WithMetadata("code", Codes.ConfigInvalidSchema)
                .WithMetadata("GroupName", group.GroupName)
                .WithMetadata("PinGroupId", group.PinGroupId.ToString())
                .WithMetadata("FirstPinId", group.FirstPinId.ToString()));
        }

        return Result.Ok();
    }

    private static Result ValidateNonOverlappingRanges(IReadOnlyList<YamlPinGroupDefinition> items)
    {
        var ranges = items
            .Select(g => new
            {
                g.GroupName,
                Start = g.FirstPinId,
                End = g.FirstPinId + g.PinQuantity - 1
            })
            .OrderBy(r => r.Start)
            .ToArray();

        for (var i = 1; i < ranges.Length; i++)
        {
            var prev = ranges[i - 1];
            var curr = ranges[i];

            if (curr.Start <= prev.End)
            {
                return Result.Fail(new Error($"PinGroupDefs.yaml: Pin ranges overlap between '{prev.GroupName}' [{prev.Start}..{prev.End}] and '{curr.GroupName}' [{curr.Start}..{curr.End}].")
                    .WithMetadata("code", Codes.ConfigInvalidSchema)
                    .WithMetadata("Group1", prev.GroupName)
                    .WithMetadata("Group2", curr.GroupName)
                    .WithMetadata("Range1", $"[{prev.Start}..{prev.End}]")
                    .WithMetadata("Range2", $"[{curr.Start}..{curr.End}]"));
            }
        }

        return Result.Ok();
    }
}