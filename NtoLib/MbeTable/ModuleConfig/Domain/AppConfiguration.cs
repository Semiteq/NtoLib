using System.Collections.Generic;

using NtoLib.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.MbeTable.ModuleConfig.Domain.PinGroups;
using NtoLib.MbeTable.ModuleCore.Properties.Contracts;

namespace NtoLib.MbeTable.ModuleConfig.Domain;

public sealed record AppConfiguration(
	IReadOnlyDictionary<string, IPropertyTypeDefinition> PropertyDefinitions,
	IReadOnlyList<ColumnDefinition> Columns,
	IReadOnlyDictionary<short, ActionDefinition> Actions,
	IReadOnlyCollection<PinGroupData> PinGroupData);
