using System;
using System.Collections.Generic;

using Serilog;

namespace NtoLib.OpcTreeManager.TreeOperations;

/// <summary>
/// Invokes every command's <see cref="ConnectCommand.Connect"/> in order; a throwing command is
/// logged at Error and the pass continues. Returns connects issued and how many threw, never a
/// read-back verdict: success is judged by the SCADA tree on reload.
/// </summary>
internal static class ConnectRunner
{
	/// <summary>
	/// Invokes every command's connect in order and returns <c>(Issued, Threw)</c>, which sum to the
	/// command count: <c>Issued</c> counts the calls that returned, <c>Threw</c> the calls that raised,
	/// matching what the per-link <c>Connect issued</c> record claims.
	/// </summary>
	public static (int Issued, int Threw) ConnectAll(IReadOnlyList<ConnectCommand> commands, ILogger logger)
	{
		if (commands == null)
		{
			throw new ArgumentNullException(nameof(commands));
		}

		var issued = 0;
		var threw = 0;

		foreach (var command in commands)
		{
			try
			{
				command.Connect();
				issued++;
				logger.Debug(
					"Connect issued {LocalPin} <-> {ExternalPin} ({LinkType})",
					command.LocalPinPath,
					command.ExternalPinPath,
					command.LinkType);
			}
			catch (Exception ex)
			{
				threw++;
				logger.Error(
					ex,
					"Connect {LocalPin} <-> {ExternalPin} ({LinkType}) threw",
					command.LocalPinPath,
					command.ExternalPinPath,
					command.LinkType);
			}
		}

		return (issued, threw);
	}
}
