namespace NtoLib.OpcTreeManager.TreeOperations;

internal interface ISubtreeDisconnector
{
	(int Total, int Success, int Fail) DisconnectSubtree(string nodePath);
}
