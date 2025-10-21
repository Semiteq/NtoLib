using System.Collections.Generic;

using FluentResults;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;

namespace NtoLib.Recipes.MbeTable.ModuleCore.Services;

public interface IActionRepository
{
    Result<ActionDefinition> GetResultActionDefinitionById(short id);
    
    Result<ActionDefinition> GetResultActionDefinitionByName(string name);

    Result<short> GetResultDefaultActionId();

    IReadOnlyDictionary<short, ActionDefinition> Actions { get; }
}