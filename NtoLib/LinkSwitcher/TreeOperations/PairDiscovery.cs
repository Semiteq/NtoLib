using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using MasterSCADA.Hlp;

using MasterSCADALib;

using NtoLib.LinkSwitcher.Entities;

namespace NtoLib.LinkSwitcher.TreeOperations;

public sealed class PairDiscovery
{
	private readonly IProjectHlp _project;

	public PairDiscovery(IProjectHlp project)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));
	}

	public Result<IReadOnlyList<ObjectPair>> FindPairs(string searchPath)
	{
		if (string.IsNullOrWhiteSpace(searchPath))
		{
			return Result.Fail("SearchPath is empty.");
		}

		var container = _project.SafeItem<ITreeItemHlp>(searchPath);
		if (container == null)
		{
			return Result.Fail($"Container not found: {searchPath}");
		}

		var children = container.EnumHlpChilds(TreeItemMask.Group);
		var childMap = new Dictionary<string, ITreeItemHlp>(StringComparer.OrdinalIgnoreCase);

		foreach (var child in children)
		{
			if (child is ITreeItemHlp treeItem)
			{
				childMap[treeItem.Name] = treeItem;
			}
		}

		var pairs = new List<ObjectPair>();
		var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var name in childMap.Keys.ToList())
		{
			if (processed.Contains(name))
			{
				continue;
			}

			if (name.EndsWith("2", StringComparison.Ordinal))
			{
				var baseName = name.Substring(0, name.Length - 1);
				if (childMap.ContainsKey(baseName) && !processed.Contains(baseName))
				{
					pairs.Add(new ObjectPair(baseName, childMap[baseName], childMap[name]));
					processed.Add(baseName);
					processed.Add(name);
				}
			}
			else
			{
				var suffixedName = name + "2";
				if (childMap.ContainsKey(suffixedName))
				{
					pairs.Add(new ObjectPair(name, childMap[name], childMap[suffixedName]));
					processed.Add(name);
					processed.Add(suffixedName);
				}
			}
		}

		return Result.Ok<IReadOnlyList<ObjectPair>>(pairs);
	}
}
