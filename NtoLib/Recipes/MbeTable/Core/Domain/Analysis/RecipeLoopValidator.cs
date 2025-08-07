#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using NtoLib.Recipes.MbeTable.Core.Domain.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Core.Domain.Analysis
{
    /// <summary>
    /// Provides validation and analysis of loop structures within a recipe.
    /// This is a stateless service that operates on immutable recipe data.
    /// </summary>
    public class RecipeLoopValidator
    {
        private readonly ActionManager _actionManager;
        private readonly int _maxLoopDepth;

        public RecipeLoopValidator(ActionManager actionManager, int maxLoopDepth = 3)
        {
            _actionManager = actionManager ?? throw new ArgumentNullException(nameof(actionManager));
            _maxLoopDepth = maxLoopDepth;
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
                if (!step.Properties.TryGetValue(ColumnKey.Action, out var actionProperty) || actionProperty == null)
                    continue;

                var actionId = actionProperty.GetValue<int>();

                var actionType = _actionManager.GetActionEntryById(actionId);

                if (actionType == _actionManager.ForLoop)
                {
                    if (currentDepth >= _maxLoopDepth)
                        return new LoopValidationResult($"Exceeded max loop depth of {_maxLoopDepth}.");

                    nestingLevels[i] = currentDepth;
                    currentDepth++;
                }
                else if (actionType == _actionManager.EndForLoop)
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

    /// <summary>
    /// Represents the result of a loop validation operation.
    /// </summary>
    public class LoopValidationResult
    {
        public bool IsValid { get; }
        public string? ErrorMessage { get; }
        public IReadOnlyDictionary<int, int> NestingLevels { get; }

        public LoopValidationResult(IReadOnlyDictionary<int, int> nestingLevels)
        {
            IsValid = true;
            NestingLevels = nestingLevels;
            ErrorMessage = null;
        }

        public LoopValidationResult(string errorMessage)
        {
            IsValid = false;
            ErrorMessage = errorMessage;
            NestingLevels = new Dictionary<int, int>();
        }
    }
}
