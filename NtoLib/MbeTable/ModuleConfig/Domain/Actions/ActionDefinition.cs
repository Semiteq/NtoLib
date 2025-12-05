using System.Collections.Generic;

using NtoLib.MbeTable.ModuleCore.Entities;

namespace NtoLib.MbeTable.ModuleConfig.Domain.Actions;

public sealed record ActionDefinition(
	short Id,
	string Name,
	IReadOnlyList<PropertyConfig> Columns,
	DeployDuration DeployDuration,
	FormulaDefinition? Formula
);
