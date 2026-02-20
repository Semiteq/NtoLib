using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Columns;
using NtoLib.Recipes.MbeTable.ModuleConfig.Errors;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Validation;

public sealed class ColumnDefsValidator : ISectionValidator<YamlColumnDefinition>
{
	private static readonly List<ColumnIdentifier> _configMandatoryColumns = new()
	{
		MandatoryColumns.Action,
		MandatoryColumns.Task,
		MandatoryColumns.StepDuration,
		MandatoryColumns.StepStartTime,
		MandatoryColumns.Comment
	};

	private static readonly HashSet<string> _supportedColumnTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		ColumnTypeIds.ActionComboBox,
		ColumnTypeIds.ActionTargetComboBox,
		ColumnTypeIds.PropertyField,
		ColumnTypeIds.StepStartTimeField,
		ColumnTypeIds.TextField
	};

	public Result Validate(IReadOnlyList<YamlColumnDefinition> items)
	{
		var emptyCheck = ValidationCheck.NotEmpty(items, "ColumnDefs.yaml");
		if (emptyCheck.IsFailed)
		{
			return emptyCheck;
		}

		var allErrors = new List<ConfigError>();

		var mandatoryResult = ValidateMandatoryColumns(items);
		AddIfFailed(mandatoryResult, allErrors);

		var columnTypesResult = ValidateSupportedColumnTypes(items);
		AddIfFailed(columnTypesResult, allErrors);

		var perItemErrors = ValidateEachDefinition(items);
		allErrors.AddRange(perItemErrors);

		return allErrors.Count > 0 ? Result.Fail(allErrors) : Result.Ok();
	}

	private static Result ValidateMandatoryColumns(IReadOnlyList<YamlColumnDefinition> items)
	{
		var definedKeys = new HashSet<string>(items.Select(d => d.Key), StringComparer.OrdinalIgnoreCase);
		var missingKeys = _configMandatoryColumns
			.Where(mc => !definedKeys.Contains(mc.Value))
			.Select(mc => mc.Value)
			.ToList();

		if (missingKeys.Any())
		{
			var missing = string.Join(", ", missingKeys);

			return Result.Fail(ConfigColumnErrors.MissingMandatory(missing));
		}

		return Result.Ok();
	}

	private static Result ValidateSupportedColumnTypes(IReadOnlyList<YamlColumnDefinition> items)
	{
		var unsupported = items
			.Where(d => d.Ui != null &&
						!string.IsNullOrWhiteSpace(d.Ui.ColumnType) &&
						!_supportedColumnTypes.Contains(d.Ui.ColumnType))
			.Select(d => new { d.Key, d.Ui!.ColumnType })
			.ToList();

		if (unsupported.Any())
		{
			var info = string.Join(", ", unsupported.Select(c => $"'{c.Key}' ({c.ColumnType})"));

			return Result.Fail(ConfigColumnErrors.UnsupportedTypes(info));
		}

		return Result.Ok();
	}

	private static List<ConfigError> ValidateEachDefinition(IReadOnlyList<YamlColumnDefinition> items)
	{
		var errors = new List<ConfigError>();
		var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var col in items)
		{
			var context = $"ColumnDefs.yaml, Key='{col.Key}'";

			ValidateColumnStructure(col, context, errors);
			ValidateColumnKeyUniqueness(col.Key, context, uniqueKeys, errors);
			ValidateColumnBusinessLogic(col, context, errors);
			ValidateColumnUi(col, context, errors);
		}

		return errors;
	}

	private static void ValidateColumnStructure(YamlColumnDefinition column, string context, List<ConfigError> errors)
	{
		AddIfFailed(ValidationCheck.NotEmpty(column.Key, context, "key"), errors);
		AddIfFailed(ValidationCheck.NotNull(column.Ui, context, "ui"), errors);
		AddIfFailed(ValidationCheck.NotNull(column.BusinessLogic, context, "business_logic"), errors);
	}

	private static void ValidateColumnKeyUniqueness(string key, string context, HashSet<string> uniqueKeys,
		List<ConfigError> errors)
	{
		if (!string.IsNullOrWhiteSpace(key))
		{
			AddIfFailed(ValidationCheck.Unique(key, uniqueKeys, context, "key"), errors);
		}
	}

	private static void ValidateColumnBusinessLogic(YamlColumnDefinition column, string context,
		List<ConfigError> errors)
	{
		if (column.BusinessLogic == null)
		{
			return;
		}

		AddIfFailed(ValidationCheck.NotEmpty(column.BusinessLogic.PropertyTypeId, context, "property_type_id"), errors);

		if (column.BusinessLogic.PlcMapping != null)
		{
			ValidatePlcMapping(column.BusinessLogic.PlcMapping, context, errors);
		}
	}

	private static void ValidatePlcMapping(YamlPlcMapping plcMapping, string context, List<ConfigError> errors)
	{
		AddIfFailed(ValidationCheck.NotEmpty(plcMapping.Area, context, "plc_mapping.area"), errors);
		AddIfFailed(ValidationCheck.NonNegative(plcMapping.Index, context, "plc_mapping.index"), errors);
	}

	private static void ValidateColumnUi(YamlColumnDefinition column, string context, List<ConfigError> errors)
	{
		if (column.Ui == null)
		{
			return;
		}

		var allowFullWidth = IsCommentColumn(column.Key);

		ValidateUiBasicFields(column.Ui, context, errors);
		ValidateUiWidthFields(column.Ui, context, column.Key, allowFullWidth, errors);
		ValidateMaxDropdownItems(column, context, errors);
		ValidateActionComboBoxBinding(column, context, errors);
	}

	private static bool IsCommentColumn(string columnKey)
	{
		return string.Equals(columnKey, "comment", StringComparison.OrdinalIgnoreCase);
	}

	private static void ValidateUiBasicFields(YamlColumnUi ui, string context, List<ConfigError> errors)
	{
		AddIfFailed(ValidationCheck.NotEmpty(ui.Code, context, "code"), errors);
		AddIfFailed(ValidationCheck.NotEmpty(ui.UiName, context, "ui_name"), errors);
	}

	private static void ValidateUiWidthFields(YamlColumnUi ui, string context, string columnKey, bool allowFullWidth,
		List<ConfigError> errors)
	{
		AddIfFailed(ValidationCheck.ValidateWidth(ui.Width, context, columnKey, allowFullWidth), errors);
		AddIfFailed(ValidationCheck.ValidateMinWidth(ui.MinWidth, context, columnKey), errors);
	}

	private static void ValidateMaxDropdownItems(YamlColumnDefinition column, string context, List<ConfigError> errors)
	{
		if (column.Ui == null)
		{
			return;
		}

		if (column.Ui.MaxDropdownItems <= 0)
		{
			errors.Add(ConfigColumnErrors.InvalidMaxDropdownItems(column.Key, column.Ui.MaxDropdownItems, context));
		}
	}

	private static void ValidateActionComboBoxBinding(YamlColumnDefinition column, string context,
		List<ConfigError> errors)
	{
		if (column.Ui == null)
		{
			return;
		}

		var isActionComboBox = string.Equals(column.Ui.ColumnType, ColumnTypeIds.ActionComboBox,
			StringComparison.OrdinalIgnoreCase);
		var isNotActionColumn = !string.Equals(column.Key, "action", StringComparison.OrdinalIgnoreCase);

		if (isActionComboBox && isNotActionColumn)
		{
			errors.Add(ConfigColumnErrors.InvalidActionComboBinding(column.Key, column.Ui.ColumnType, context));
		}
	}

	private static void AddIfFailed(Result result, List<ConfigError> errors)
	{
		if (result.IsFailed)
		{
			foreach (var e in result.Errors)
			{
				errors.Add(e as ConfigError ?? new ConfigError(e.Message, "ColumnDefs.yaml", "validation"));
			}
		}
	}
}
