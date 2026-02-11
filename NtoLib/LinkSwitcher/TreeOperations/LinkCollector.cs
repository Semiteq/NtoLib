using System;
using System.Collections.Generic;

using FluentResults;

using MasterSCADA.Hlp;

using MasterSCADALib;

using NtoLib.LinkSwitcher.Entities;

namespace NtoLib.LinkSwitcher.TreeOperations;

public sealed class LinkCollector
{
	public Result<IReadOnlyList<LinkOperation>> CollectOperations(ObjectPair pair, bool forward)
	{
		var source = forward ? pair.Source : pair.Target;
		var target = forward ? pair.Target : pair.Source;

		var sourceRootPath = source.FullName;
		var targetRootPath = target.FullName;

		const TreeItemMask PinMask = TreeItemMask.Pin | TreeItemMask.Pout | TreeItemMask.Variable;
		var allPins = source.EnumAllChilds(PinMask, 0);

		var operations = new List<LinkOperation>();

		foreach (var child in allPins)
		{
			if (child is not ITreePinHlp pin)
			{
				continue;
			}

			var relativePath = ExtractRelativePath(pin.FullName, sourceRootPath);
			if (relativePath == null)
			{
				continue;
			}

			var targetPinPath = targetRootPath + "." + relativePath;

			CollectIncomingOperations(pin, sourceRootPath, targetPinPath, operations);
			CollectOutgoingOperations(pin, sourceRootPath, targetPinPath, operations);
			CollectIConnectOperations(pin, sourceRootPath, targetPinPath, operations);
		}

		return Result.Ok<IReadOnlyList<LinkOperation>>(operations);
	}

	private static void CollectIncomingOperations(
		ITreePinHlp pin,
		string sourceRootPath,
		string targetPinPath,
		List<LinkOperation> operations)
	{
		var incomingConnections = pin.GetConnections(EConnectionTypeMask.ctGenericPin);

		foreach (var externalPin in incomingConnections)
		{
			if (IsInternalConnection(externalPin.FullName, sourceRootPath))
			{
				continue;
			}

			operations.Add(new LinkOperation(
				externalPin.FullName,
				pin.FullName,
				targetPinPath,
				IsIncoming: true));
		}
	}

	private static void CollectOutgoingOperations(
		ITreePinHlp pin,
		string sourceRootPath,
		string targetPinPath,
		List<LinkOperation> operations)
	{
		var outgoingConnections = pin.GetConnections(EConnectionTypeMask.ctGenericPout);

		foreach (var externalPin in outgoingConnections)
		{
			if (IsInternalConnection(externalPin.FullName, sourceRootPath))
			{
				continue;
			}

			operations.Add(new LinkOperation(
				externalPin.FullName,
				pin.FullName,
				targetPinPath,
				IsIncoming: false));
		}
	}

	private static void CollectIConnectOperations(
		ITreePinHlp pin,
		string sourceRootPath,
		string targetPinPath,
		List<LinkOperation> operations)
	{
		var iConnectLinks = pin.GetConnections(EConnectionTypeMask.ctIConnect);

		foreach (var externalPin in iConnectLinks)
		{
			if (IsInternalConnection(externalPin.FullName, sourceRootPath))
			{
				continue;
			}

			operations.Add(new LinkOperation(
				externalPin.FullName,
				pin.FullName,
				targetPinPath,
				IsIncoming: false,
				IsIConnect: true));
		}
	}

	private static bool IsInternalConnection(string pinFullName, string rootPath)
	{
		return pinFullName.StartsWith(rootPath + ".", StringComparison.OrdinalIgnoreCase)
			   || string.Equals(pinFullName, rootPath, StringComparison.OrdinalIgnoreCase);
	}

	private static string? ExtractRelativePath(string fullPath, string rootPath)
	{
		var prefix = rootPath + ".";

		if (!fullPath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		return fullPath[prefix.Length..];
	}
}
