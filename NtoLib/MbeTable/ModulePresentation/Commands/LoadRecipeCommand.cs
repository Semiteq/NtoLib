using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using FluentResults;

using NtoLib.MbeTable.ModuleApplication;
using NtoLib.MbeTable.ModuleApplication.Operations.Contracts;
using NtoLib.MbeTable.ModulePresentation.Errors;
using NtoLib.MbeTable.ModulePresentation.State;

namespace NtoLib.MbeTable.ModulePresentation.Commands;

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
		_app = app ?? throw new ArgumentNullException(nameof(app));
		_dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
	}

	protected override OperationKind GetOperationKind() => OperationKind.Loading;

	protected override async Task<Result> ExecuteInternalAsync(CancellationToken ct)
	{
		if (_dialog.ShowDialog() != DialogResult.OK)
			return Result.Ok();

		var path = _dialog.FileName;
		if (!File.Exists(path))
			return Result.Fail(new PresentationFileNotFoundError());
		;

		return await _app.LoadRecipeAsync(path).ConfigureAwait(false);
	}
}
