using NtoLib.Recipes.MbeTable.ModuleApplication.Status;

namespace NtoLib.Test.MbeTable.Application.Clipboard.Helpers;

public sealed class FakeStatusPresenter : IStatusPresenter
{
	public string? LastMessage { get; private set; }
	public StatusKind? LastKind { get; private set; }

	public void Clear()
	{
		LastMessage = null;
		LastKind = null;
	}

	public void ShowSuccess(string message)
	{
		LastMessage = message;
		LastKind = StatusKind.Success;
	}

	public void ShowWarning(string message)
	{
		LastMessage = message;
		LastKind = StatusKind.Warning;
	}

	public void ShowError(string message)
	{
		LastMessage = message;
		LastKind = StatusKind.Error;
	}

	public void ShowInfo(string message)
	{
		LastMessage = message;
		LastKind = StatusKind.Info;
	}

	public enum StatusKind
	{
		Success,
		Warning,
		Error,
		Info
	}
}
