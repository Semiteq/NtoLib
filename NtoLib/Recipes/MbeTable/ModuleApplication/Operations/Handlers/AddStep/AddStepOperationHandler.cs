using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;
using NtoLib.Recipes.MbeTable.ModuleCore.Runtime;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.AddStep;

public sealed class AddStepOperationHandler : IRecipeOperationHandler<AddStepArgs>
{
	private readonly OperationPipeline _pipeline;
	private readonly AddStepOperationDefinition _op;
	private readonly ITimerService _timer;
	private readonly RecipeViewModel _viewModel;
	private readonly IRecipeFacade _recipeService;

	public AddStepOperationHandler(
		OperationPipeline pipeline,
		AddStepOperationDefinition op,
		ITimerService timer,
		RecipeViewModel viewModel,
		IRecipeFacade recipeService)
	{
		_pipeline = pipeline;
		_op = op;
		_timer = timer;
		_viewModel = viewModel;
		_recipeService = recipeService;
	}

	public async Task<Result> ExecuteAsync(AddStepArgs args)
	{
		var result = await _pipeline.RunAsync(
			_op,
			() => Task.FromResult(_recipeService.AddStep(args.Index)),
			successMessage: $"Добавлена строка №{args.Index + 1}");

		if (result.IsSuccess)
		{
			_viewModel.OnRecipeStructureChanged();
			_timer.Reset();
		}

		return result.ToResult();
	}
}
