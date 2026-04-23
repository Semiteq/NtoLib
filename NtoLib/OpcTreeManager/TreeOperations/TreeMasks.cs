using MasterSCADALib;

namespace NtoLib.OpcTreeManager.TreeOperations;

internal static class TreeMasks
{
	public const TreeItemMask AllPinKinds =
		TreeItemMask.Pin | TreeItemMask.Pout | TreeItemMask.Variable;
}
