using System.Linq;

using FluentAssertions;

using NtoLib.OpcTreeManager.TreeOperations;

using Serilog.Events;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

public sealed class ProjectSubtreeDisconnectorTests
{
	[Fact]
	public void RunDisconnectPass_NodeMissingFromProject_CountsOneMissingNodeAndNoFailedLink()
	{
		var sink = new CapturingSink();

		var (issued, threw, nodesMissing) = ProjectSubtreeDisconnector.RunDisconnectPass(
			"Root.Group.Legacy",
			_ => null,
			() => { },
			sink.ToLogger());

		issued.Should().Be(0);
		threw.Should().Be(0, "no disconnect was attempted, so none could fail");
		nodesMissing.Should().Be(1);
	}

	[Fact]
	public void RunDisconnectPass_NodeMissingFromProject_NeverFlagsAMutation()
	{
		var flagged = false;

		ProjectSubtreeDisconnector.RunDisconnectPass(
			"Root.Group.Legacy",
			_ => null,
			() => flagged = true,
			new CapturingSink().ToLogger());

		flagged.Should().BeFalse();
	}

	[Fact]
	public void RunDisconnectPass_NodeMissingFromProject_NamesTheNodeAtError()
	{
		var sink = new CapturingSink();

		ProjectSubtreeDisconnector.RunDisconnectPass(
			"Root.Group.Legacy",
			_ => null,
			() => { },
			sink.ToLogger());

		var error = sink.Events.Single(e => e.Level == LogEventLevel.Error);

		error.MessageTemplate.Text.Should()
			.Be("Remove '{NodePath}': node not found in the project, links not disconnected");
		((ScalarValue)error.Properties["NodePath"]).Value.Should().Be("Root.Group.Legacy");
	}
}
