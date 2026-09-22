using System;
using System.Linq;

using MasterSCADA.Hlp;

using MasterSCADALib;

using NtoLib.OpcTreeManager.Entities;

using Serilog;

namespace NtoLib.OpcTreeManager.TreeOperations;

internal sealed class ProjectSubtreeDisconnector : ISubtreeDisconnector
{
	private readonly IProjectHlp _project;
	private readonly ILogger _logger;

	public ProjectSubtreeDisconnector(IProjectHlp project, ILogger logger)
	{
		_project = project ?? throw new ArgumentNullException(nameof(project));

		if (logger == null)
		{
			throw new ArgumentNullException(nameof(logger));
		}

		_logger = logger;
	}

	public (int Issued, int Threw, int NodesMissing) DisconnectSubtree(string nodePath, Action onTreeMutationStarting)
	{
		return RunDisconnectPass(
			nodePath,
			path => _project.SafeItem<ITreeItemHlp>(path),
			onTreeMutationStarting,
			_logger);
	}

	internal static (int Issued, int Threw, int NodesMissing) RunDisconnectPass(
		string nodePath,
		Func<string, ITreeItemHlp?> resolveNode,
		Action onTreeMutationStarting,
		ILogger logger)
	{
		var node = resolveNode(nodePath);

		if (node == null)
		{
			logger.Error("Remove '{NodePath}': node not found in the project, links not disconnected", nodePath);
			return (0, 0, 1);
		}

		var allPins = node.EnumAllChilds(TreeMasks.AllPinKinds, 0);

		var issued = 0;
		var threw = 0;

		foreach (var child in allPins)
		{
			if (child is not ITreePinHlp localPin)
			{
				continue;
			}

			var (pinIssued, pinThrew) = DisconnectPinConnections(localPin, onTreeMutationStarting, logger);
			issued += pinIssued;
			threw += pinThrew;
		}

		return (issued, threw, 0);
	}

	private static (int Issued, int Threw) DisconnectPinConnections(
		ITreePinHlp localPin,
		Action onTreeMutationStarting,
		ILogger logger)
	{
		var issued = 0;
		var threw = 0;

		foreach (var mask in new[]
		{
			EConnectionTypeMask.ctGenericPin,
			EConnectionTypeMask.ctGenericPout,
			EConnectionTypeMask.ctIConnect,
		})
		{
			// Materialise the COM enumerable before iterating to avoid modifying the
			// collection while enumerating it (COM collections are live views).
			var connections = localPin.GetConnections(mask).Cast<ITreePinHlp>().ToList();

			foreach (var externalPin in connections)
			{
				onTreeMutationStarting();

				try
				{
					localPin.Disconnect(externalPin);
					logger.Debug(
						"Disconnect issued {LocalPin} <-> {ExternalPin} ({LinkType})",
						localPin.FullName,
						externalPin.FullName,
						LinkTypeOf(mask));
					issued++;
				}
				catch (Exception ex)
				{
					logger.Error(
						ex,
						"Disconnect {LocalPin} <-> {ExternalPin} ({LinkType}) threw",
						localPin.FullName,
						externalPin.FullName,
						LinkTypeOf(mask));
					threw++;
				}
			}
		}

		return (issued, threw);
	}

	private static string LinkTypeOf(EConnectionTypeMask mask)
	{
		return mask switch
		{
			EConnectionTypeMask.ctGenericPin => LinkTypes.DirectPin,
			EConnectionTypeMask.ctGenericPout => LinkTypes.DirectPout,
			EConnectionTypeMask.ctIConnect => LinkTypes.IConnect,
			_ => mask.ToString(),
		};
	}
}
