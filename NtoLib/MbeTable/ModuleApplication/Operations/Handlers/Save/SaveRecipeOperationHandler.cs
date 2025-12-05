using System;
using System.IO;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.MbeTable.ModuleApplication.Operations.Csv;
using NtoLib.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.MbeTable.ModuleApplication.Reasons.Errors;
using NtoLib.MbeTable.ModuleCore.Facade;

namespace NtoLib.MbeTable.ModuleApplication.Operations.Handlers.Save;

public sealed class SaveRecipeOperationHandler : IRecipeOperationHandler<SaveRecipeArgs>
{
	private readonly OperationPipeline _pipeline;
	private readonly SaveRecipeOperationDefinition _op;
	private readonly ICsvService _csv;
	private readonly IRecipeFacade _recipeService;
	private readonly ILogger<SaveRecipeOperationHandler> _logger;

	public SaveRecipeOperationHandler(
		OperationPipeline pipeline,
		SaveRecipeOperationDefinition op,
		ICsvService csv,
		IRecipeFacade recipeService,
		ILogger<SaveRecipeOperationHandler> logger)
	{
		_pipeline = pipeline;
		_op = op;
		_csv = csv;
		_recipeService = recipeService;
		_logger = logger;
	}

	public async Task<Result> ExecuteAsync(SaveRecipeArgs args)
	{
		return await _pipeline.RunAsync(
			_op,
			() => PerformSaveAsync(args.FilePath),
			successMessage: $"Рецепт сохранен в {Path.GetFileName(args.FilePath)}");
	}

	private async Task<Result> PerformSaveAsync(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath))
			return new ApplicationFilePathEmptyError();

		try
		{
			var currentRecipe = _recipeService.LastValidSnapshot!.Recipe;
			return await _csv.WriteCsvAsync(currentRecipe, filePath).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			_logger.LogCritical(ex, "Unexpected error during save operation");
			return Result.Fail(new ApplicationUnexpectedIoWriteError());
		}
	}
}
