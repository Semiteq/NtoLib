using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreFormulaVariableNotFoundError : BilingualError
{
	public CoreFormulaVariableNotFoundError(string variableName)
		: base(
			$"Formula variable '{variableName}' not found in step properties",
			$"Переменная формулы '{variableName}' не найдена в свойствах шага")
	{
		VariableName = variableName;
	}

	public string VariableName { get; }
}
