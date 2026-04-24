using System.Collections.Generic;

using NtoLib.OpcTreeManager.TreeOperations;

namespace Tests.OpcTreeManager.Integration.Fakes;

internal sealed class FakeSubtreeDisconnector : ISubtreeDisconnector
{
	public List<string> RecordedPaths { get; } = new();

	public (int Total, int Success, int Fail) DisconnectSubtree(string nodePath)
	{
		RecordedPaths.Add(nodePath);
		return (0, 0, 0);
	}
}
