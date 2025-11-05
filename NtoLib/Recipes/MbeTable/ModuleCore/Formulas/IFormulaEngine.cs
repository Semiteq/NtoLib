using System.Collections.Generic;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

public interface IFormulaEngine
{
    Result<IReadOnlyDictionary<string, double>> Calculate(
        short actionId,
        string changedVariable,
        IReadOnlyDictionary<string, double> currentValues);
}