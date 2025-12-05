using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.MbeTable.ModuleApplication;
using NtoLib.MbeTable.ModuleApplication.Operations.Contracts;
using NtoLib.MbeTable.ModulePresentation.State;

namespace NtoLib.MbeTable.ModulePresentation.Commands;

public sealed class ReceiveRecipeCommand : CommandBase
{
	private readonly IRecipeApplicationService _app;

	public ReceiveRecipeCommand(
		IRecipeApplicationService app,
		IBusyStateManager busy)
		: base(busy)
	{
		_app = app;
	}

	protected override OperationKind GetOperationKind() => OperationKind.Transferring;

	protected override Task<Result> ExecuteInternalAsync(CancellationToken ct)
		=> _app.ReceiveRecipeAsync();
}
