using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;

public sealed class ApplicationIndexOutOfRangeError : BilingualError
{
	public int Index { get; }
	public int Count { get; }

	public ApplicationIndexOutOfRangeError(int index, int count)
		: base(
			$"Index {index} is out of range (total: {count})",
			$"Индекс {index} вне диапазона (всего: {count})")
	{
		Index = index;
		Count = count;
	}
}
