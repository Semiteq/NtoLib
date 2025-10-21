using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleApplication.State;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Commands;

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