using FluentAssertions;

using NtoLib.OpcTreeManager.Entities;
using NtoLib.OpcTreeManager.TreeOperations;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class LinkCollectorFilterTests
{
	[Fact]
	public void FilterForSubtree_KeepsLinksUnderAnyGivenPath()
	{
		var links = new[]
		{
			Link("Root.Valves.VPG1.StatusWord", "External.A"),
			Link("Root.Valves.VPG2.StatusWord", "External.B"),
			Link("Root.Valves.VPG4.StatusWord", "External.C"),
			Link("Root.Pumps.NR1.StatusWord", "External.D"),
		};

		var kept = new[] { "Root.Valves.VPG1", "Root.Valves.VPG2" };

		var result = LinkCollector.FilterForSubtree(links, kept);

		result.Should().HaveCount(2);
		result.Select(l => l.ExternalPinPath).Should().BeEquivalentTo("External.A", "External.B");
	}

	[Fact]
	public void FilterForSubtree_EmptyKeptPaths_ReturnsEmpty()
	{
		var links = new[] { Link("Root.Valves.VPG1.StatusWord", "External.A") };

		var result = LinkCollector.FilterForSubtree(links, System.Array.Empty<string>());

		result.Should().BeEmpty();
	}

	[Fact]
	public void FilterForSubtree_PrefixMatchRequiresDotSeparator()
	{
		// "Valves1.VPG" must not match prefix "Valves" — prefix check is path-segment aware.
		var links = new[]
		{
			Link("Root.Valves.VPG1.StatusWord", "External.A"),
			Link("Root.Valves1.VPG1.StatusWord", "External.B"),
		};

		var kept = new[] { "Root.Valves" };

		var result = LinkCollector.FilterForSubtree(links, kept);

		result.Should().HaveCount(1);
		result[0].ExternalPinPath.Should().Be("External.A");
	}

	[Fact]
	public void FilterForSubtree_NestedPathsBothMatch_LinkKeptOnce()
	{
		var links = new[] { Link("Root.Valves.VPG1.StatusWord", "External.A") };

		var kept = new[] { "Root.Valves", "Root.Valves.VPG1" };

		var result = LinkCollector.FilterForSubtree(links, kept);

		result.Should().HaveCount(1);
	}

	private static LinkEntry Link(string local, string external)
	{
		return new LinkEntry
		{
			LocalPinPath = local,
			ExternalPinPath = external,
			LinkType = LinkTypes.DirectPin,
		};
	}
}
