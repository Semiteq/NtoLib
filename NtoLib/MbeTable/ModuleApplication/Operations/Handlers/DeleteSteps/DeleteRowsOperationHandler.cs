using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.MbeTable.ModuleApplication.ViewModels;
using NtoLib.MbeTable.ModuleCore.Facade;
using NtoLib.MbeTable.ModuleCore.Runtime;
using NtoLib.MbeTable.ModuleCore.Snapshot;

namespace NtoLib.MbeTable.ModuleApplication.Operations.Handlers.DeleteSteps;

public sealed class DeleteRowsOperationHandler : IRecipeOperationHandler<DeleteRowsArgs>
{
	private readonly OperationPipeline _pipeline;
	private readonly DeleteRowsOperationDefinition _op;
	private readonly IRecipeFacade _facade;
	private readonly ITimerService _timer;
	private readonly RecipeViewModel _viewModel;

	public DeleteRowsOperationHandler(
		OperationPipeline pipeline,
		DeleteRowsOperationDefinition op,
		IRecipeFacade facade,
		ITimerService timer,
		RecipeViewModel viewModel)
	{
		_pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
		_op = op ?? throw new ArgumentNullException(nameof(op));
		_facade = facade ?? throw new ArgumentNullException(nameof(facade));
		_timer = timer ?? throw new ArgumentNullException(nameof(timer));
		_viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
	}

	public async Task<Result> ExecuteAsync(DeleteRowsArgs args)
	{
		if (args.Indices.Count == 0)
			return Result.Ok();

		var result = await _pipeline.RunAsync(
			_op,
			() => Task.FromResult(PerformDelete(args.Indices)),
			successMessage: $"Удалено {args.Indices.Count} строк");

		if (result.IsSuccess)
		{
			_viewModel.OnRecipeStructureChanged();
			_timer.Reset();
		}

		return result.ToResult();
	}

	private Result<RecipeAnalysisSnapshot> PerformDelete(IReadOnlyList<int> indices)
	{
		var recipe = _facade.CurrentSnapshot.Recipe;
		var valid = indices.Where(i => i >= 0 && i < recipe.Steps.Count).Distinct().ToList();

		if (valid.Count == 0)
			return Result.Ok(_facade.CurrentSnapshot);

		var deleteResult = _facade.DeleteSteps(valid);
		return deleteResult;
	}
}

public sealed class DeleteRowsArgs
{
	public IReadOnlyList<int> Indices { get; }

	public DeleteRowsArgs(IReadOnlyList<int> indices)
	{
		Indices = indices ?? throw new ArgumentNullException(nameof(indices));
	}
}
