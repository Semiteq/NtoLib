using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyInvalidRowCountError : BilingualError
{
	public AssemblyInvalidRowCountError(int rowCount)
		: base(
			$"Invalid row count: {rowCount}",
			$"Недопустимое количество строк: {rowCount}")
	{
		RowCount = rowCount;
		Metadata["rowCount"] = rowCount;
	}

	public int RowCount { get; }
}
