using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.Application;
using NtoLib.Recipes.MbeTable.Application.State;
using NtoLib.Recipes.MbeTable.Presentation.State;

namespace NtoLib.Recipes.MbeTable.Presentation.Commands;

/// <summary>
/// Sends current recipe to PLC.
/// </summary>
public sealed class SendRecipeCommand : CommandBase
{
    private readonly IRecipeApplicationService _app;

    public SendRecipeCommand(
        IRecipeApplicationService app,
        IBusyStateManager busy)
        : base(busy) => _app = app;

    protected override OperationKind GetOperationKind() => OperationKind.Transferring;

    protected override Task<Result> ExecuteInternalAsync(CancellationToken ct) =>
        _app.SendRecipeAsync();
}