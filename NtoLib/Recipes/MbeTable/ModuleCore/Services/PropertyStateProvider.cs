using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Services;

public sealed class PropertyStateProvider
{
	private readonly IReadOnlyList<ColumnDefinition> _columnsInConfig;

	public PropertyStateProvider(IReadOnlyList<ColumnDefinition> columnsInConfig)
	{
		_columnsInConfig = columnsInConfig ?? throw new ArgumentNullException(nameof(columnsInConfig));
	}

	public PropertyState GetPropertyState(Step step, ColumnIdentifier columnKey)
	{
		if (IsStepStartTimeColumn(columnKey))
		{
			return PropertyState.Readonly;
		}

		if (!PropertyExistsInStep(step, columnKey))
		{
			return PropertyState.Disabled;
		}

		var columnDefinition = FindColumnDefinition(columnKey);
		if (columnDefinition == null)
		{
			return PropertyState.Disabled;
		}

		return columnDefinition.ReadOnly
			? PropertyState.Readonly
			: PropertyState.Enabled;
	}

	private static bool IsStepStartTimeColumn(ColumnIdentifier columnKey)
	{
		return columnKey == MandatoryColumns.StepStartTime;
	}

	private static bool PropertyExistsInStep(Step step, ColumnIdentifier columnKey)
	{
		return step.Properties.TryGetValue(columnKey, out var propertyValue) && propertyValue != null;
	}

	private ColumnDefinition? FindColumnDefinition(ColumnIdentifier columnKey)
	{
		return _columnsInConfig.FirstOrDefault(c => c.Key == columnKey);
	}
}
