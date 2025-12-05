using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyMandatoryColumnMissingError : BilingualError
{
	public string ColumnName { get; }

	public AssemblyMandatoryColumnMissingError(string columnName)
		: base(
			$"{columnName} column not found in configuration",
			$"Столбец {columnName} не найден в конфигурации")
	{
		ColumnName = columnName;
		Metadata["columnName"] = columnName;
	}
}
