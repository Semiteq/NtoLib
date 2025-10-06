using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using FluentResults;

using NtoLib.Recipes.MbeTable.Application;
using NtoLib.Recipes.MbeTable.Application.State;
using NtoLib.Recipes.MbeTable.Presentation.State;

namespace NtoLib.Recipes.MbeTable.Presentation.Commands;

/// <summary>
/// Opens file-dialog and loads recipe from CSV.
/// </summary>
public sealed class LoadRecipeCommand : CommandBase
{
    private readonly IRecipeApplicationService _app;
    private readonly OpenFileDialog _dialog;

    public LoadRecipeCommand(
        IRecipeApplicationService app,
        OpenFileDialog dialog,
        IBusyStateManager busy)
        : base(busy)
    {
        _app = app ?? throw new System.ArgumentNullException(nameof(app));
        _dialog = dialog ?? throw new System.ArgumentNullException(nameof(dialog));
    }

    protected override OperationKind GetOperationKind() => OperationKind.Loading;

    protected override async Task<Result> ExecuteInternalAsync(CancellationToken ct)
    {
        if (_dialog.ShowDialog() != DialogResult.OK) return Result.Ok();

        var path = _dialog.FileName;
        if (!File.Exists(path)) return Result.Fail("File not found");

        return await _app.LoadRecipeAsync(path).ConfigureAwait(false);
    }
}