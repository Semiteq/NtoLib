using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.State;

/// <summary>
/// Index-shift arithmetic for the defaulted-cell mark store: re-keys row-indexed marks
/// after structural recipe changes. Callers own the synchronization of the store.
/// </summary>
internal static class MarkIndexShifter
{
	private const int RowDropped = -1;

	public static bool ShiftForInsert(Dictionary<int, HashSet<int>> marks, int index, int count)
	{
		return Rebuild(marks, row => row >= index ? row + count : row);
	}

	public static bool ShiftForRemove(Dictionary<int, HashSet<int>> marks, IReadOnlyList<int> removedIndices)
	{
		var removed = new HashSet<int>(removedIndices);

		return Rebuild(marks, row => removed.Contains(row) ? RowDropped : row - CountBelow(removedIndices, row));
	}

	private static int CountBelow(IReadOnlyList<int> indices, int row)
	{
		var count = 0;
		for (var i = 0; i < indices.Count; i++)
		{
			if (indices[i] < row)
			{
				count++;
			}
		}

		return count;
	}

	/// <summary>
	/// Re-keys every mark through <paramref name="mapRow"/>. A return of <see cref="RowDropped"/>
	/// removes the mark entirely. Returns whether any row index actually changed.
	/// </summary>
	private static bool Rebuild(Dictionary<int, HashSet<int>> marks, Func<int, int> mapRow)
	{
		var shifted = new Dictionary<int, HashSet<int>>();
		var anyIndexShifted = false;
		foreach (var entry in marks)
		{
			var newRow = mapRow(entry.Key);
			if (newRow == RowDropped)
			{
				anyIndexShifted = true;

				continue;
			}

			anyIndexShifted |= newRow != entry.Key;
			shifted[newRow] = entry.Value;
		}

		marks.Clear();
		foreach (var entry in shifted)
		{
			marks[entry.Key] = entry.Value;
		}

		return anyIndexShifted;
	}
}
