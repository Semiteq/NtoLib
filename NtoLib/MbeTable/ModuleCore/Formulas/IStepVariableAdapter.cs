using System.Collections.Generic;

using FluentResults;

using NtoLib.MbeTable.ModuleCore.Entities;

namespace NtoLib.MbeTable.ModuleCore.Formulas;

public interface IStepVariableAdapter
{
	Result<IReadOnlyDictionary<string, double>> ExtractVariables(
		Step step,
		IReadOnlyList<string> variableNames);

	Result<Step> ApplyChanges(
		Step originalStep,
		IReadOnlyDictionary<string, double> variableUpdates);
}
