using System;
using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;
using NtoLib.Recipes.MbeTable.ModuleCore.Services;

namespace NtoLib.Recipes.MbeTable.ModuleApplication.ViewModels;

public sealed class StepViewModel
{
	private Step _step;
	private TimeSpan _startTime;
	private readonly ComboboxDataProvider _comboboxDataProvider;

	public StepViewModel(
		Step step,
		TimeSpan startTime,
		ComboboxDataProvider comboboxDataProvider)
	{
		_step = step ?? throw new ArgumentNullException(nameof(step));
		_comboboxDataProvider = comboboxDataProvider ?? throw new ArgumentNullException(nameof(comboboxDataProvider));
		_startTime = startTime;
	}

	public string StepStartTime => FormatTime(_startTime);

	public Result<IReadOnlyDictionary<short, string>> GetComboItems(ColumnIdentifier key)
	{
		var actionIdResult = GetCurrentActionId();
		return actionIdResult.IsFailed
			? actionIdResult.ToResult()
			: _comboboxDataProvider.GetResultEnumOptions(actionIdResult.Value, key.Value);
	}

	public Result<object?> GetPropertyValue(ColumnIdentifier identifier)
	{
		if (identifier == MandatoryColumns.StepStartTime)
		{
			return StepStartTime;
		}

		var getPropertyResult = _step.GetProperty(identifier);
		if (getPropertyResult.IsFailed)
		{
			return getPropertyResult.ToResult();
		}

		var property = getPropertyResult.Value;

		var value = identifier == MandatoryColumns.Action
			? property.GetValueAsObject
			: property.GetDisplayValue;

		return value;
	}

	private Result<short> GetCurrentActionId()
	{
		var getPropertyResult = _step.GetProperty(MandatoryColumns.Action);
		if (getPropertyResult.IsFailed)
		{
			return getPropertyResult.ToResult();
		}

		var actionProperty = getPropertyResult.Value;

		var getValueResult = actionProperty.GetValue<short>();
		if (getValueResult.IsFailed)
		{
			return getValueResult.ToResult();
		}

		return Result.Ok(getValueResult.Value);
	}

	internal void UpdateInPlace(Step newStep, TimeSpan newStartTime)
	{
		_step = newStep ?? throw new ArgumentNullException(nameof(newStep));
		_startTime = newStartTime;
	}

	private static string FormatTime(TimeSpan time) => time.ToString(@"hh\:mm\:ss");
}
