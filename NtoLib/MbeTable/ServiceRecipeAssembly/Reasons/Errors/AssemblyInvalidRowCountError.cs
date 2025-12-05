using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyInvalidRowCountError : BilingualError
{
	public int RowCount { get; }

	public AssemblyInvalidRowCountError(int rowCount)
		: base(
			$"Invalid row count: {rowCount}",
			$"Недопустимое количество строк: {rowCount}")
	{
		RowCount = rowCount;
		Metadata["rowCount"] = rowCount;
	}
}
