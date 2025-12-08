using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;
using NtoLib.Recipes.MbeTable.ModuleCore.Snapshot;
using NtoLib.Recipes.MbeTable.ServiceClipboard;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Clipboard.Schema;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.CutSteps;

public sealed class CutRowsOperationHandler : IRecipeOperationHandler<CutRowsArgs>
{
	private readonly OperationPipeline _pipeline;
	private readonly CutRowsOperationDefinition _op;
	private readonly IRecipeFacade _facade;
	private readonly IClipboardSchemaDescriptor _schema;
	private readonly IClipboardService _clipboard;
	private readonly ITimerService _timer;
	private readonly RecipeViewModel _viewModel;

	public CutRowsOperationHandler(
		OperationPipeline pipeline,
		CutRowsOperationDefinition op,
		IRecipeFacade facade,
		IClipboardSchemaDescriptor schema,
		IClipboardService clipboard,
		ITimerService timer,
		RecipeViewModel viewModel)
	{
		_pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
		_op = op ?? throw new ArgumentNullException(nameof(op));
		_facade = facade ?? throw new ArgumentNullException(nameof(facade));
		_schema = schema ?? throw new ArgumentNullException(nameof(schema));
		_clipboard = clipboard ?? throw new ArgumentNullException(nameof(clipboard));
		_timer = timer ?? throw new ArgumentNullException(nameof(timer));
		_viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
	}

	public async Task<Result> ExecuteAsync(CutRowsArgs args)
	{
		if (args.Indices.Count == 0)
			return Result.Ok();

		var result = await _pipeline.RunAsync(
			_op,
			() => Task.FromResult(PerformCut(args.Indices)),
			successMessage: $"Вырезано {args.Indices.Count} строк");

		if (result.IsSuccess)
		{
			_viewModel.OnRecipeStructureChanged();
			_timer.Reset();
		}

		return result.ToResult();
	}

	private Result<RecipeAnalysisSnapshot> PerformCut(IReadOnlyList<int> indices)
	{
		var recipe = _facade.CurrentSnapshot.Recipe;
		var valid = indices.Where(i => i >= 0 && i < recipe.Steps.Count).Distinct().OrderBy(i => i).ToList();

		if (valid.Count == 0)
			return Result.Ok(_facade.CurrentSnapshot);

		var steps = valid.Select(i => recipe.Steps[i]).ToList();
		var writeResult = _clipboard.WriteSteps(steps, _schema.TransferColumns);
		if (writeResult.IsFailed)
			return writeResult.ToResult<RecipeAnalysisSnapshot>();

		var deleteResult = _facade.DeleteSteps(valid);
		if (deleteResult.IsFailed)
			return deleteResult;

		return deleteResult.WithReasons(writeResult.Reasons);
	}
}

public sealed class CutRowsArgs
{
	public IReadOnlyList<int> Indices { get; }

	public CutRowsArgs(IReadOnlyList<int> indices)
	{
		Indices = indices ?? throw new ArgumentNullException(nameof(indices));
	}
}
