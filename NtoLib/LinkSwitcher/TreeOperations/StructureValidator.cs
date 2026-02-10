using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using MasterSCADA.Hlp;

using MasterSCADALib;

using NtoLib.LinkSwitcher.Entities;

namespace NtoLib.LinkSwitcher.TreeOperations;

public sealed class StructureValidator
{
	public Result Validate(ObjectPair pair)
	{
		return CompareChildren(pair.Source, pair.Target, pair.Name);
	}

	private Result CompareChildren(ITreeItemHlp source, ITreeItemHlp target, string contextPath)
	{
		var sourceChildren = source.EnumHlpChilds(TreeItemMask.All);
		var targetChildren = target.EnumHlpChilds(TreeItemMask.All);

		var sourceByName = BuildChildMap(sourceChildren);
		var targetByName = BuildChildMap(targetChildren);

		var missingInTarget = sourceByName.Keys
			.Where(name => !targetByName.ContainsKey(name))
			.ToList();

		if (missingInTarget.Count > 0)
		{
			var missing = string.Join(", ", missingInTarget);
			return Result.Fail($"[{contextPath}] Children missing in target: {missing}");
		}

		var missingInSource = targetByName.Keys
			.Where(name => !sourceByName.ContainsKey(name))
			.ToList();

		if (missingInSource.Count > 0)
		{
			var missing = string.Join(", ", missingInSource);
			return Result.Fail($"[{contextPath}] Children missing in source: {missing}");
		}

		foreach (var name in sourceByName.Keys)
		{
			var sourceChild = sourceByName[name];
			var targetChild = targetByName[name];

			if (sourceChild is ITreeItemHlp sourceItem && targetChild is ITreeItemHlp targetItem)
			{
				var childResult = CompareChildren(sourceItem, targetItem, $"{contextPath}.{name}");
				if (childResult.IsFailed)
				{
					return childResult;
				}
			}
		}

		return Result.Ok();
	}

	private static Dictionary<string, ITreeObjectHlp> BuildChildMap(List<ITreeObjectHlp> children)
	{
		var map = new Dictionary<string, ITreeObjectHlp>(StringComparer.OrdinalIgnoreCase);
		foreach (var child in children)
		{
			map[child.Name] = child;
		}

		return map;
	}
}
