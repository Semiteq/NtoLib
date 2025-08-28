using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Config.Models.Actions;
using NtoLib.Recipes.MbeTable.Core.Domain.Services;

namespace NtoLib.Recipes.MbeTable.Config;

/// <summary>
/// A container for the entire application's configuration, loaded at startup.
/// This can remain a record as it's constructed in code, not deserialized directly.
/// </summary>
/// <param name="Schema">The definition of the table's structure.</param>
/// <param name="Actions">A dictionary of all available actions, keyed by their ID.</param>
public sealed record AppConfiguration(
    TableSchema Schema,
    IReadOnlyDictionary<int, ActionDefinition> Actions
);