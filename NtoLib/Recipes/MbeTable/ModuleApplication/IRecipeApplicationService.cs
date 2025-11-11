using System;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;

namespace NtoLib.Recipes.MbeTable.ModuleApplication;

// Application service entry point for the presentation layer.
public interface IRecipeApplicationService
{
    RecipeViewModel ViewModel { get; }

    // Raised when rows are added or removed.
    event Action? RecipeStructureChanged;

    // Raised when a specific row changes; arg = row index.
    event Action<int>? StepDataChanged;

    Task<Result> SetCellValueAsync(int rowIndex, ColumnIdentifier columnKey, object value);

    Result AddStep(int index);

    Result RemoveStep(int index);

    Task<Result> LoadRecipeAsync(string filePath);

    Task<Result> SaveRecipeAsync(string filePath);

    Task<Result> SendRecipeAsync();

    Task<Result> ReceiveRecipeAsync();

    int GetRowCount();
}