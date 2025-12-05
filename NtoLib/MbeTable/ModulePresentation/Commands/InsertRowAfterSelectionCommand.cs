using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.MbeTable.ModuleApplication;
using NtoLib.MbeTable.ModuleApplication.Operations.Contracts;
using NtoLib.MbeTable.ModulePresentation.State;

namespace NtoLib.MbeTable.ModulePresentation.Commands;

/// <summary>
/// Inserts a new row at the specified index.
/// Used as a building block for "insert after selection" logic in the input manager.
/// </summary>
public sealed class InsertRowCommand : CommandBase
{
	private readonly IRecipeApplicationService _applicationService;

	public InsertRowCommand(
		IRecipeApplicationService applicationService,
		IBusyStateManager busyStateManager)
		: base(busyStateManager)
	{
		_applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
	}

	public Task<Result> ExecuteAsync(int insertIndex, CancellationToken cancellationToken = default)
	{
		return ExecuteWithBusyAsync(_ => ExecuteInternalAsync(insertIndex, cancellationToken), cancellationToken);
	}

	protected override OperationKind GetOperationKind() => OperationKind.Other;

	protected override Task<Result> ExecuteInternalAsync(CancellationToken ct)
	{
		// This overload is not used. Use ExecuteAsync(int) instead.
		return Task.FromResult(Result.Ok());
	}

	private Task<Result> ExecuteInternalAsync(int insertIndex, CancellationToken ct)
	{
		var result = _applicationService.AddStep(insertIndex);
		return Task.FromResult(result);
	}
}
