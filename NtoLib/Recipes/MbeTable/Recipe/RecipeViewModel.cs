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
        
        public BindingList<StepViewModel> Steps { get; }

        public RecipeViewModel(TableSchema schema, ActionManager actionManager, ComboBoxDataProvider dataProvider)
        {
            _recipeManager = new RecipeManager(schema, actionManager);
            _dataProvider = dataProvider;
            Steps = new BindingList<StepViewModel>();

            _recipeManager.StepAdded += OnStepAdded;
            _recipeManager.StepRemoved += OnStepRemoved;
            _recipeManager.StepPropertyChanged += OnStepPropertyChanged;
        }

        public bool AddNewStep(int rowIndex, out string errorString)
        {
            return _recipeManager.TryAddNewStep(rowIndex, out _, out errorString);
        }

        public bool RemoveStep(int rowIndex, out string errorString)
        {
            return _recipeManager.TryRemoveStep(rowIndex, out errorString);
        }

        private void OnStepAdded(Step step, int index)
        {
            var viewModel = new StepViewModel(step, _recipeManager, index, _dataProvider);
            Steps.Insert(index, viewModel);
            RefreshViewModelIndexes();
        }

        private void OnStepRemoved(int index)
        {
            Steps.RemoveAt(index);
            RefreshViewModelIndexes();
        }

        private void OnStepPropertyChanged(int rowIndex, string propertyName)
        {
            if (rowIndex >= 0 && rowIndex < Steps.Count)
            {
                Steps[rowIndex].RaisePropertyChanged(propertyName);
            }
        }
        
        private void RefreshViewModelIndexes()
        {
            for (int i = 0; i < Steps.Count; i++)
            {
                Steps[i].UpdateRowIndex(i);
            }
        }
    }
}