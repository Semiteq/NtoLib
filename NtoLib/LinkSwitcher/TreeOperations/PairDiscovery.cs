using System;
using System.Collections.Generic;

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

	public Result<IReadOnlyList<ObjectPair>> FindPairs(string sourcePath, string targetPath)
	{
		if (string.IsNullOrWhiteSpace(sourcePath))
		{
			return Result.Fail("SourcePath is empty.");
		}

		if (string.IsNullOrWhiteSpace(targetPath))
		{
			return Result.Fail("TargetPath is empty.");
		}

		var sourceContainer = _project.SafeItem<ITreeItemHlp>(sourcePath);
		if (sourceContainer == null)
		{
			return Result.Fail($"Source container not found: {sourcePath}");
		}

		var targetContainer = _project.SafeItem<ITreeItemHlp>(targetPath);
		if (targetContainer == null)
		{
			return Result.Fail($"Target container not found: {targetPath}");
		}

		var targetChildMap = BuildChildMap(targetContainer);
		var pairs = new List<ObjectPair>();

		foreach (var sourceChild in sourceContainer.EnumHlpChilds(TreeItemMask.Group))
		{
			if (sourceChild is not ITreeItemHlp sourceItem)
			{
				continue;
			}

			if (targetChildMap.TryGetValue(sourceItem.Name, out var targetItem))
			{
				pairs.Add(new ObjectPair(sourceItem.Name, sourceItem, targetItem));
			}
		}

		return Result.Ok<IReadOnlyList<ObjectPair>>(pairs);
	}

	private static Dictionary<string, ITreeItemHlp> BuildChildMap(ITreeItemHlp container)
	{
		var map = new Dictionary<string, ITreeItemHlp>(StringComparer.OrdinalIgnoreCase);

		foreach (var child in container.EnumHlpChilds(TreeItemMask.Group))
		{
			if (child is ITreeItemHlp treeItem)
			{
				map[treeItem.Name] = treeItem;
			}
		}

		return map;
	}
}
