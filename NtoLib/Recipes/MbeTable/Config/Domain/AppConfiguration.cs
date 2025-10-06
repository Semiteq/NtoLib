

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Domain.Actions;
using NtoLib.Recipes.MbeTable.Config.Domain.Columns;
using NtoLib.Recipes.MbeTable.Config.Domain.PinGroups;
using NtoLib.Recipes.MbeTable.Core.Properties.Contracts;

namespace NtoLib.Recipes.MbeTable.Config.Domain;

/// <summary>
/// Contains the complete validated application configuration assembled from YAML files.
/// </summary>
public sealed record AppConfiguration(
    IReadOnlyDictionary<string, IPropertyTypeDefinition> PropertyDefinitions,
    IReadOnlyList<ColumnDefinition> Columns,
    IReadOnlyDictionary<short, ActionDefinition> Actions,
    IReadOnlyCollection<PinGroupData> PinGroupData
);