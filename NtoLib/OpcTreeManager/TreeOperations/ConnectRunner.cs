using System;
using System.Collections.Generic;

using Serilog;

namespace NtoLib.OpcTreeManager.TreeOperations;

/// <summary>
/// Connect-all pass: invoke every command's <see cref="ConnectCommand.Connect"/> in order, tolerating
/// a throwing command (caught, counted, and the pass continues to the rest). Returns an honest tally
/// of connects issued and how many threw — no read-back verdict; success is judged by the SCADA tree
/// on reload.
/// </summary>
internal static class ConnectRunner
{
	/// <summary>
	/// Invokes every command's connect in order and returns <c>(Issued, Threw)</c>: <c>Issued</c> is the
	/// command count (every command is attempted), <c>Threw</c> is how many raised. A throw is caught,
	/// logged at Warning, and the pass continues to the remaining commands.
	/// </summary>
	public static (int Issued, int Threw) ConnectAll(IReadOnlyList<ConnectCommand> commands, ILogger logger)
	{
		if (commands == null)
		{
			throw new ArgumentNullException(nameof(commands));
		}

		var threw = 0;

		foreach (var command in commands)
		{
			try
			{
				command.Connect();
				logger.Debug(
					"Connect issued {LocalPin} ↔ {ExternalPin} ({LinkType})",
					command.LocalPinPath,
					command.ExternalPinPath,
					command.LinkType);
			}
			catch (Exception ex)
			{
				threw++;
				logger.Warning(
					"Connect {LocalPin} ↔ {ExternalPin} ({LinkType}) threw during connect — {Message}",
					command.LocalPinPath,
					command.ExternalPinPath,
					command.LinkType,
					ex.Message);
			}
		}

		return (commands.Count, threw);
	}
}
