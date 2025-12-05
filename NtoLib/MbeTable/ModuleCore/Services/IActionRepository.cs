using System.Collections.Generic;

using FluentResults;

using NtoLib.MbeTable.ModuleConfig.Domain.Actions;

namespace NtoLib.MbeTable.ModuleCore.Services;

public interface IActionRepository
{
	Result<ActionDefinition> GetActionDefinitionById(short id);

	Result<ActionDefinition> GetResultActionDefinitionByName(string name);

	Result<short> GetResultDefaultActionId();

	IReadOnlyDictionary<short, ActionDefinition> Actions { get; }
}
