using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleCore.Formulas;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Formulas;

// Precompiles formulas for all actions.
public interface IFormulaPrecompiler
{
    /// <summary>
    /// Precompiles formulas for the provided actions. Only actions with a non-null Formula are processed.
    /// </summary>
    /// <param name="actions">Action definitions keyed by ActionId.</param>
    /// <returns>
    /// Success: a dictionary keyed by ActionId with compiled formulas.
    /// Failure: Result with one or more errors describing compilation failures.
    /// </returns>
    Result<IReadOnlyDictionary<short, CompiledFormula>> Precompile(
        IReadOnlyDictionary<short, ActionDefinition> actions);
}