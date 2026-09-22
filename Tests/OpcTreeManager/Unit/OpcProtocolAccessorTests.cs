using System;
using System.Globalization;
using System.Linq;

using FluentAssertions;

using MasterSCADA.Hlp;

using NtoLib.OpcTreeManager.TreeOperations;

using OpcUaClient.Client;
using OpcUaClient.Client.Common;
using OpcUaClient.Client.Common.Data;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

/// <summary>
/// Path resolution in <see cref="OpcProtocolAccessor.GetProtocol"/> and group lookup in
/// <see cref="OpcProtocolAccessor.FindGroup"/>.
/// See Docs/known_issues/15-derived-properties-as-identity.md.
/// </summary>
public sealed class OpcProtocolAccessorTests
{
	private const string GroupName = "MBE";

	private const string FbNodePath = "System.Workstation.OPC UA Siemens";
	private const string PathBelowTheFbNode = FbNodePath + ".ServerInterfaces";

	// Mirrors the private caps in OpcProtocolAccessor; moving one there breaks these counts here.
	private const int MaxDescribeChildren = 20;
	private const int MaxDescribeLines = 200;

	// Only the exact-resolution branch emits this sentence. Any walk up to the OPC FB node reaches
	// ResolveProtocol instead and reports its own fault, whichever path that message names.
	private const string ExactResolutionRefusal = "must name the OPC UA FB node itself";

	private const string ResolveProtocolFault = "Instance is null";

	[Fact]
	public void GetProtocol_PathBelowTheFbNode_Fails()
	{
		var fbNode = TreeItemCarrying(new OpcUaClientHostObject());

		var result = OpcProtocolAccessor.GetProtocol(
			PathBelowTheFbNode,
			path => path == FbNodePath ? fbNode : null);

		result.IsFailed.Should().BeTrue("the configured path names a child of the OPC FB node");

		string.Join(";", result.Errors).Should()
			.Contain(ExactResolutionRefusal, "the ancestor holding the OPC FB node was never consulted")
			.And.Contain(PathBelowTheFbNode, "the failure names the configured path");
	}

	[Fact]
	public void GetProtocol_ItemAtThePathIsNotAnOpcFbNode_Fails()
	{
		// The production shape of the fault: the configured path names a real tree item whose
		// FBObject is null, as '...OPC UA Siemens.ServerInterfaces' is in the host.
		var plainItem = TreeItemCarrying(null);

		var result = OpcProtocolAccessor.GetProtocol(FbNodePath, path => path == FbNodePath ? plainItem : null);

		result.IsFailed.Should().BeTrue("an item without an OpcUaClientHostObject is not an OPC FB node");

		string.Join(";", result.Errors).Should()
			.Contain(ExactResolutionRefusal)
			.And.Contain(FbNodePath);
	}

	[Fact]
	public void GetProtocol_PathAtTheFbNode_PassesTheNodeTest()
	{
		// A live OpcUaClientInstance cannot be built here, so the run stops one step further on,
		// inside ResolveProtocol: reaching that message is the proof the node itself was accepted.
		var fbNode = TreeItemCarrying(new OpcUaClientHostObject());

		var result = OpcProtocolAccessor.GetProtocol(FbNodePath, path => path == FbNodePath ? fbNode : null);

		string.Join(";", result.Errors).Should()
			.Contain(ResolveProtocolFault, "the exact path resolved to the OPC FB node");
	}

	[Fact]
	public void FindGroup_GroupWithoutChildren_IsFound()
	{
		var protocol = ProtocolOver(
			Folder("ServerInterfaces",
				Folder(GroupName)));

		var result = OpcProtocolAccessor.FindGroup(protocol, GroupName);

		result.IsSuccess.Should().BeTrue("an empty folder is still a container");
		result.Value.RelativePath.Should().Be("ServerInterfaces.MBE");
		result.Value.Group.Items.Should().BeEmpty();
	}

	[Fact]
	public void FindGroup_GroupAtRootLevel_IsFound()
	{
		var protocol = ProtocolOver(Folder(GroupName));

		var result = OpcProtocolAccessor.FindGroup(protocol, GroupName);

		result.IsSuccess.Should().BeTrue();
		result.Value.RelativePath.Should().Be("MBE", "a root-level group carries no path prefix");
	}

