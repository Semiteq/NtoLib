using System.ComponentModel;
using NtoLib.Recipes.MbeTable.Recipe.Actions;
using NtoLib.Recipes.MbeTable.Recipe.StepManager;
using NtoLib.Recipes.MbeTable.Schema;

namespace NtoLib.Recipes.MbeTable.Recipe
{
    public class RecipeViewModel
    {
        private readonly RecipeManager _recipeManager;
        
        public BindingList<StepViewModel> Steps { get; }

        public RecipeViewModel(TableSchema schema, ActionManager actionManager)
        {
            _recipeManager = new RecipeManager(schema, actionManager);
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
            var viewModel = new StepViewModel(step, _recipeManager, index);
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