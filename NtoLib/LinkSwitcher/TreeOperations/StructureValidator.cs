using System;
using System.Collections.Generic;
using System.Linq;

using MasterSCADA.Hlp;

using MasterSCADALib;

using NtoLib.LinkSwitcher.Entities;

namespace NtoLib.LinkSwitcher.TreeOperations;

public static class StructureValidator
{
	public static IReadOnlyList<string> FindDifferences(ObjectPair pair)
	{
		var differences = new List<string>();
		CompareChildren(pair.Source, pair.Target, pair.Name, differences);
		return differences;
	}

	private static void CompareChildren(
		ITreeItemHlp source,
		ITreeItemHlp target,
		string contextPath,
		List<string> differences)
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
			differences.Add($"[{contextPath}] Missing in target: {missing}");
		}

		var missingInSource = targetByName.Keys
			.Where(name => !sourceByName.ContainsKey(name))
			.ToList();

		if (missingInSource.Count > 0)
		{
			var missing = string.Join(", ", missingInSource);
			differences.Add($"[{contextPath}] Missing in source: {missing}");
		}

		var commonNames = sourceByName.Keys
			.Where(name => targetByName.ContainsKey(name));

		foreach (var name in commonNames)
		{
			var sourceChild = sourceByName[name];
			var targetChild = targetByName[name];

			if (sourceChild is ITreeItemHlp sourceItem && targetChild is ITreeItemHlp targetItem)
			{
				CompareChildren(sourceItem, targetItem, $"{contextPath}.{name}", differences);
			}
		}
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
