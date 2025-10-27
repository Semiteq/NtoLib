using System;
using System.Collections.Generic;
using System.Linq;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ResultsExtension.ErrorDefinitions;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Services;

public sealed class ActionRepository : IActionRepository
{
    public ActionRepository(AppConfiguration configuration)
    {
        Actions = configuration.Actions ?? throw new ArgumentNullException(nameof(configuration));
    }

    public IReadOnlyDictionary<short, ActionDefinition> Actions { get; }

    public Result<ActionDefinition> GetResultActionDefinitionById(short id)
    {
        if (Actions.TryGetValue(id, out var action))
            return Result.Ok(action);

        return Result.Fail(new Error($"Action with id {id} not found")
            .WithMetadata(nameof(Codes), Codes.CoreActionNotFound)
            .WithMetadata("actionId", id));
    }

    public Result<ActionDefinition> GetResultActionDefinitionByName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail(new Error("Action name is empty")
                .WithMetadata(nameof(Codes), Codes.CoreActionNotFound));
        }

        var action = Actions.Values.FirstOrDefault(a => 
            string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));

        if (action != null)
            return Result.Ok(action);

        return Result.Fail(new Error($"Action with name '{name}' not found")
            .WithMetadata(nameof(Codes), Codes.CoreActionNotFound)
            .WithMetadata("actionName", name));
    }

    public Result<short> GetResultDefaultActionId()
    {
        var first = Actions.Values.FirstOrDefault();
        if (first != null)
            return Result.Ok(first.Id);

        return Result.Fail(new Error("No actions defined in configuration")
            .WithMetadata(nameof(Codes), Codes.CoreActionNotFound));
    }
}