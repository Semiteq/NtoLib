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
		var sourceRootPrefix = sourceRootPath + ".";

		const TreeItemMask PinMask = TreeItemMask.Pin | TreeItemMask.Pout | TreeItemMask.Variable;
		var allPins = source.EnumAllChilds(PinMask, 0);

		var operations = new List<LinkOperation>();

		foreach (var child in allPins)
		{
			if (child is not ITreePinHlp pin)
			{
				continue;
			}

			if (!pin.FullName.StartsWith(sourceRootPrefix, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var targetPinPath = targetRootPath + pin.FullName.Substring(sourceRootPath.Length);

			CollectIncomingOperations(pin, targetPinPath, operations);
			CollectOutgoingOperations(pin, targetPinPath, operations);
			CollectIConnectOperations(pin, targetPinPath, operations);
		}

		return Result.Ok<IReadOnlyList<LinkOperation>>(operations);
	}

	private static void CollectIncomingOperations(
		ITreePinHlp pin,
		string targetPinPath,
		List<LinkOperation> operations)
	{
		var incomingConnections = pin.GetConnections(EConnectionTypeMask.ctGenericPin);

		foreach (var externalPin in incomingConnections)
		{
			operations.Add(new LinkOperation(
				externalPin.FullName,
				pin.FullName,
				targetPinPath,
				IsIncoming: true));
		}
	}

	private static void CollectOutgoingOperations(
		ITreePinHlp pin,
		string targetPinPath,
		List<LinkOperation> operations)
	{
		var outgoingConnections = pin.GetConnections(EConnectionTypeMask.ctGenericPout);

		foreach (var externalPin in outgoingConnections)
		{
			operations.Add(new LinkOperation(
				externalPin.FullName,
				pin.FullName,
				targetPinPath,
				IsIncoming: false));
		}
	}

	private static void CollectIConnectOperations(
		ITreePinHlp pin,
		string targetPinPath,
		List<LinkOperation> operations)
	{
		var iConnectLinks = pin.GetConnections(EConnectionTypeMask.ctIConnect);

		foreach (var externalPin in iConnectLinks)
		{
			operations.Add(new LinkOperation(
				externalPin.FullName,
				pin.FullName,
				targetPinPath,
				IsIncoming: false,
				IsIConnect: true));
		}
	}
}