	[Fact]
	public void FindGroup_PopulatedGroup_IsFound()
	{
		var protocol = ProtocolOver(
			Folder("ServerInterfaces",
				Folder(GroupName,
					Leaf("Valves"))));

		var result = OpcProtocolAccessor.FindGroup(protocol, GroupName);

		result.IsSuccess.Should().BeTrue();
		result.Value.RelativePath.Should().Be("ServerInterfaces.MBE");
		result.Value.Group.Items.Should().ContainSingle(i => i.Name == "Valves");
	}

	[Fact]
	public void FindGroup_VariableAsGroup_IsFound()
	{
		// A browse node cut off at MaxBrowseLevel is born IsNode == true and gains children later.
		var variableAsGroup = Leaf(GroupName);
		variableAsGroup.Items.Add(Leaf("Valves"));

		var protocol = ProtocolOver(
			Folder("ServerInterfaces", variableAsGroup));

		var result = OpcProtocolAccessor.FindGroup(protocol, GroupName);

		result.IsSuccess.Should().BeTrue("a variable that holds children is a container");
		result.Value.RelativePath.Should().Be("ServerInterfaces.MBE");
	}

	[Fact]
	public void FindGroup_LeafPinWithMatchingName_IsNotMatched()
	{
		var protocol = ProtocolOver(
			Folder("ServerInterfaces",
				Leaf(GroupName)));

		var result = OpcProtocolAccessor.FindGroup(protocol, GroupName);

		result.IsFailed.Should().BeTrue("an empty IsNode item renders as a pin pair, never as a container");
		string.Join(";", result.Errors).Should().Contain(GroupName);
	}

	[Fact]
	public void FindGroup_NoMatch_FailureCarriesTheTreeShape()
	{
		var protocol = ProtocolOver(
			Folder("ServerInterfaces",
				Folder("Other")));

		var result = OpcProtocolAccessor.FindGroup(protocol, GroupName);

		result.IsFailed.Should().BeTrue("no container carries the requested name");

		string.Join(";", result.Errors).Should()
			.Contain("ServerInterfaces (IsNode=False, children=1)")
			.And.Contain("Other (IsNode=False, children=0)");
	}

	[Fact]
	public void DescribeTree_NestedTree_IndentsByDepthAndReportsChildCount()
	{
		var root = Root(
			Folder("ServerInterfaces",
				Folder(GroupName),
				Leaf("Status")));

		var description = OpcProtocolAccessor.DescribeTree(root);

		description.Should().Be(Lines(
			"ServerInterfaces (IsNode=False, children=2)",
			"  MBE (IsNode=False, children=0)",
			"  Status (IsNode=True, children=0)"));
	}

	[Fact]
	public void DescribeTree_BelowCutoffDepth_ReplacesChildrenWithMarker()
	{
		var root = Root(
			Folder("Level1",
				Folder("Level2",
					Folder("Level3",
						Leaf("Level4a"),
						Leaf("Level4b")))));

		var description = OpcProtocolAccessor.DescribeTree(root);

		description.Should().Be(Lines(
			"Level1 (IsNode=False, children=1)",
			"  Level2 (IsNode=False, children=1)",
			"    Level3 (IsNode=False, children=2)",
			"      ... 2 direct child node(s) not shown"));
	}

	[Fact]
	public void DescribeTree_WideNode_StopsAfterTheBreadthCap()
	{
		const int HiddenChildren = 5;
		var children = new OpcUaScadaItem[MaxDescribeChildren + HiddenChildren];

		for (var i = 0; i < children.Length; i++)
		{
			children[i] = Leaf("Tag" + i);
		}

		var description = OpcProtocolAccessor.DescribeTree(Root(children));

		description.Split('\n').Should()
			.HaveCount(MaxDescribeChildren + 1, $"{MaxDescribeChildren} nodes plus one marker line");
		description.Should().EndWith($"... {HiddenChildren} direct child node(s) not shown");
		description.Should().NotContain("Tag" + MaxDescribeChildren);
	}

