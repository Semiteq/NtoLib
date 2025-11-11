using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Errors;

public sealed class CoreFormulaVariableNotFoundError : BilingualError
{
    public string VariableName { get; }

    public CoreFormulaVariableNotFoundError(string variableName)
        : base(
            $"Formula variable '{variableName}' not found in step properties",
            $"Переменная формулы '{variableName}' не найдена в свойствах шага")
    {
        VariableName = variableName;
    }
}