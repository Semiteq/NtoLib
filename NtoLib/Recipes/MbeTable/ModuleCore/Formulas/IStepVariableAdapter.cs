using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

public interface IStepVariableAdapter
{
    Result<IReadOnlyDictionary<string, double>> ExtractVariables(
        Step step,
        IReadOnlyList<string> variableNames);

    Result<Step> ApplyChanges(
        Step originalStep,
        IReadOnlyDictionary<string, double> variableUpdates);
}