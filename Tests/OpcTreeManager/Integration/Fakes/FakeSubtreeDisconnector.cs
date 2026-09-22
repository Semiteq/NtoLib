using System;
using System.Collections.Generic;

using NtoLib.OpcTreeManager.TreeOperations;

namespace Tests.OpcTreeManager.Integration.Fakes;

/// <summary>
/// Records the paths it was asked to disconnect and returns a per-path tally the test sets up.
/// </summary>
internal sealed class FakeSubtreeDisconnector : ISubtreeDisconnector
{
	private readonly Dictionary<string, (int Issued, int Threw, int NodesMissing)> _resultsBySuffix = new();

	public List<string> RecordedPaths { get; } = new();

	/// <summary>Keyed by path suffix so a test names the node, not the container it hangs under.</summary>
	public FakeSubtreeDisconnector Returns(string pathSuffix, int issued, int threw, int nodesMissing)
	{
		_resultsBySuffix[pathSuffix] = (issued, threw, nodesMissing);
		return this;
	}

	/// <summary>Mirrors the real disconnector's flag point.</summary>
	public (int Issued, int Threw, int NodesMissing) DisconnectSubtree(string nodePath, Action onTreeMutationStarting)
	{
		RecordedPaths.Add(nodePath);

		foreach (var entry in _resultsBySuffix)
		{
			if (!nodePath.EndsWith(entry.Key, StringComparison.Ordinal))
			{
				continue;
			}

			if (entry.Value.Issued > 0 || entry.Value.Threw > 0)
			{
				onTreeMutationStarting();
			}

			return entry.Value;
		}

		return (0, 0, 0);
	}
}
