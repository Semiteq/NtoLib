using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.MbeTable.ModuleApplication.Operations.Handlers;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.AddStep;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.CopySteps;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.CutSteps;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.DeleteSteps;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.EditCell;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Load;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.PasteSteps;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Recive;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Remove;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Save;
using NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Send;
using NtoLib.MbeTable.ModuleApplication.ViewModels;
using NtoLib.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.MbeTable.ModuleCore.Entities;
using NtoLib.MbeTable.ModuleCore.Facade;

namespace NtoLib.MbeTable.ModuleApplication;

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
	private readonly IRecipeOperationHandler<CopyRowsArgs> _copyRows;
	private readonly IRecipeOperationHandler<CutRowsArgs> _cutRows;
	private readonly IRecipeOperationHandler<PasteRowsArgs> _pasteRows;
	private readonly IRecipeOperationHandler<DeleteRowsArgs> _deleteRows;

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
		IRecipeOperationHandler<CopyRowsArgs> copyRows,
		IRecipeOperationHandler<CutRowsArgs> cutRows,
		IRecipeOperationHandler<PasteRowsArgs> pasteRows,
		IRecipeOperationHandler<DeleteRowsArgs> deleteRows,
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
		_copyRows = copyRows ?? throw new ArgumentNullException(nameof(copyRows));
		_cutRows = cutRows ?? throw new ArgumentNullException(nameof(cutRows));
		_pasteRows = pasteRows ?? throw new ArgumentNullException(nameof(pasteRows));
		_deleteRows = deleteRows ?? throw new ArgumentNullException(nameof(deleteRows));

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

	public Task<Result> CopyRowsAsync(IReadOnlyList<int> indices) =>
		_copyRows.ExecuteAsync(new CopyRowsArgs(indices));

	public async Task<Result> CutRowsAsync(IReadOnlyList<int> indices)
	{
		var result = await _cutRows.ExecuteAsync(new CutRowsArgs(indices));
		if (result.IsSuccess)
		{
			RaiseRecipeStructureChanged();
		}

		return result;
	}

	public async Task<Result> PasteRowsAsync(int targetIndex)
	{
		var result = await _pasteRows.ExecuteAsync(new PasteRowsArgs(targetIndex));
		if (result.IsSuccess)
		{
			RaiseRecipeStructureChanged();
		}

		return result;
	}

	public async Task<Result> DeleteRowsAsync(IReadOnlyList<int> indices)
	{
		var result = await _deleteRows.ExecuteAsync(new DeleteRowsArgs(indices));
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
