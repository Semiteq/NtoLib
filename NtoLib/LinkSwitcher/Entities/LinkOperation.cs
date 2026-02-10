namespace NtoLib.LinkSwitcher.Entities;

public sealed record LinkOperation(
	string ExternalPinPath,
	string SourcePinPath,
	string TargetPinPath,
	bool IsIncoming,
	bool IsIConnect = false);
