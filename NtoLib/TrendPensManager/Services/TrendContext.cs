using System.Windows.Threading;

using MasterSCADA.Hlp;
using MasterSCADA.Trend.Services;

namespace NtoLib.TrendPensManager.Services;

public sealed record TrendContext(
	TrendService TrendService,
	Dispatcher Dispatcher,
	ITreeItemHlp TreeItem,
	IAttributeHlp Attribute);
