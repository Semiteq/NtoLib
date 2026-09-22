using System;
using System.Text;

using FluentResults;

using MasterSCADA.Hlp;

using OpcUaClient.Client;
using OpcUaClient.Client.Common;
using OpcUaClient.Client.Common.Data;

namespace NtoLib.OpcTreeManager.TreeOperations;

internal static class OpcProtocolAccessor
{
	private const int MaxDescribeDepth = 3;
	private const int MaxDescribeChildren = 20;
	private const int MaxDescribeLines = 200;

	/// <summary>Resolves the configured path exactly: a path below the OPC UA FB node is refused.</summary>
	internal static Result<OpcUaProtocol> GetProtocol(string opcFbPath, Func<string, ITreeItemHlp?> resolveItem)
	{
		var treeItem = resolveItem(opcFbPath);

		if (treeItem?.FBObject is not OpcUaClientHostObject hostObject)
		{
			return Result.Fail(
				$"No OPC UA FB node at path '{opcFbPath}'. OpcFbPath must name the OPC UA FB node itself.");
		}

		return ResolveProtocol(hostObject, opcFbPath);
	}

	/// <summary>Finds the group by node type, not by contents: <c>IsGroup</c> is derived from
	/// <c>Items.Count</c>, so an empty folder reads as a non-group.</summary>
	internal static Result<(OpcUaScadaItem Group, string RelativePath)> FindGroup(
		OpcUaProtocol protocol, string groupName)
	{
		var root = protocol.ScadaRootNode;
		var found = FindGroupRecursive(root, groupName, string.Empty);

		return found != null
			? Result.Ok(found.Value)
			: Result.Fail(
				$"OPC group '{groupName}' not found in ScadaRootNode. ScadaRootNode holds:{Environment.NewLine}{DescribeTree(root)}");
	}

	private static (OpcUaScadaItem Group, string RelativePath)? FindGroupRecursive(
		OpcUaScadaItem node, string groupName, string prefix)
	{
		foreach (var child in node.Items)
		{
			var childPath = prefix.Length == 0 ? child.Name : prefix + "." + child.Name;

			var isContainer = !child.IsNode || child.IsGroup;

			if (isContainer && string.Equals(child.Name, groupName, StringComparison.Ordinal))
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

	/// <summary>Renders the tree under <paramref name="root"/> as indented name, IsNode and child-count lines.</summary>
	internal static string DescribeTree(OpcUaScadaItem root)
	{
		var dump = new TreeDump();

		AppendChildren(root, dump, depth: 1);

		if (dump.OmittedNodeCount > 0)
		{
			dump.Builder.AppendLine(
				$"... {dump.OmittedNodeCount} node(s) not shown: the dump is capped at {MaxDescribeLines} lines");
		}

		return dump.Builder.Length == 0 ? "(no nodes)" : dump.Builder.ToString().TrimEnd();
	}

	// The walk continues past the cap without emitting so the closing marker counts every node it left out.
	private static void AppendChildren(OpcUaScadaItem node, TreeDump dump, int depth)
	{
		var indent = new string(' ', (depth - 1) * 2);
		var shown = 0;

		foreach (var child in node.Items)
		{
			if (shown == MaxDescribeChildren)
			{
				AppendHiddenMarker(dump, indent, node.Items.Count - shown);
				return;
			}

			shown++;

			var childCount = child.Items.Count;

			if (!dump.TryAppendLine($"{indent}{child.Name} (IsNode={child.IsNode}, children={childCount})"))
			{
				dump.OmittedNodeCount++;
			}

			if (depth < MaxDescribeDepth)
			{
				AppendChildren(child, dump, depth + 1);
			}
			else if (childCount > 0)
			{
				AppendHiddenMarker(dump, indent + "  ", childCount);
			}
		}
	}

	// A marker stands for the direct children of one node, never for the subtrees they carry, and
	// it is the only report those children had, so the trailer inherits its count when it is dropped.
	private static void AppendHiddenMarker(TreeDump dump, string indent, int hiddenCount)
	{
		if (!dump.TryAppendLine($"{indent}... {hiddenCount} direct child node(s) not shown"))
		{
			dump.OmittedNodeCount += hiddenCount;
		}
	}

	private sealed class TreeDump
	{
		private int _lineCount;

		internal StringBuilder Builder { get; } = new();

		internal int OmittedNodeCount { get; set; }

		internal bool TryAppendLine(string line)
		{
			if (_lineCount == MaxDescribeLines)
			{
				return false;
			}

			Builder.AppendLine(line);
			_lineCount++;

			return true;
		}
	}

	private static Result<OpcUaProtocol> ResolveProtocol(OpcUaClientHostObject hostObject, string opcFbPath)
	{
		var instance = hostObject.Instance;

		if (instance == null)
		{
			return Result.Fail($"OpcUaClientHostObject.Instance is null for node at path '{opcFbPath}'.");
		}

		if (instance is not OpcUaClientInstance clientInstance)
		{
			return Result.Fail(
				$"Instance at path '{opcFbPath}' is of type '{instance.GetType().FullName}', expected OpcUaClientInstance.");
		}

		var protocolInterface = clientInstance.OpcUaProtocol;

		if (protocolInterface is not OpcUaProtocol protocol)
		{
			return Result.Fail(
				$"OpcUaProtocol at path '{opcFbPath}' is of type '{protocolInterface?.GetType().FullName ?? "null"}', expected OpcUaProtocol.");
		}

		return Result.Ok(protocol);
	}
}
