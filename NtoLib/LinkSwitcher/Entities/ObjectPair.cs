using MasterSCADA.Hlp;

namespace NtoLib.LinkSwitcher.Entities;

public sealed record ObjectPair(
	string Name,
	ITreeItemHlp Source,
	ITreeItemHlp Target);
