using System.Collections.Generic;

using FluentResults;

using MasterSCADA.Hlp;

using NtoLib.OpcTreeManager.Entities;

using Serilog.Core;

namespace NtoLib.OpcTreeManager.Facade;

public interface IOpcTreeManagerService
{
	bool HasPendingTask { get; }
	RebuildPlan? PendingPlan { get; }

	Result ScanAndValidate(
		string targetProject,
		string opcFbPath,
		string groupName,
		string treeJsonPath,
		string configYamlPath);

	/// <summary>
	/// Posts the current <see cref="PendingPlan"/> onto the single-shot deferred
	/// executor, which runs <c>PlanExecutor.Execute</c> once <see cref="IProjectHlp.InRuntime"/>
	/// drops to <c>false</c>. The caller transfers ownership of <paramref name="logger"/> —
	/// the executor disposes it when deferred execution finishes (success, failure, or
	/// timeout). If there is no pending plan, the logger is disposed and the call is a no-op.
	/// </summary>
	void ExecuteDeferred(Logger? logger);

	void Cancel();

	Result<Dictionary<string, NodeSnapshot>> BuildSnapshot(string opcFbPath, string groupName);
	Result CaptureAndWriteSnapshot(string opcFbPath, string groupName, string treeJsonPath);
}
