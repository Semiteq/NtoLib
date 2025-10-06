using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using NtoLib.Recipes.MbeTable.Application;
using NtoLib.Recipes.MbeTable.Application.State;
using NtoLib.Recipes.MbeTable.Presentation.State;

namespace NtoLib.Recipes.MbeTable.Presentation.Commands;

public sealed class RemoveStepCommand : CommandBase
{
    private readonly IRecipeApplicationService _app;

    public RemoveStepCommand(
        IRecipeApplicationService app,
        IBusyStateManager busy)
        : base(busy)
    {
        _app = app;
    }

    public Task<Result> ExecuteAsync(int rowIndex, CancellationToken ct = default)
        => ExecuteWithBusyAsync(_ => Task.FromResult(_app.RemoveStep(rowIndex)), ct);

    protected override OperationKind GetOperationKind() => OperationKind.None;

    protected override Task<Result> ExecuteInternalAsync(CancellationToken ct)
        => Task.FromResult(Result.Fail("Index is required"));
}