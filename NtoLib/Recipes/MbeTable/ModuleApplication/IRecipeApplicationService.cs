using System;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleApplication;

public interface IRecipeApplicationService
{
    RecipeViewModel ViewModel { get; }

    event Action? RecipeStructureChanged;
    event Action<int>? StepDataChanged;

    Recipe GetCurrentRecipe();
    int GetRowCount();

    Task<Result> SetCellValueAsync(int rowIndex, ColumnIdentifier columnKey, object value);
    Result AddStep(int index);
    Result RemoveStep(int index);

    Task<Result> LoadRecipeAsync(string filePath);
    Task<Result> SaveRecipeAsync(string filePath);
    Task<Result> SendRecipeAsync();
    Task<Result> ReceiveRecipeAsync();
}