	[Fact]
	public void DescribeTree_TreeLargerThanTheLineCap_StopsAtTheCapAndCountsTheRest()
	{
		// Folders filled to the breadth cap emit no marker, so every line here is a node line and
		// the per-node cutoffs alone would print all 315 of them.
		const int FolderCount = 15;
		var folders = new OpcUaScadaItem[FolderCount];

		for (var f = 0; f < folders.Length; f++)
		{
			var leaves = new OpcUaScadaItem[MaxDescribeChildren];

			for (var l = 0; l < leaves.Length; l++)
			{
				leaves[l] = Leaf($"Tag{f}_{l}");
			}

			folders[f] = Folder("Folder" + f, leaves);
		}

		var description = OpcProtocolAccessor.DescribeTree(Root(folders));

		var lines = description.Split('\n');
		var totalNodes = FolderCount + (FolderCount * MaxDescribeChildren);

		lines.Should().HaveCount(MaxDescribeLines + 1, $"{MaxDescribeLines} emitted lines plus the closing marker");
		lines[^1].Should()
			.Be($"... {totalNodes - MaxDescribeLines} node(s) not shown: the dump is capped at {MaxDescribeLines} lines");
	}

	[Fact]
	public void DescribeTree_HiddenMarkersDroppedByTheLineCap_AreCountedInTheTrailer()
	{
		// More leaves than the breadth cap put a marker after every folder, so markers land both
		// before the line cap (emitted) and past it (dropped). The folder count stays under that
		// cap so the root emits no marker of its own, which leaves every hidden node a leaf.
		const int FolderCount = 12;
		const int LeavesPerFolder = MaxDescribeChildren + 5;

		var folders = new OpcUaScadaItem[FolderCount];

		for (var f = 0; f < folders.Length; f++)
		{
			var leaves = new OpcUaScadaItem[LeavesPerFolder];

			for (var l = 0; l < leaves.Length; l++)
			{
				leaves[l] = Leaf($"Tag{f}_{l}");
			}

			folders[f] = Folder("Folder" + f, leaves);
		}

		var description = OpcProtocolAccessor.DescribeTree(Root(folders));

		var lines = description.Split('\n');
		var body = lines.Take(lines.Length - 1).ToArray();
		var emittedMarkers = body.Where(IsHiddenMarker).ToArray();

		emittedMarkers.Should()
			.HaveCountLessThan(FolderCount, "the cap drops the markers of the last folders")
			.And.NotBeEmpty("the folders before the cap keep theirs");

		var totalNodes = FolderCount + (FolderCount * LeavesPerFolder);
		var shownNodes = body.Count(line => !IsHiddenMarker(line));
		var nodesReportedByMarkers = emittedMarkers.Sum(HiddenCountOf);
		var unshownNodes = totalNodes - shownNodes - nodesReportedByMarkers;

		lines[^1].Should()
			.Be($"... {unshownNodes} node(s) not shown: the dump is capped at {MaxDescribeLines} lines");
	}

	[Fact]
	public void DescribeTree_EmptyRoot_ReportsNoNodes()
	{
		var description = OpcProtocolAccessor.DescribeTree(Root());

		description.Should().Be("(no nodes)");
	}

	private static bool IsHiddenMarker(string line)
	{
		return line.TrimStart().StartsWith("...", StringComparison.Ordinal);
	}

	private static int HiddenCountOf(string markerLine)
	{
		return int.Parse(markerLine.Trim().Split(' ')[1], CultureInfo.InvariantCulture);
	}

	private static string Lines(params string[] lines)
	{
		return string.Join(Environment.NewLine, lines);
	}

	private static OpcUaProtocol ProtocolOver(params OpcUaScadaItem[] topLevel)
	{
		return new OpcUaProtocol(null!) { ScadaRootNode = Root(topLevel) };
	}

	private static OpcUaScadaItem Root(params OpcUaScadaItem[] topLevel)
	{
		var root = new OpcUaScadaItem();

		foreach (var item in topLevel)
		{
			root.Items.Add(item);
		}

		return root;
	}

	private static OpcUaScadaItem Folder(string name, params OpcUaScadaItem[] children)
	{
		var folder = new OpcUaScadaItem { Name = name, IsNode = false };

		foreach (var child in children)
		{
			folder.Items.Add(child);
		}

		return folder;
	}

	private static OpcUaScadaItem Leaf(string name)
	{
		return new OpcUaScadaItem { Name = name, IsNode = true };
	}

	private static ITreeItemHlp TreeItemCarrying(object? fbObject)
	{
		return new FakeTreeItem(fbObject);
	}

	// The vendor base getter reads the wrapped COM item, which is null here; overriding it keeps
	// the test double clear of that read.
	private sealed class FakeTreeItem : ITreeItemHlp
	{
		private readonly object? _fbObject;

		internal FakeTreeItem(object? fbObject)
			: base(null!)
		{
			_fbObject = fbObject;
		}

		public override object? FBObject => _fbObject;
	}
}
