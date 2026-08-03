using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.OpcTreeManager.Entities;

namespace NtoLib.OpcTreeManager.TreeOperations;

/// <summary>
/// COM-free replay ordering, split out of <see cref="PlanExecutor"/> so the ordering decision is
/// unit-testable without a live <c>IProjectHlp</c> — it orders already-built
/// <see cref="ConnectCommand"/>s by <see cref="ConnectCommand.LinkType"/> alone.
/// </summary>
internal static class CommandOrdering
{
	/// <summary>
	/// Returns a new list with <c>directPin</c>/<c>directPout</c> commands first and <c>iconnect</c>
	/// commands last, preserving the original relative order within each class (stable). Ordering the
	/// direct input of a settings-class pin before its <c>iconnect</c> feedback twin matches the state
	/// manual editing always operates in (direct wired first). The commands arrive in pin-enumeration
	/// order, which reverses that within each pin.
	/// </summary>
	internal static IReadOnlyList<ConnectCommand> OrderCommandsDirectFirst(IReadOnlyList<ConnectCommand> commands)
	{
		if (commands == null)
		{
			throw new ArgumentNullException(nameof(commands));
		}

		// OrderBy is a stable sort in .NET, so commands with equal keys keep their original order —
		// this is what makes the ordering "stable within class".
		return commands.OrderBy(IconnectLast).ToList();
	}

	private static int IconnectLast(ConnectCommand command)
	{
		return string.Equals(command.LinkType, LinkTypes.IConnect, StringComparison.Ordinal) ? 1 : 0;
	}
}
