using NtoLib.Recipes.MbeTable.ModuleApplication;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace Tests.MbeTable.Application.Clipboard.Helpers;

public sealed class ClipboardTestDriver
{
	private readonly IRecipeApplicationService _app;

	public ClipboardTestDriver(IRecipeApplicationService app)
	{
		_app = app;
	}

	public ClipboardTestDriver AddWait(int index)
	{
		_app.AddStep(index);
		_app.SetCellValueAsync(index, MandatoryColumns.Action, (short)ServiceActions.Wait).GetAwaiter().GetResult();
		return this;
	}

	public ClipboardTestDriver AddActionStep(int index, short actionId)
	{
		_app.AddStep(index);
		_app.SetCellValueAsync(index, MandatoryColumns.Action, actionId).GetAwaiter().GetResult();
		return this;
	}

	public ClipboardTestDriver SetDuration(int index, float seconds)
	{
		_app.SetCellValueAsync(index, MandatoryColumns.StepDuration, seconds).GetAwaiter().GetResult();
		return this;
	}
}
