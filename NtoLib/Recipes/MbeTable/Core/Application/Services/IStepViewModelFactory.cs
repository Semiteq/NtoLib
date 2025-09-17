using System;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.Columns;
using NtoLib.Recipes.MbeTable.Core.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Core.Application.Services;

public interface IStepViewModelFactory
{
    /// <summary>
    /// Creates a new instance of <see cref="StepViewModel"/> based on the provided step data, index, analysis result, and update callback function.
    /// </summary>
    /// <param name="step">The step data used to create the view model.</param>
    /// <param name="index">The index of the step in the sequence.</param>
    /// <param name="analysisResult">The analysis results related to the recipe, used to build the view model.</param>
    /// <param name="updateCallback">A callback function invoked when a step property is updated, providing the step index, column identifier, and updated value.</param>
    /// <returns>A new instance of <see cref="StepViewModel"/> representing the step.</returns>
    StepViewModel Create(Step step, int index, RecipeUpdateResult analysisResult,
        Action<int, ColumnIdentifier, object> updateCallback);
}