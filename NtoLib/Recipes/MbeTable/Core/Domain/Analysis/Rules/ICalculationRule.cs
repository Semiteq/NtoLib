#nullable enable

using NtoLib.Recipes.MbeTable.Composition;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Config.Models.Schema;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties;
using NtoLib.Recipes.MbeTable.Core.Domain.Properties.Errors;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis.Rules;

/// <summary>
/// Defines a strategy for calculating dependent properties within a recipe step.
/// </summary>
public interface ICalculationRule
{
    /// <summary>
    /// Gets the unique name of the rule, used to identify it in the configuration.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Applies the calculation rule to a step after one of its properties has changed.
    /// </summary>
    /// <param name="currentStep">The current state of the step before changes.</param>
    /// <param name="triggerKey">The identifier of the column that was just changed by the user.</param>
    /// <param name="newTriggerProperty">The new property value for the trigger column.</param>
    /// <param name="mapping">The mapping of rule parameters to column keys for this specific action.</param>
    /// <returns>A tuple containing the updated <see cref="Step"/> and an optional <see cref="RecipePropertyError"/> if calculation fails.</returns>
    (Step NewStep, RecipePropertyError? Error) Apply(
        Step currentStep,
        ColumnIdentifier triggerKey,
        StepProperty newTriggerProperty,
        CalculationRuleMapping mapping);
}