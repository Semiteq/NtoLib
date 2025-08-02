using System.ComponentModel;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;
using NtoLib.Recipes.MbeTable.Table;

namespace NtoLib.Recipes.MbeTable.Recipe
{
    public class RecipeViewModel
    {
        /// <summary>
        /// Part of the MVVM pattern.
        /// Acts as a Facade between the UI (View) and the business logic (Model).
        /// It exposes a simplified interface for UI interactions, like adding
        /// or removing steps, and manages the collection of StepViewModels for data binding.
        /// This class hides the complexity of the RecipeManager
        /// and ensures the View remains decoupled from the core logic.
        /// </summary>
        
        private readonly ComboBoxDataProvider _dataProvider;
        private readonly RecipeManager _recipeManager;
        
        public BindingList<StepViewModel> ViewModels { get; }

        public RecipeViewModel(RecipeManager recipeManager, ComboBoxDataProvider dataProvider)
        {
            _recipeManager = recipeManager;
            _dataProvider = dataProvider;
            ViewModels = new BindingList<StepViewModel>();

            _recipeManager.StepAdded += OnStepAdded;
            _recipeManager.StepRemoved += OnStepRemoved;
            _recipeManager.StepPropertyChanged += OnStepPropertyChanged;
        }

        public bool AddNewStep(int rowIndex, out string errorString)
        {
            return _recipeManager.TryAddDefaultStep(rowIndex, out _, out errorString);
        }

        public bool RemoveStep(int rowIndex, out string errorString)
        {
            return _recipeManager.TryRemoveStep(rowIndex, out errorString);
        }

        private void OnStepAdded(IReadOnlyStep readOnlyStep, int index)
        {
            var viewModel = new StepViewModel(readOnlyStep, _recipeManager, index, _dataProvider);
            ViewModels.Insert(index, viewModel);
            RefreshViewModelIndexes();
        }

        private void OnStepRemoved(int index)
        {
            ViewModels.RemoveAt(index);
            RefreshViewModelIndexes();
        }

        private void OnStepPropertyChanged(int rowIndex, ColumnKey key)
        {
            if (rowIndex >= 0 && rowIndex < ViewModels.Count)
            {
                ViewModels[rowIndex].RaisePropertyChanged(key.ToString());
            }
        }
        
        private void RefreshViewModelIndexes()
        {
            for (int i = 0; i < ViewModels.Count; i++)
            {
                ViewModels[i].UpdateRowIndex(i);
            }
        }
    }
}