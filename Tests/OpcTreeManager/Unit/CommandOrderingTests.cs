using System.Collections.Generic;
using System.Linq;

using FluentAssertions;

using NtoLib.OpcTreeManager.Entities;
using NtoLib.OpcTreeManager.TreeOperations;

using Xunit;

namespace Tests.OpcTreeManager.Unit;

/// <summary>
/// Drives <see cref="CommandOrdering.OrderCommandsDirectFirst"/> — the COM-free replay ordering that
/// puts direct links before iconnect. Ordering is by <see cref="ConnectCommand.LinkType"/> alone, so
/// no live IProjectHlp is needed.
///
/// The COM connect itself (<c>ITreePinHlp.Connect</c>) and the WinForms single-tick timer wiring
/// (<c>DeferredExecutor.Post</c>) are host/pump-bound and are NOT unit-tested here — mocking them
/// would stub away the exact behavior under test.
/// </summary>
public sealed class CommandOrderingTests
{
	[Fact]
	public void OrderCommandsDirectFirst_MixedSet_DirectsBeforeIconnects_StableWithinClass()
	{
		// Interleaved as BuildCommands emits them (a pin's iconnect before its '$' directPin twin).
		var iconnectKp = MakeCommand(LinkTypes.IConnect, "Root.CH17.Kp");
		var directKp = MakeCommand(LinkTypes.DirectPin, "Root.CH17.Kp$");
		var directStatus = MakeCommand(LinkTypes.DirectPin, "Root.CH17.StatusWord");
		var iconnectTi = MakeCommand(LinkTypes.IConnect, "Root.CH17.Ti");
		var directPoutTimeLeft = MakeCommand(LinkTypes.DirectPout, "Root.CH17.TimeLeft");

		var commands = new[] { iconnectKp, directKp, directStatus, iconnectTi, directPoutTimeLeft };

		var ordered = CommandOrdering.OrderCommandsDirectFirst(commands);

		// All directs precede all iconnects, and within each class the original relative order holds:
		// directs [Kp$, StatusWord, TimeLeft], then iconnects [Kp, Ti].
		LocalPaths(ordered).Should().ContainInOrder(
			"Root.CH17.Kp$", "Root.CH17.StatusWord", "Root.CH17.TimeLeft", "Root.CH17.Kp", "Root.CH17.Ti");
	}

	[Fact]
	public void OrderCommandsDirectFirst_AllIconnect_IsUnchanged()
	{
		var commands = new[]
		{
			MakeCommand(LinkTypes.IConnect, "Root.CH17.Kp"),
			MakeCommand(LinkTypes.IConnect, "Root.CH17.Ti"),
			MakeCommand(LinkTypes.IConnect, "Root.CH17.Td"),
		};

		var ordered = CommandOrdering.OrderCommandsDirectFirst(commands);

		LocalPaths(ordered).Should().Equal("Root.CH17.Kp", "Root.CH17.Ti", "Root.CH17.Td");
	}

	[Fact]
	public void OrderCommandsDirectFirst_AllDirect_IsUnchanged()
	{
		var commands = new[]
		{
			MakeCommand(LinkTypes.DirectPin, "Root.CH17.StatusWord"),
			MakeCommand(LinkTypes.DirectPout, "Root.CH17.TimeLeft"),
			MakeCommand(LinkTypes.DirectPin, "Root.CH17.Kp$"),
		};

		var ordered = CommandOrdering.OrderCommandsDirectFirst(commands);

		LocalPaths(ordered).Should().Equal("Root.CH17.StatusWord", "Root.CH17.TimeLeft", "Root.CH17.Kp$");
	}

	private static ConnectCommand MakeCommand(string linkType, string localPinPath)
	{
		return new ConnectCommand(
			LinkType: linkType,
			LocalPinPath: localPinPath,
			ExternalPinPath: "Ext." + localPinPath,
			Connect: () => { });
	}

	private static IEnumerable<string> LocalPaths(IReadOnlyList<ConnectCommand> commands)
	{
		return commands.Select(p => p.LocalPinPath);
	}
}
