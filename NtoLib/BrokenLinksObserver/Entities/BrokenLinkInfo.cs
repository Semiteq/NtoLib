namespace NtoLib.BrokenLinksObserver.Entities;

public record BrokenLinkInfo(string PinName, string ObjectFullName, BrokenLinkKind LinkKind)
{
	public string ObjectFullName { get; set; } = ObjectFullName;
	public string PinName { get; set; } = PinName;
	public BrokenLinkKind LinkKind { get; set; } = LinkKind;
}
