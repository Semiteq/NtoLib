#nullable enable

using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.PinGroups;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Loaders;

/// <summary>
/// Defines a contract for loading pin groups configuration from a JSON file (PinGroupDefs.yaml).
/// </summary>
public interface IPinGroupDefsLoader
{
    Result<IReadOnlyList<PinGroupData>> LoadPinGroups(string configPath);
}