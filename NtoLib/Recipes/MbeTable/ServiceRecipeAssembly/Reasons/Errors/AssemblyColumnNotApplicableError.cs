using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyColumnNotApplicableError : BilingualError
{
	public AssemblyColumnNotApplicableError(string columnCode, string actionName, string value, int? lineNumber = null)
		: base(
			lineNumber.HasValue
				? $"Column '{columnCode}' not applicable for action '{actionName}' but has value '{value}' at line {lineNumber.Value}"
				: $"Column '{columnCode}' not applicable for action '{actionName}' but has value '{value}'",
			lineNumber.HasValue
				? $"Столбец '{columnCode}' не применим для действия '{actionName}', но имеет значение '{value}' на строке {lineNumber.Value}"
				: $"Столбец '{columnCode}' не применим для действия '{actionName}', но имеет значение '{value}'")
	{
		ColumnCode = columnCode;
		ActionName = actionName;
		Value = value;
		LineNumber = lineNumber;

		Metadata["columnCode"] = columnCode;
		Metadata["actionName"] = actionName;
		Metadata["value"] = value;
		if (lineNumber.HasValue)
		{
			Metadata["lineNumber"] = lineNumber.Value;
		}
	}

	public string ColumnCode { get; }
	public string ActionName { get; }
	public string Value { get; }
	public int? LineNumber { get; }
}
