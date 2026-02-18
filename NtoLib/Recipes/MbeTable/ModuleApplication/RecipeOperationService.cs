using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Contracts;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Csv;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Modbus;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleApplication.Reasons.Errors;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Recipes.MbeTable.ModuleCore.Snapshot;
using NtoLib.Recipes.MbeTable.ServiceClipboard;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Assembly;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;

namespace NtoLib.Recipes.MbeTable.ModuleApplication;

public sealed class RecipeOperationService : IRecipeApplicationService
{
	private readonly IRecipeFacade _recipeFacade;
	private readonly ITimerService _timer;
	private readonly OperationPipelineRunner _pipeline;
	private readonly ICsvService _csv;
	private readonly IModbusTcpService _modbus;
	private readonly IClipboardService _clipboard;
	private readonly IClipboardSchemaDescriptor _schema;
	private readonly IClipboardAssemblyService _clipboardAssembly;
	private readonly ILogger<RecipeOperationService> _logger;

	public RecipeViewModel ViewModel { get; }

	public event Action? RecipeStructureChanged;
	public event Action<int>? StepDataChanged;

	public RecipeOperationService(
		IRecipeFacade recipeFacade,
		ITimerService timer,
		OperationPipelineRunner pipeline,
		ICsvService csv,
		IModbusTcpService modbus,
		IClipboardService clipboard,
		IClipboardSchemaDescriptor schema,
		IClipboardAssemblyService clipboardAssembly,
		RecipeViewModel viewModel,
		ILogger<RecipeOperationService> logger)
	{
		_recipeFacade = recipeFacade ?? throw new ArgumentNullException(nameof(recipeFacade));
		_timer = timer ?? throw new ArgumentNullException(nameof(timer));
		_pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
		_csv = csv ?? throw new ArgumentNullException(nameof(csv));
		_modbus = modbus ?? throw new ArgumentNullException(nameof(modbus));
		_clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
		_schema = schema ?? throw new ArgumentNullException(nameof(schema));
		_clipboardAssembly = clipboardAssembly ?? throw new ArgumentNullException(nameof(clipboardAssembly));
		ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public int GetRowCount() => _recipeFacade.CurrentSnapshot.Recipe.Steps.Count;

	public async Task<Result> SetCellValueAsync(int rowIndex, ColumnIdentifier columnKey, object value)
	{
		var result = await _pipeline.RunAsync(
			OperationMetadata.EditCell,
			() => PerformEditCellAsync(rowIndex, columnKey, value),
			successMessage: null);

		if (result.IsSuccess)
		{
			_timer.Reset();
			ViewModel.OnTimeRecalculated(rowIndex);
			RaiseStepDataChanged(rowIndex);
		}

		return result;
	}

	private Task<Result<RecipeAnalysisSnapshot>> PerformEditCellAsync(
		int rowIndex, ColumnIdentifier columnKey, object value)
	{
		var stepCount = _recipeFacade.CurrentSnapshot.StepCount;
		if (rowIndex < 0 || rowIndex >= stepCount)
		{
			_logger.LogWarning("EditCell validation failed: rowIndex={RowIndex}", rowIndex);
			return Task.FromResult(
				Result.Fail<RecipeAnalysisSnapshot>(new ApplicationInvalidRowIndexError(rowIndex)));
		}

		var applyResult = columnKey == MandatoryColumns.Action && value is short actionId
			? _recipeFacade.ReplaceAction(rowIndex, actionId)
			: _recipeFacade.UpdateProperty(rowIndex, columnKey, value);

		return Task.FromResult(applyResult);
	}

	public Result AddStep(int index)
	{
		var result = _pipeline.RunSync(
			OperationMetadata.AddStep,
			() => _recipeFacade.AddStep(index),
			successMessage: $"Добавлена строка №{index + 1}");

		if (result.IsSuccess)
		{
			NotifyStructureChanged();
		}

		return result;
	}

	public Result RemoveStep(int index)
	{
		var result = _pipeline.RunSync(
			OperationMetadata.RemoveStep,
			() => _recipeFacade.RemoveStep(index),
			successMessage: $"Удалена строка №{index + 1}");

		if (result.IsSuccess)
		{
			NotifyStructureChanged();
		}

		return result;
	}

	public async Task<Result> LoadRecipeAsync(string filePath)
	{
		var result = await _pipeline.RunAsync(
			OperationMetadata.Load,
			() => PerformLoadAsync(filePath),
			successMessage: $"Загружен рецепт из {Path.GetFileName(filePath)}");

		if (result.IsSuccess)
		{
			NotifyStructureChanged();
		}

		return result;
	}

	private async Task<Result<RecipeAnalysisSnapshot>> PerformLoadAsync(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return new ApplicationFilePathEmptyError();
		}

		try
		{
			var loadResult = await _csv.ReadCsvAsync(filePath).ConfigureAwait(false);
			if (loadResult.IsFailed)
			{
				return loadResult.ToResult<RecipeAnalysisSnapshot>();
			}

			var setResult = _recipeFacade.LoadRecipe(loadResult.Value);
			if (setResult.IsFailed)
			{
				return setResult;
			}

			return setResult.WithReasons(loadResult.Reasons);
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Unexpected error during load operation");
			return Result.Fail<RecipeAnalysisSnapshot>(new ApplicationUnexpectedIoReadError());
		}
	}

