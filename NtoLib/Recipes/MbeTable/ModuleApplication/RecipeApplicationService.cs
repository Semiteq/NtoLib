using System;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.AddStep;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.EditCell;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Load;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Recive;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Remove;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Save;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Send;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;

namespace NtoLib.Recipes.MbeTable.ModuleApplication;

public sealed class RecipeApplicationService : IRecipeApplicationService
{
    private readonly IRecipeFacade _recipeService;

    private readonly IRecipeOperationHandler<EditCellArgs> _editCell;
    private readonly IRecipeOperationHandler<AddStepArgs> _addStep;
    private readonly IRecipeOperationHandler<RemoveStepArgs> _removeStep;
    private readonly IRecipeOperationHandler<LoadRecipeArgs> _load;
    private readonly IRecipeOperationHandler<SaveRecipeArgs> _save;
    private readonly IRecipeOperationHandler<SendRecipeArgs> _send;
    private readonly IRecipeOperationHandler<ReceiveRecipeArgs> _receive;

    public RecipeViewModel ViewModel { get; }

    public event Action? RecipeStructureChanged;
    public event Action<int>? StepDataChanged;

    public RecipeApplicationService(
        IRecipeFacade recipeService,
        IRecipeOperationHandler<EditCellArgs> editCell,
        IRecipeOperationHandler<AddStepArgs> addStep,
        IRecipeOperationHandler<RemoveStepArgs> removeStep,
        IRecipeOperationHandler<LoadRecipeArgs> load,
        IRecipeOperationHandler<SaveRecipeArgs> save,
        IRecipeOperationHandler<SendRecipeArgs> send,
        IRecipeOperationHandler<ReceiveRecipeArgs> receive,
        RecipeViewModel viewModel)
    {
        _recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));

        _editCell = editCell ?? throw new ArgumentNullException(nameof(editCell));
        _addStep = addStep ?? throw new ArgumentNullException(nameof(addStep));
        _removeStep = removeStep ?? throw new ArgumentNullException(nameof(removeStep));
        _load = load ?? throw new ArgumentNullException(nameof(load));
        _save = save ?? throw new ArgumentNullException(nameof(save));
        _send = send ?? throw new ArgumentNullException(nameof(send));
        _receive = receive ?? throw new ArgumentNullException(nameof(receive));

        ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }

    public Recipe GetCurrentRecipe() => _recipeService.CurrentSnapshot.Recipe;

    public int GetRowCount() => _recipeService.CurrentSnapshot.Recipe.Steps.Count;

    public async Task<Result> SetCellValueAsync(int rowIndex, ColumnIdentifier columnKey, object value)
    {
        var result = await _editCell.ExecuteAsync(new EditCellArgs(rowIndex, columnKey, value));
        if (result.IsSuccess)
        {
            RaiseStepDataChanged(rowIndex);
        }

        return result;
    }

    public Result AddStep(int index)
    {
        var result = _addStep.ExecuteAsync(new AddStepArgs(index))
            .GetAwaiter().GetResult();

        if (result.IsSuccess)
        {
            RaiseRecipeStructureChanged();
        }

        return result;
    }

    public Result RemoveStep(int index)
    {
        var result = _removeStep.ExecuteAsync(new RemoveStepArgs(index))
            .GetAwaiter().GetResult();

        if (result.IsSuccess)
        {
            RaiseRecipeStructureChanged();
        }

        return result;
    }

    public async Task<Result> LoadRecipeAsync(string filePath)
    {
        var result = await _load.ExecuteAsync(new LoadRecipeArgs(filePath));
        if (result.IsSuccess)
        {
            RaiseRecipeStructureChanged();
        }

        return result;
    }

    public Task<Result> SaveRecipeAsync(string filePath) =>
        _save.ExecuteAsync(new SaveRecipeArgs(filePath));

    public Task<Result> SendRecipeAsync() =>
        _send.ExecuteAsync(new SendRecipeArgs());

    public async Task<Result> ReceiveRecipeAsync()
    {
        var result = await _receive.ExecuteAsync(new ReceiveRecipeArgs());
        if (result.IsSuccess)
        {
            RaiseRecipeStructureChanged();
        }

        return result;
    }

    private void RaiseRecipeStructureChanged()
    {
        try
        {
            RecipeStructureChanged?.Invoke();
        }
        catch
        {
            /* ignored */
        }
    }

    private void RaiseStepDataChanged(int rowIndex)
    {
        try
        {
            StepDataChanged?.Invoke(rowIndex);
        }
        catch
        {
            /* ignored */
        }
    }
}