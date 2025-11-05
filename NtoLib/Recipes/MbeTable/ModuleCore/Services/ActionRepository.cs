using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ResultsExtension;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Services;

public sealed class ActionRepository : IActionRepository
{
    public ActionRepository(AppConfiguration configuration)
    {
        Actions = configuration.Actions ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IReadOnlyDictionary<short, ActionDefinition> Actions { get; }

    public Result<ActionDefinition> GetActionDefinitionById(short id)
    {
        return Actions.TryGetValue(id, out var action) 
            ? Result.Ok(action) 
            : Errors.ActionNotFound(id);
    }

    public Result<ActionDefinition> GetResultActionDefinitionByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Errors.ActionNameEmpty();

        var action = Actions.Values.FirstOrDefault(a => 
            string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

        if (action != null)
            return Result.Ok(action);

        return Errors.ActionNameNotFound(name);
    }

    public Result<short> GetResultDefaultActionId()
    {
        var first = Actions.Values.FirstOrDefault();
        return first is null 
            ? Errors.NoActionsInConfig() 
            : Result.Ok(first.Id);
    }
}