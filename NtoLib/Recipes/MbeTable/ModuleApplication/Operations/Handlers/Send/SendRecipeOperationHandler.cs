using System.Threading.Tasks;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Modbus;
using NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Pipeline;
using NtoLib.Recipes.MbeTable.ModuleCore.Facade;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Handlers.Send;

public sealed class SendRecipeOperationHandler : IRecipeOperationHandler<SendRecipeArgs>
{
	private readonly OperationPipeline _pipeline;
	private readonly SendRecipeOperationDefinition _op;
	private readonly IModbusTcpService _modbus;
	private readonly IRecipeFacade _recipeService;

	public SendRecipeOperationHandler(
		OperationPipeline pipeline,
		SendRecipeOperationDefinition op,
		IModbusTcpService modbus,
		IRecipeFacade recipeService)
	{
		_pipeline = pipeline;
		_op = op;
		_modbus = modbus;
		_recipeService = recipeService;
	}

	public async Task<Result> ExecuteAsync(SendRecipeArgs args)
	{
		return await _pipeline.RunAsync(
			_op,
			PerformSendAsync,
			successMessage: "Рецепт успешно отправлен в контроллер");
	}

	private Task<Result> PerformSendAsync()
	{
		var current = _recipeService.CurrentSnapshot.Recipe;
		return _modbus.SendRecipeAsync(current);
	}
}
