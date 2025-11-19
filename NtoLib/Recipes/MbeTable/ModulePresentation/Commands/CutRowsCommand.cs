using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;
using NtoLib.Recipes.MbeTable.ModulePresentation.State;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.Commands;

/// <summary>
/// Cuts specified rows: copies them to the clipboard and removes from the recipe.
/// </summary>
public sealed class CutRowsCommand : CommandBase
{
    private readonly IRecipeApplicationService _applicationService;

    public CutRowsCommand(
        IRecipeApplicationService applicationService,
        IBusyStateManager busyStateManager)
        : base(busyStateManager)
    {
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
    }

    public Task<Result> ExecuteAsync(IReadOnlyList<int> rowIndices, CancellationToken cancellationToken = default)
    {
        if (rowIndices == null)
            throw new ArgumentNullException(nameof(rowIndices));

        return ExecuteWithBusyAsync(_ => ExecuteInternalAsync(rowIndices, cancellationToken), cancellationToken);
    }

    protected override OperationKind GetOperationKind() => OperationKind.Other;

    protected override Task<Result> ExecuteInternalAsync(CancellationToken ct)
    {
        // This overload is not used. Use ExecuteAsync(IReadOnlyList<int>) instead.
        return Task.FromResult(Result.Ok());
    }

    private Task<Result> ExecuteInternalAsync(IReadOnlyList<int> rowIndices, CancellationToken ct)
    {
        if (rowIndices.Count == 0)
            return Task.FromResult(Result.Ok());

        return _applicationService.CutRowsAsync(rowIndices);
    }
}