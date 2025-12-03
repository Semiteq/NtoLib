using System.Collections.Generic;

using MasterSCADA.Hlp;
using MasterSCADA.Script.FB;

using MasterSCADALib;

using NtoLib.BrokenLinksObserver.Entities;

namespace NtoLib.BrokenLinksObserver.Facade;

public class BrokenLinksService
{
	private readonly List<BrokenLinkInfo> _brokenLinks = new();

	public IList<BrokenLinkInfo> BrokenLinks
	{
		get { return _brokenLinks.AsReadOnly(); }
	}

	public void Refresh()
	{
		_brokenLinks.Clear();

		var scriptBase = new ScriptBase();
		var hostFb = scriptBase.HostFB;

		var hostItem = hostFb?.TreeItemHlp;

		var rootObjectItem = hostItem?.Project?.ObjectTreeRootItem;
		if (rootObjectItem == null)
		{
			return;
		}

		var navigateDelegate = new ITreeObjectHlp.TreeObjectDelegate(NavigateCallback);

		rootObjectItem.NavigateChilds(
			navigateDelegate,
			TreeItemMask.Parser | TreeItemMask.Event,
			NavigateItemsFlags.IncludeCurrentItem);
	}

	private bool NavigateCallback(ITreeObjectHlp treeObject)
	{
		if (treeObject is ParserHlp parser)
		{
			ProcessParser(parser);
		}

		if (treeObject is EventHlp eventHelper)
		{
			ProcessEvent(eventHelper);
		}

		return true;
	}

	private void ProcessParser(ParserHlp parser)
	{
		IEnumerable<ITreePinHlp> inputs = parser.Inputs;
		if (inputs == null)
		{
			return;
		}

		foreach (var pin in inputs)
		{
			ProcessPin(
				parser.FullName,
				pin,
				BrokenLinkKind.ParserInput);
		}
	}

	private void ProcessEvent(EventHlp eventHelper)
	{
		IEnumerable<ITreePinHlp> inputs = eventHelper.PatternInputs;
		if (inputs == null)
		{
			return;
		}

		foreach (var pin in inputs)
		{
			ProcessPin(
				eventHelper.FullName,
				pin,
				BrokenLinkKind.EventPatternInput);
		}
	}

	private void ProcessPin(string ownerFullName, ITreePinHlp pin, BrokenLinkKind linkKind)
	{
		if (string.IsNullOrEmpty(ownerFullName))
		{
			return;
		}

		ITreePinHlp[] connections = pin.GetConnections(EConnectionTypeMask.ctGeneric);
		if (connections != null && connections.Length > 0)
		{
			return;
		}

		var info = new BrokenLinkInfo(ownerFullName, pin.Name, linkKind);

		_brokenLinks.Add(info);
	}
}
