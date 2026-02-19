using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Domain;
using NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Modbus;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.Operations.Modbus;

public sealed class ModbusTcpService : IModbusTcpService
{
	private readonly RecipePlcService _plcService;
	private readonly ModbusRecipeAssemblyService _assemblyService;
	private readonly RecipeComparator _comparator;
	private readonly ILogger<ModbusTcpService> _logger;

	public ModbusTcpService(
		RecipePlcService plcService,
		ModbusRecipeAssemblyService assemblyService,
		RecipeComparator comparator,
		ILogger<ModbusTcpService> logger)
	{
		_plcService = plcService ?? throw new ArgumentNullException(nameof(plcService));
		_assemblyService = assemblyService ?? throw new ArgumentNullException(nameof(assemblyService));
		_comparator = comparator ?? throw new ArgumentNullException(nameof(comparator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public async Task<Result> SendRecipeAsync(Recipe recipe)
	{
		_logger.LogTrace("Starting Send-And-Verify operation for recipe with {StepCount} steps.", recipe.Steps.Count);

		var sendResult = await _plcService.SendAsync(recipe, CancellationToken.None);
		if (sendResult.IsFailed)
		{
			_logger.LogError("Sending to PLC failed: {Errors}", sendResult.Errors);
			return sendResult;
		}

		await Task.Delay(TimeSpan.FromMilliseconds(100), CancellationToken.None);

		_logger.LogTrace("Starting read-back for verification.");

		var receiveResult = await _plcService.ReceiveAsync(CancellationToken.None);
		if (receiveResult.IsFailed)
		{
			_logger.LogError("Read-back from PLC failed: {Errors}", receiveResult.Errors);
			return receiveResult.ToResult();
		}

		var (intData, floatData, rowCount) = receiveResult.Value;
		_logger.LogTrace("Read-back received {RowCount} rows from PLC.", rowCount);

		if (rowCount == 0)
		{
			_logger.LogWarning("Read-back returned zero rows. Recipe not verified.");
			return receiveResult.ToResult();
		}

		_logger.LogTrace("Assembling recipe from read-back data.");

		var assembleResult = _assemblyService.AssembleFromModbusData(intData, floatData, rowCount);
		if (assembleResult.IsFailed)
		{
			_logger.LogError("Assembling recipe from read-back data failed: {Errors}", assembleResult.Errors);
			return assembleResult.ToResult();
		}

		_logger.LogTrace("Comparing original recipe with data read back from PLC.");
		var compareResult = _comparator.Compare(recipe, assembleResult.Value);
		_logger.LogTrace("Recipe comparison result: {Result}", compareResult.IsSuccess ? "OK" : "FAILED");
		return compareResult;
	}

	public async Task<Result<Recipe>> ReceiveRecipeAsync()
	{
		var readResult = await _plcService.ReceiveAsync(CancellationToken.None);
		if (readResult.IsFailed)
			return readResult.ToResult();

		var (intData, floatData, rowCount) = readResult.Value;

		if (rowCount == 0)
		{
			var result = Result.Ok(Recipe.Empty);
			if (readResult.Reasons.Count > 0)
				result = result.WithReasons(readResult.Reasons);
			return result;
		}

		var assembleResult = _assemblyService.AssembleFromModbusData(intData, floatData, rowCount);
		return assembleResult;
	}
}
