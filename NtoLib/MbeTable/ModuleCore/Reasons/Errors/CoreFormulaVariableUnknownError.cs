using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ModuleCore.Reasons.Errors;

public sealed class CoreFormulaVariableUnknownError : BilingualError
{
	public string VariableName { get; }

	public CoreFormulaVariableUnknownError(string variableName)
		: base(
			$"Variable '{variableName}' is not known in formula",
			$"Переменная '{variableName}' не известна в формуле")
	{
		VariableName = variableName;
	}
}
