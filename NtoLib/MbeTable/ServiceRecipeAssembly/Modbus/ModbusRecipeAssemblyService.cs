using System;

using FluentResults;

using Microsoft.Extensions.Logging;

using NtoLib.MbeTable.ModuleCore.Entities;
using NtoLib.MbeTable.ServiceRecipeAssembly.Common;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Modbus;

/// <summary>
/// Modbus-specific recipe assembly service.
/// </summary>
public sealed class ModbusRecipeAssemblyService : IModbusRecipeAssemblyService
{
	private readonly ModbusAssemblyStrategy _modbusStrategy;
	private readonly AssemblyValidator _validator;
	private readonly ILogger<ModbusRecipeAssemblyService> _logger;

	public ModbusRecipeAssemblyService(
		ModbusAssemblyStrategy modbusStrategy,
		AssemblyValidator validator,
		ILogger<ModbusRecipeAssemblyService> logger)
	{
		_modbusStrategy = modbusStrategy ?? throw new ArgumentNullException(nameof(modbusStrategy));
		_validator = validator ?? throw new ArgumentNullException(nameof(validator));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public Result<Recipe> AssembleFromModbusData(int[] intData, int[] floatData, int rowCount)
	{
		return AssemblyPipeline.Assemble(
			"Modbus",
			_logger,
			_validator,
			() => _modbusStrategy.AssembleFromModbusData(intData, floatData, rowCount));
	}
}
