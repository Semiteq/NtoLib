using System;
using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.ModuleApplication;

public enum StructureChangeKind
{
	Insert,
	Remove,
	Reset
}

public sealed record StructureChange(
	StructureChangeKind Kind,
	int Index,
	int Count,
	IReadOnlyList<int>? RemovedIndices)
{
	public static StructureChange Insert(int index, int count)
	{
		return new StructureChange(StructureChangeKind.Insert, index, count, null);
	}

	public static StructureChange Remove(IReadOnlyList<int> indices)
	{
		if (indices == null)
		{
			throw new ArgumentNullException(nameof(indices));
		}

		return new StructureChange(StructureChangeKind.Remove, -1, indices.Count, indices);
	}

	public static StructureChange RemoveSingle(int index)
	{
		return Remove(new[] { index });
	}

	public static StructureChange Reset()
	{
		return new StructureChange(StructureChangeKind.Reset, -1, 0, null);
	}
}
