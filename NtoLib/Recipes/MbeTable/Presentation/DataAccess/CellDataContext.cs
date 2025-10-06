using NtoLib.Recipes.MbeTable.Application.ViewModels;
using NtoLib.Recipes.MbeTable.Presentation.DataAccess;

namespace NtoLib.Recipes.MbeTable.Presentation.DataAccess;

/// <summary>
/// Simple adapter that exposes <see cref="RecipeViewModel"/> to presentation strategies.
/// Lifetime – scoped together with RecipeViewModel.
/// </summary>
public sealed class CellDataContext : ICellDataContext
{
    private readonly RecipeViewModel _recipeViewModel;

    public CellDataContext(RecipeViewModel recipeViewModel)
    {
        _recipeViewModel = recipeViewModel;
    }

    public StepViewModel? GetStepViewModel(int rowIndex)
    {
        return rowIndex >= 0 && rowIndex < _recipeViewModel.ViewModels.Count
            ? _recipeViewModel.ViewModels[rowIndex]
            : null;
    }

    public int RowCount => _recipeViewModel.ViewModels.Count;
}