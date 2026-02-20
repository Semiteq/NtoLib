using System;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.RuntimeOptions;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Domain;
using NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;

public sealed class PlcCapacityCalculator
{
	private readonly RecipeColumnLayout _layout;
	private readonly FbRuntimeOptionsProvider _optionsProvider;

	public PlcCapacityCalculator(
		RecipeColumnLayout layout,
		FbRuntimeOptionsProvider optionsProvider)
	{
		_layout = layout ?? throw new ArgumentNullException(nameof(layout));
		_optionsProvider = optionsProvider ?? throw new ArgumentNullException(nameof(optionsProvider));
	}

	public Result TryCheckCapacity(Recipe recipe)
	{
		if (recipe is null)
		{
			throw new ArgumentNullException(nameof(recipe));
		}

		var settings = _optionsProvider.GetCurrent();
		var rows = recipe.Steps.Count;

		var requiredInt = rows * _layout.IntColumnCount;
		var requiredFloat = rows * _layout.FloatColumnCount * 2;

		if (requiredInt > settings.IntAreaSize)
		{
			return Result.Fail(new ModbusTcpCapacityExceededError("INT", requiredInt, settings.IntAreaSize));
		}

		if (requiredFloat > settings.FloatAreaSize)
		{
			return Result.Fail(new ModbusTcpCapacityExceededError("FLOAT", requiredFloat, settings.FloatAreaSize));
		}

		return Result.Ok();
	}

	public Result ValidateReadCapacity(int rowCount)
	{
		if (rowCount < 0)
		{
			return Result.Fail(new ModbusTcpInvalidResponseError("Invalid negative row count"));
		}

		if (rowCount == 0)
		{
			return Result.Ok();
		}

		var settings = _optionsProvider.GetCurrent();
		var requiredInt = rowCount * _layout.IntColumnCount;
		var requiredFloat = rowCount * _layout.FloatColumnCount * 2;

		if (requiredInt > settings.IntAreaSize)
		{
			return Result.Fail(new ModbusTcpCapacityExceededError("INT", requiredInt, settings.IntAreaSize));
		}

		if (requiredFloat > settings.FloatAreaSize)
		{
			return Result.Fail(new ModbusTcpCapacityExceededError("FLOAT", requiredFloat, settings.FloatAreaSize));
		}

		return Result.Ok();
	}
}
