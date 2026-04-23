using System;

using FluentResults;

using MasterSCADA.Hlp;

using OpcUaClient.Client;
using OpcUaClient.Client.Common;
using OpcUaClient.Client.Common.Data;

namespace NtoLib.OpcTreeManager.TreeOperations;

internal static class OpcProtocolAccessor
{
	internal static Result<OpcUaProtocol> GetProtocol(IProjectHlp project, string opcFbPath)
	{
		var searchPath = opcFbPath;

		while (!string.IsNullOrEmpty(searchPath))
		{
			var treeItem = project.SafeItem<ITreeItemHlp>(searchPath);

			if (treeItem is ITreeObjectHlp treeObject && treeObject.FBObject is OpcUaClientHostObject hostObject)
			{
				return ResolveProtocol(hostObject, searchPath);
			}

			var lastDot = searchPath.LastIndexOf('.');
			searchPath = lastDot >= 0 ? searchPath[..lastDot] : string.Empty;
		}

		return Result.Fail($"No OPC UA FB node found at path '{opcFbPath}' or any of its ancestors.");
	}

	internal static Result<(OpcUaScadaItem Group, string RelativePath)> FindGroup(
		OpcUaProtocol protocol, string groupName)
	{
		var found = FindGroupRecursive(protocol.ScadaRootNode, groupName, string.Empty);

		return found != null
			? Result.Ok(found.Value)
			: Result.Fail($"OPC group '{groupName}' not found in ScadaRootNode.");
	}

	private static (OpcUaScadaItem Group, string RelativePath)? FindGroupRecursive(
		OpcUaScadaItem node, string groupName, string prefix)
	{
		foreach (var child in node.Items)
		{
			var childPath = prefix.Length == 0 ? child.Name : prefix + "." + child.Name;

			if (child.IsGroup && string.Equals(child.Name, groupName, StringComparison.Ordinal))
			{
				return (child, childPath);
			}

			var found = FindGroupRecursive(child, groupName, childPath);

			if (found != null)
			{
				return found;
			}
		}

		return null;
	}

	private static Result<OpcUaProtocol> ResolveProtocol(OpcUaClientHostObject hostObject, string resolvedPath)
	{
		var instance = hostObject.Instance;

		if (instance == null)
		{
			return Result.Fail($"OpcUaClientHostObject.Instance is null for node at path '{resolvedPath}'.");
		}

		if (instance is not OpcUaClientInstance clientInstance)
		{
			return Result.Fail(
				$"Instance at path '{resolvedPath}' is of type '{instance.GetType().FullName}', expected OpcUaClientInstance.");
		}

		var protocolInterface = clientInstance.OpcUaProtocol;

		if (protocolInterface is not OpcUaProtocol protocol)
		{
			return Result.Fail(
				$"OpcUaProtocol at path '{resolvedPath}' is of type '{protocolInterface?.GetType().FullName ?? "null"}', expected OpcUaProtocol.");
		}

		return Result.Ok(protocol);
	}
}
