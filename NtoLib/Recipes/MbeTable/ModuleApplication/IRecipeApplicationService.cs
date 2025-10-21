using System;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleApplication;

/// <summary>
/// Main application service providing unified API for all recipe operations.
/// Serves as the single entry point for the Presentation layer.
/// </summary>
public interface IRecipeApplicationService
{
    /// <summary>
    /// Gets the RecipeViewModel for presentation layer data access.
    /// </summary>
    RecipeViewModel ViewModel { get; }

    /// <summary>
    /// Raised when recipe structure changes (rows added/removed).
    /// </summary>
    event Action? RecipeStructureChanged;

    /// <summary>
    /// Raised when a specific step data changes.
    /// Parameters: rowIndex.
    /// </summary>
    event Action<int>? StepDataChanged;

    /// <summary>
    /// Raised when recipe validation state changes.
    /// </summary>
    event Action<bool>? ValidationStateChanged;

    /// <summary>
    /// Gets the current recipe entity for direct read access.
    /// </summary>
    /// <returns>Current recipe.</returns>
    Recipe GetCurrentRecipe();

    /// <summary>
    /// Sets a cell value from user input.
    /// </summary>
    /// <param name="rowIndex">Zero-based row index.</param>
    /// <param name="columnKey">Column identifier.</param>
    /// <param name="value">User-provided value.</param>
    /// <returns>Result indicating success or validation errors.</returns>
    Task<Result> SetCellValueAsync(int rowIndex, ColumnIdentifier columnKey, object? value);

    /// <summary>
    /// Adds a new default step at the specified index.
    /// </summary>
    /// <param name="index">Index where to insert the step.</param>
    /// <returns>Result indicating success or error.</returns>
    Result AddStep(int index);

    /// <summary>
    /// Removes a step at the specified index.
    /// </summary>
    /// <param name="index">Index of the step to remove.</param>
    /// <returns>Result indicating success or error.</returns>
    Result RemoveStep(int index);

    /// <summary>
    /// Loads a recipe from file asynchronously.
    /// </summary>
    /// <param name="filePath">Full path to the recipe file.</param>
    /// <returns>Result indicating success or errors.</returns>
    Task<Result> LoadRecipeAsync(string filePath);

    /// <summary>
    /// Saves the current recipe to file asynchronously.
    /// </summary>
    /// <param name="filePath">Full path to the target file.</param>
    /// <returns>Result indicating success or errors.</returns>
    Task<Result> SaveRecipeAsync(string filePath);

    /// <summary>
    /// Sends current recipe to PLC asynchronously.
    /// </summary>
    /// <returns>Result indicating success or errors.</returns>
    Task<Result> SendRecipeAsync();

    /// <summary>
    /// Receives recipe from PLC asynchronously.
    /// </summary>
    /// <returns>Result indicating success or errors.</returns>
    Task<Result> ReceiveRecipeAsync();

    /// <summary>
    /// Gets the current number of rows in the recipe.
    /// </summary>
    /// <returns>Row count.</returns>
    int GetRowCount();
}