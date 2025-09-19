#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Config;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Infrastructure.Logging;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis;

/// <summary>
/// Provides validation and analysis of loop structures within a recipe.
/// This is a stateless service that operates on immutable recipe data.
/// </summary>
public class RecipeLoopValidator : IRecipeLoopValidator
{
    private const int ForLoopActionId = 20;
    private const int EndForLoopActionId = 30;

    private readonly ILogger _debugLogger;
    private const int MaxLoopDepth = 3;

    public RecipeLoopValidator(ILogger debugLogger)
    {
        _debugLogger = debugLogger ?? throw new ArgumentNullException(nameof(debugLogger));
    }

    /// <summary>
    /// Calculates the nesting level for each step and validates the overall loop structure.
    /// </summary>
    /// <param name="recipe">The recipe to analyze.</param>
    /// <returns>A result object containing nesting levels or a validation error.</returns>
    public LoopValidationResult Validate(Recipe recipe)
    {
        var nestingLevels = new Dictionary<int, int>();
        var currentDepth = 0;

        for (var i = 0; i < recipe.Steps.Count; i++)
        {
            var step = recipe.Steps[i];
            if (!step.Properties.TryGetValue(WellKnownColumns.Action, out var actionProperty) || actionProperty == null)
            {
                var ex = new InvalidOperationException($"Step {i} does not have an action property.");
                _debugLogger.LogException(ex);
                throw ex;
            }

            var actionId = actionProperty.GetValue<int>();

            if (actionId == ForLoopActionId)
            {
                if (currentDepth >= MaxLoopDepth)
                    return new LoopValidationResult($"Exceeded max loop depth of {MaxLoopDepth}.");

                nestingLevels[i] = currentDepth;
                currentDepth++;
            }
            else if (actionId == EndForLoopActionId)
            {
                currentDepth--;
                if (currentDepth < 0)
                    return new LoopValidationResult("Unmatched 'EndForLoop' found.");

                nestingLevels[i] = currentDepth;
            }
            else
            {
                nestingLevels[i] = currentDepth;
            }
        }

        if (currentDepth != 0)
            return new LoopValidationResult("Unmatched 'ForLoop' found, loop was not closed.");

        return new LoopValidationResult(nestingLevels.ToImmutableDictionary());
    }
}