using System;
using System.Linq;

using MasterSCADA.Hlp;

using MasterSCADALib;

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

		_logger = logger.ForContext<ProjectSubtreeDisconnector>();
	}

	public (int Total, int Success, int Fail) DisconnectSubtree(string nodePath)
	{
		var node = _project.SafeItem<ITreeItemHlp>(nodePath);

		if (node == null)
		{
			_logger.Error("Disconnect — node not found: {NodePath}", nodePath);
			return (1, 0, 1);
		}

		var allPins = node.EnumAllChilds(TreeMasks.AllPinKinds, 0);

		var success = 0;
		var fail = 0;

		foreach (var child in allPins)
		{
			if (child is not ITreePinHlp localPin)
			{
				continue;
			}

			var (s, f) = DisconnectPinConnections(localPin);
			success += s;
			fail += f;
		}

		return (success + fail, success, fail);
	}

	private (int Success, int Fail) DisconnectPinConnections(ITreePinHlp localPin)
	{
		var success = 0;
		var fail = 0;

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
				try
				{
					localPin.Disconnect(externalPin);
					_logger.Debug(
						"Disconnected {LocalPin} ← {ExternalPin}",
						localPin.FullName,
						externalPin.FullName);
					success++;
				}
				catch (Exception ex)
				{
					_logger.Error(
						"Disconnect {LocalPin} ← {ExternalPin} — {Message}",
						localPin.FullName,
						externalPin.FullName,
						ex.Message);
					fail++;
				}
			}
		}

		return (success, fail);
	}
}
