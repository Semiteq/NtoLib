using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyMissingPlcMappingError : BilingualError
{
	public AssemblyMissingPlcMappingError(string columnName)
		: base(
			$"{columnName} column has no PLC mapping",
			$"Столбец {columnName} не имеет отображения PLC")
	{
		ColumnName = columnName;
		Metadata["columnName"] = columnName;
	}

	public string ColumnName { get; }
}
