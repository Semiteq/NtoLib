using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.PinGroups;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

/// <summary>
/// Validates pin group definitions from PinGroupDefs.yaml.
/// Aggregates structure, uniqueness, and non-overlapping pin ranges.
/// </summary>
public sealed class PinGroupDefsValidator : ISectionValidator<YamlPinGroupDefinition>
{
	public Result Validate(IReadOnlyList<YamlPinGroupDefinition> items)
	{
		var emptyCheck = ValidationCheck.NotEmpty(items, "PinGroupDefs.yaml");
		if (emptyCheck.IsFailed)
			return emptyCheck;

		var errors = new List<ConfigError>();

		var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var uniqueIds = new HashSet<int>();

		foreach (var group in items)
		{
			var context = $"PinGroupDefs.yaml, GroupName='{group.GroupName}'";

			AddIfFailed(ValidationCheck.NotEmpty(group.GroupName, context, "group_name"), errors);
			AddIfFailed(ValidationCheck.Positive(group.PinGroupId, context, "pin_group_id"), errors);
			AddIfFailed(ValidationCheck.Positive(group.FirstPinId, context, "first_pin_id"), errors);
			AddIfFailed(ValidationCheck.Positive(group.PinQuantity, context, "pin_quantity"), errors);

			if (!string.IsNullOrWhiteSpace(group.GroupName))
				AddIfFailed(ValidationCheck.Unique(group.GroupName, uniqueNames, "PinGroupDefs.yaml", "group_name"),
					errors);

			AddIfFailed(ValidationCheck.Unique(group.PinGroupId, uniqueIds, context, "pin_group_id"), errors);

			// Order rule
			if (group.PinGroupId > group.FirstPinId)
			{
				errors.Add(new ConfigError(
						$"FirstPinId ({group.FirstPinId}) must be >= PinGroupId ({group.PinGroupId}).",
						section: "PinGroupDefs.yaml",
						context: context)
					.WithDetail("GroupName", group.GroupName)
					.WithDetail("PinGroupId", group.PinGroupId)
					.WithDetail("FirstPinId", group.FirstPinId));
			}
		}

		// Non-overlapping ranges check (only if basic structure is ok)
		errors.AddRange(ValidateNonOverlappingRanges(items));

		return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
	}

	private static List<ConfigError> ValidateNonOverlappingRanges(IReadOnlyList<YamlPinGroupDefinition> items)
	{
		var errors = new List<ConfigError>();

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
				errors.Add(new ConfigError(
						$"Pin ranges overlap between '{prev.GroupName}' [{prev.Start}..{prev.End}] and '{curr.GroupName}' [{curr.Start}..{curr.End}].",
						section: "PinGroupDefs.yaml",
						context: "ranges-overlap")
					.WithDetail("Group1", prev.GroupName)
					.WithDetail("Group2", curr.GroupName)
					.WithDetail("Range1", $"[{prev.Start}..{prev.End}]")
					.WithDetail("Range2", $"[{curr.Start}..{curr.End}]"));
			}
		}

		return errors;
	}

	private static void AddIfFailed(Result result, List<ConfigError> errors)
	{
		if (result.IsFailed)
		{
			foreach (var e in result.Errors)
			{
				if (e is ConfigError ce)
					errors.Add(ce);
				else
					errors.Add(new ConfigError(e.Message, "PinGroupDefs.yaml", "validation"));
			}
		}
	}
}
