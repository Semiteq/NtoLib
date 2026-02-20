using FluentResults;

using NtoLib.LinkSwitcher.Entities;

namespace NtoLib.LinkSwitcher.Facade;

public interface ILinkSwitcherService
{
	bool HasPendingTask { get; }
	SwitchPlan? PendingPlan { get; }
	Result<SwitchPlan> ScanAndValidate(string sourcePath, string targetPath, bool reverse);
	Result Execute(SwitchPlan plan);
	void Cancel();
}
