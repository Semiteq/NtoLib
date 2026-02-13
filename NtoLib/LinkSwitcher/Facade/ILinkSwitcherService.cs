using FluentResults;

using NtoLib.LinkSwitcher.Entities;

namespace NtoLib.LinkSwitcher.Facade;

public interface ILinkSwitcherService
{
	Result<SwitchPlan> ScanAndValidate(string searchPath, bool reverse);
	Result Execute(SwitchPlan plan);
	void Cancel();
	bool HasPendingTask { get; }
	SwitchPlan? PendingPlan { get; }
}
