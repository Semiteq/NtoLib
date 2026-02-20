using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreFormulaVariableNonNumericError : BilingualError
{
	public CoreFormulaVariableNonNumericError(string variableName)
		: base(
			$"Formula variable '{variableName}' has a non-numeric type",
			$"Переменная формулы '{variableName}' имеет нечисловой тип")
	{
		VariableName = variableName;
	}

	public string VariableName { get; }
}
