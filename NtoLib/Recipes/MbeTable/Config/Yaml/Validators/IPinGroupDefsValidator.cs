using System.Collections.Generic;
using FluentResults;
using NtoLib.Recipes.MbeTable.Config.Yaml.Models.PinGroups;

namespace NtoLib.Recipes.MbeTable.Config.Yaml.Validators;

public interface IPinGroupDefsValidator
{
    /// <summary>
    /// Validates the provided groups.
    /// </summary>
    Result Validate(IReadOnlyCollection<PinGroupData> groups);
}