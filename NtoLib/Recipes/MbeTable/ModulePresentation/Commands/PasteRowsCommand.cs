using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Commands;

/// <summary>
/// Pastes rows from clipboard at the specified target index.
/// </summary>
public sealed class PasteRowsCommand : CommandBase
{
    private readonly IRecipeApplicationService _applicationService;

    public PasteRowsCommand(
        IRecipeApplicationService applicationService,
        IBusyStateManager busyStateManager)
        : base(busyStateManager)
    {
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
    }

    public Task<Result> ExecuteAsync(int targetIndex, CancellationToken cancellationToken = default)
    {
        return ExecuteWithBusyAsync(_ => ExecuteInternalAsync(targetIndex, cancellationToken), cancellationToken);
    }

    protected override OperationKind GetOperationKind() => OperationKind.Other;

    protected override Task<Result> ExecuteInternalAsync(CancellationToken ct)
    {
        // This overload is not used. Use ExecuteAsync(int) instead.
        return Task.FromResult(Result.Ok());
    }

    private Task<Result> ExecuteInternalAsync(int targetIndex, CancellationToken ct)
    {
        return _applicationService.PasteRowsAsync(targetIndex);
    }
}