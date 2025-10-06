using System.Threading;
using System.Threading.Tasks;
using FluentResults;
using NtoLib.Recipes.MbeTable.Application;
using NtoLib.Recipes.MbeTable.Application.State;
using NtoLib.Recipes.MbeTable.Presentation.State;

namespace NtoLib.Recipes.MbeTable.Presentation.Commands;

public sealed class AddStepCommand : CommandBase
{
    private readonly IRecipeApplicationService _app;

    public AddStepCommand(
        IRecipeApplicationService app,
        IBusyStateManager busy)
        : base(busy)
    {
        _app = app;
    }

    public Task<Result> ExecuteAsync(int insertIndex, CancellationToken ct = default)
        => ExecuteWithBusyAsync(_ => Task.FromResult(_app.AddStep(insertIndex)), ct);

    protected override OperationKind GetOperationKind() => OperationKind.None;

    protected override Task<Result> ExecuteInternalAsync(CancellationToken ct)
        => Task.FromResult(Result.Fail("Index is required"));
}