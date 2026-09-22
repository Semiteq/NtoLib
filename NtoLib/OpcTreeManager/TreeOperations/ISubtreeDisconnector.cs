using System;

namespace NtoLib.OpcTreeManager.TreeOperations;

internal interface ISubtreeDisconnector
{
	/// <summary>A missing node is not a link: it counts into <c>NodesMissing</c> (0 or 1), never into
	/// <c>Threw</c>. Raises <paramref name="onTreeMutationStarting"/> immediately before the first
	/// <c>Disconnect</c> and never earlier; the caller makes the callback once-only.</summary>
	(int Issued, int Threw, int NodesMissing) DisconnectSubtree(string nodePath, Action onTreeMutationStarting);
}
