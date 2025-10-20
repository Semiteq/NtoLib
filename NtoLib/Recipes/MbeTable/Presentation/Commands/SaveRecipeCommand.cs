﻿using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using FluentResults;

using NtoLib.Recipes.MbeTable.Application;
using NtoLib.Recipes.MbeTable.Application.State;
using NtoLib.Recipes.MbeTable.Presentation.State;

namespace NtoLib.Recipes.MbeTable.Presentation.Commands;

/// <summary>
/// Shows save-dialog and persists current recipe to CSV.
/// </summary>
public sealed class SaveRecipeCommand : CommandBase
{
    private readonly IRecipeApplicationService _app;
    private readonly SaveFileDialog _dialog;

    public SaveRecipeCommand(
        IRecipeApplicationService app,
        SaveFileDialog dialog,
        IBusyStateManager busy)
        : base(busy)
    {
        _app = app;
        _dialog = dialog;
    }

    protected override OperationKind GetOperationKind() => OperationKind.Saving;

    protected override async Task<Result> ExecuteInternalAsync(CancellationToken ct)
    {
        if (_dialog.ShowDialog() != DialogResult.OK) return Result.Ok();
        return await _app.SaveRecipeAsync(_dialog.FileName).ConfigureAwait(false);
    }
}