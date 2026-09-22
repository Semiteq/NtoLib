using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.OpcTreeManager.Entities;

namespace NtoLib.OpcTreeManager.TreeOperations;

/// <summary>
/// Replay ordering, split out of <see cref="PlanExecutor"/> so it is testable without a live
/// <c>IProjectHlp</c>: it orders built <see cref="ConnectCommand"/>s by
/// <see cref="ConnectCommand.LinkType"/> alone.
/// </summary>
internal static class CommandOrdering
{
	/// <summary>
	/// Returns a new list with direct commands first and <c>iconnect</c> last, stable within each
	/// class. A settings pin must have its direct input wired before its feedback twin, the state
	/// manual editing always produces; pin-enumeration order reverses that within each pin.
	/// </summary>
	internal static IReadOnlyList<ConnectCommand> OrderCommandsDirectFirst(IReadOnlyList<ConnectCommand> commands)
	{
		if (commands == null)
		{
			throw new ArgumentNullException(nameof(commands));
		}

		// OrderBy is a stable sort in .NET: equal keys keep their original order.
		return commands.OrderBy(IconnectLast).ToList();
	}

	private static int IconnectLast(ConnectCommand command)
	{
		return string.Equals(command.LinkType, LinkTypes.IConnect, StringComparison.Ordinal) ? 1 : 0;
	}
}