	public async Task<Result> SaveRecipeAsync(string filePath)
	{
		return await _pipeline.RunAsync(
			OperationMetadata.Save,
			() => PerformSaveAsync(filePath),
			successMessage: $"Рецепт сохранен в {Path.GetFileName(filePath)}");
	}

	private async Task<Result> PerformSaveAsync(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			return new ApplicationFilePathEmptyError();
		}

		try
		{
			var currentRecipe = _recipeFacade.LastValidSnapshot!.Recipe;
			return await _csv.WriteCsvAsync(currentRecipe, filePath).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Unexpected error during save operation");
			return Result.Fail(new ApplicationUnexpectedIoWriteError());
		}
	}

	public async Task<Result> SendRecipeAsync()
	{
		return await _pipeline.RunAsync(
			OperationMetadata.Send,
			() =>
			{
				var current = _recipeFacade.CurrentSnapshot.Recipe;
				return _modbus.SendRecipeAsync(current);
			},
			successMessage: "Рецепт успешно отправлен в контроллер");
	}

	public async Task<Result> ReceiveRecipeAsync()
	{
		var result = await _pipeline.RunAsync(
			OperationMetadata.Receive,
			PerformReceiveAsync,
			successMessage: "Рецепт успешно прочитан из контроллера");

		if (result.IsSuccess)
		{
			NotifyStructureChanged();
		}

		return result;
	}

	private async Task<Result<RecipeAnalysisSnapshot>> PerformReceiveAsync()
	{
		try
		{
			var receiveResult = await _modbus.ReceiveRecipeAsync().ConfigureAwait(false);
			if (receiveResult.IsFailed || receiveResult.Value == null)
			{
				return receiveResult.ToResult<RecipeAnalysisSnapshot>();
			}

			var setResult = _recipeFacade.LoadRecipe(receiveResult.Value);
			if (setResult.IsFailed)
			{
				return setResult;
			}

			return setResult.WithReasons(receiveResult.Reasons);
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Unexpected error during receive operation");
			return Result.Fail<RecipeAnalysisSnapshot>(new ApplicationUnexpectedIoReadError());
		}
	}

	public Task<Result> CopyRowsAsync(IReadOnlyList<int> indices)
	{
		return _pipeline.RunAsync(
			OperationMetadata.CopyRows,
			() => Task.FromResult(PerformCopy(indices)),
			successMessage: indices.Count == 0 ? null : $"Скопировано {indices.Count} строк");
	}

	private Result PerformCopy(IReadOnlyList<int> indices)
	{
		var recipe = _recipeFacade.CurrentSnapshot.Recipe;
		var valid = indices.Where(i => i >= 0 && i < recipe.Steps.Count)
			.Distinct().OrderBy(i => i).ToList();

		if (valid.Count == 0)
		{
			return Result.Ok();
		}

		var steps = valid.Select(i => recipe.Steps[i]).ToList();
		return _clipboard.WriteSteps(steps, _schema.TransferColumns);
	}

	public async Task<Result> CutRowsAsync(IReadOnlyList<int> indices)
	{
		if (indices.Count == 0)
		{
			return Result.Ok();
		}

		var result = await _pipeline.RunAsync(
			OperationMetadata.CutRows,
			() => Task.FromResult(PerformCut(indices)),
			successMessage: $"Вырезано {indices.Count} строк");

		if (result.IsSuccess)
		{
			NotifyStructureChanged();
		}

		return result;
	}

	private Result<RecipeAnalysisSnapshot> PerformCut(IReadOnlyList<int> indices)
	{
		var recipe = _recipeFacade.CurrentSnapshot.Recipe;
		var valid = indices.Where(i => i >= 0 && i < recipe.Steps.Count)
			.Distinct().OrderBy(i => i).ToList();

		if (valid.Count == 0)
		{
			return Result.Ok(_recipeFacade.CurrentSnapshot);
		}

		var steps = valid.Select(i => recipe.Steps[i]).ToList();
		var writeResult = _clipboard.WriteSteps(steps, _schema.TransferColumns);
		if (writeResult.IsFailed)
		{
			return writeResult.ToResult<RecipeAnalysisSnapshot>();
		}

		var deleteResult = _recipeFacade.DeleteSteps(valid);
		if (deleteResult.IsFailed)
		{
			return deleteResult;
		}

		return deleteResult.WithReasons(writeResult.Reasons);
	}

	public async Task<Result> PasteRowsAsync(int targetIndex)
	{
		var assembleResult = _clipboardAssembly.AssembleFromClipboard();
		if (assembleResult.IsFailed)
		{
			return assembleResult.ToResult();
		}

		var steps = assembleResult.Value;

		if (steps.Count == 0)
		{
			return await _pipeline.RunAsync(
				OperationMetadata.PasteRows,
				() => Task.FromResult(assembleResult.ToResult()),
				successMessage: null);
		}

		var result = await _pipeline.RunAsync(
			OperationMetadata.PasteRows,
			() => Task.FromResult(PerformPaste(targetIndex, steps, assembleResult.Reasons)),
			successMessage: null);

		if (result.IsSuccess)
		{
			NotifyStructureChanged();
		}

		return result;
	}

	private Result<RecipeAnalysisSnapshot> PerformPaste(
		int targetIndex,
		IReadOnlyList<Step> steps,
		IReadOnlyList<IReason> assemblyReasons)
	{
		var insertResult = _recipeFacade.InsertSteps(targetIndex, steps);
		if (insertResult.IsFailed)
		{
			return insertResult;
		}

		return insertResult.WithReasons(assemblyReasons);
	}

	public async Task<Result> DeleteRowsAsync(IReadOnlyList<int> indices)
	{
		if (indices.Count == 0)
		{
			return Result.Ok();
		}

		var result = await _pipeline.RunAsync(
			OperationMetadata.DeleteRows,
			() => Task.FromResult(PerformDelete(indices)),
			successMessage: $"Удалено {indices.Count} строк");

		if (result.IsSuccess)
		{
			NotifyStructureChanged();
		}

		return result;
	}

	private Result<RecipeAnalysisSnapshot> PerformDelete(IReadOnlyList<int> indices)
	{
		var recipe = _recipeFacade.CurrentSnapshot.Recipe;
		var valid = indices.Where(i => i >= 0 && i < recipe.Steps.Count)
			.Distinct().ToList();

		if (valid.Count == 0)
		{
			return Result.Ok(_recipeFacade.CurrentSnapshot);
		}

		return _recipeFacade.DeleteSteps(valid);
	}

	private void NotifyStructureChanged()
	{
		ViewModel.OnRecipeStructureChanged();
		_timer.Reset();
		RaiseRecipeStructureChanged();
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
