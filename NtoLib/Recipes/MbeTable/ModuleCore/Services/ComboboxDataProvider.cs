using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleInfrastructure.ActionTartget;
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Services;

/// <inheritdoc />
public sealed class ComboboxDataProvider : IComboboxDataProvider
{
    private readonly IActionRepository      _actions;
    private readonly IActionTargetProvider  _targets;

    public ComboboxDataProvider(
        IActionRepository actionRepository,
        IActionTargetProvider actionTargetProvider)
    {
        _actions = actionRepository  ?? throw new ArgumentNullException(nameof(actionRepository));
        _targets = actionTargetProvider ?? throw new ArgumentNullException(nameof(actionTargetProvider));
    }

    /// <inheritdoc />
    public Result<IReadOnlyDictionary<short, string>> GetResultEnumOptions(short actionId, string columnKey)
    {
        var actionResult = _actions.GetActionDefinitionById(actionId);
        if (actionResult.IsFailed)
            return actionResult.ToResult();

        var columnResult = GetColumn(actionResult.Value, columnKey);
        if (columnResult.IsFailed)
            return columnResult.ToResult();

        var validationResult = ValidateColumn(columnResult.Value);
        if (validationResult.IsFailed)
            return validationResult.ToResult();

        return GetFilteredTargets(columnResult.Value.GroupName!);
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<short, string> GetActions()
        => _actions.Actions.Values.ToDictionary(a => a.Id, a => a.Name);

    private static Result<PropertyConfig> GetColumn(ActionDefinition action, string columnKey)
    {
        var col = action.Columns.FirstOrDefault(c =>
            string.Equals(c.Key, columnKey, StringComparison.OrdinalIgnoreCase));

        return col == null
            ? Errors.ColumnNotFoundInAction(action.Name, action.Id, columnKey)
            : Result.Ok(col);
    }

    private static Result<PropertyConfig> ValidateColumn(PropertyConfig column)
    {
        return string.IsNullOrWhiteSpace(column.GroupName) 
            ? Errors.ColumnGroupNameEmpty() 
            : Result.Ok(column);
    }

    private Result<IReadOnlyDictionary<short, string>> GetFilteredTargets(string groupName)
    {
        if (!_targets.TryGetTargets(groupName, out var targets))
            return Errors.TargetsNotDefined();

        return Result.Ok<IReadOnlyDictionary<short, string>>(targets
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => (short)kv.Key, kv => kv.Value));
    }
}