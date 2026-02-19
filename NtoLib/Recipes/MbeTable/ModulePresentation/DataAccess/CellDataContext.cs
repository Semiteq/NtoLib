using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;

namespace NtoLib.Recipes.MbeTable.ModulePresentation.DataAccess;

public sealed class CellDataContext
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
