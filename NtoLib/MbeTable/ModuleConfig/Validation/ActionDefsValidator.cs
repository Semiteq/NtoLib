using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.MbeTable.ModuleConfig.Common;
using NtoLib.MbeTable.ModuleConfig.Dto.Actions;
using NtoLib.MbeTable.ModuleConfig.Errors;
using NtoLib.MbeTable.ModuleCore.Entities;

namespace NtoLib.MbeTable.ModuleConfig.Validation;

public sealed class ActionDefsValidator : ISectionValidator<YamlActionDefinition>
{
	public Result Validate(IReadOnlyList<YamlActionDefinition> items)
	{
		var emptyCheck = ValidationCheck.NotEmpty(items, "ActionsDefs.yaml");
		if (emptyCheck.IsFailed)
			return emptyCheck;

		var errors = new List<ConfigError>();
		var uniqueIds = new HashSet<int>();

		foreach (var action in items)
		{
			var context = $"ActionsDefs.yaml, ActionId={action.Id}, ActionName='{action.Name}'";

			AddIfFailed(ValidationCheck.Positive(action.Id, context, "id"), errors);
			AddIfFailed(ValidationCheck.NotEmpty(action.Name, context, "name"), errors);
			AddIfFailed(ValidationCheck.NotNull(action.Columns, context, "columns"), errors);

			AddIfFailed(ValidationCheck.Unique(action.Id, uniqueIds, context, "id"), errors);

			AddIfFailed(ValidateDeployDuration(action, context), errors);

			if (action.Columns != null)
				errors.AddRange(ValidateActionColumns(action, context));

			AddIfFailed(ValidateLongLastingRequirements(action, context), errors);
		}

		return errors.Count > 0 ? Result.Fail(errors) : Result.Ok();
	}

	private static Result ValidateDeployDuration(YamlActionDefinition action, string context)
	{
		var durationCheck = ValidationCheck.NotEmpty(action.DeployDuration, context, "deploy_duration");
		if (durationCheck.IsFailed)
			return durationCheck;

		if (!Enum.TryParse<DeployDuration>(action.DeployDuration, ignoreCase: true, out _))
			return Result.Fail(ConfigActionErrors.UnknownDeployDuration(action.DeployDuration, context));

		return Result.Ok();
	}

	private static List<ConfigError> ValidateActionColumns(YamlActionDefinition action, string context)
	{
		var errors = new List<ConfigError>();
		var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (var column in action.Columns)
		{
			var columnContext = BuildColumnContext(context, column.Key);
			ValidateSingleColumn(column, columnContext, uniqueKeys, errors);
		}

		return errors;
	}

	private static string BuildColumnContext(string actionContext, string columnKey)
	{
		return $"{actionContext}, ColumnKey='{columnKey}'";
	}

	private static void ValidateSingleColumn(
		YamlActionColumn column,
		string columnContext,
		HashSet<string> uniqueKeys,
		List<ConfigError> errors)
	{
		ValidateColumnBasicFields(column, columnContext, errors);
		ValidateColumnKeyUniqueness(column.Key, columnContext, uniqueKeys, errors);
		ValidateEnumColumnRequirements(column, columnContext, errors);
	}

	private static void ValidateColumnBasicFields(
		YamlActionColumn column,
		string columnContext,
		List<ConfigError> errors)
	{
		AddIfFailed(ValidationCheck.NotEmpty(column.Key, columnContext, "key"), errors);
		AddIfFailed(ValidationCheck.NotEmpty(column.PropertyTypeId, columnContext, "property_type_id"), errors);
	}

	private static void ValidateColumnKeyUniqueness(
		string columnKey,
		string columnContext,
		HashSet<string> uniqueKeys,
		List<ConfigError> errors)
	{
		if (!string.IsNullOrWhiteSpace(columnKey))
		{
			AddIfFailed(ValidationCheck.Unique(columnKey, uniqueKeys, columnContext, $"column key '{columnKey}'"),
				errors);
		}
	}

	private static void ValidateEnumColumnRequirements(
		YamlActionColumn column,
		string columnContext,
		List<ConfigError> errors)
	{
		var isEnumType = string.Equals(column.PropertyTypeId, PropertyTypeIds.Enum, StringComparison.OrdinalIgnoreCase);
		var hasNoGroupName = string.IsNullOrWhiteSpace(column.GroupName);

		if (isEnumType && hasNoGroupName)
		{
			errors.Add(ConfigActionErrors.EnumGroupMissing(column.Key, column.PropertyTypeId, columnContext));
		}
	}


	private static Result ValidateLongLastingRequirements(YamlActionDefinition action, string context)
	{
		if (!IsLongLastingAction(action))
			return Result.Ok();

		if (!HasStepDurationColumn(action))
			return Result.Fail(ConfigActionErrors.LongLastingStepDurationMissing(context));

		return Result.Ok();
	}

	private static bool IsLongLastingAction(YamlActionDefinition action)
	{
		return string.Equals(action.DeployDuration, "LongLasting", StringComparison.OrdinalIgnoreCase);
	}

	private static bool HasStepDurationColumn(YamlActionDefinition action)
	{
		return action.Columns?.Any(c =>
			string.Equals(c.Key, "step_duration", StringComparison.OrdinalIgnoreCase)) == true;
	}

	private static void AddIfFailed(Result result, List<ConfigError> errors)
	{
		if (result.IsFailed)
		{
			foreach (var e in result.Errors)
				errors.Add(e as ConfigError ?? new ConfigError(e.Message, "ActionsDefs.yaml", "validation"));
		}
	}
}
