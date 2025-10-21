using System.Collections.Generic;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Columns;
using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.PinGroups;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Domain;

/// <summary>
/// Contains the complete validated application configuration assembled from YAML files.
/// </summary>
public sealed record AppConfiguration(
    IReadOnlyDictionary<string, IPropertyTypeDefinition> PropertyDefinitions,
    IReadOnlyList<ColumnDefinition> Columns,
    IReadOnlyDictionary<short, ActionDefinition> Actions,
    IReadOnlyCollection<PinGroupData> PinGroupData
);