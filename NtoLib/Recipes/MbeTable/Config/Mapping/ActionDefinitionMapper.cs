

using System;
using System.Collections.Generic;
using System.Linq;
using NtoLib.Recipes.MbeTable.Config.Domain.Actions;
using NtoLib.Recipes.MbeTable.Config.Dto.Actions;
using NtoLib.Recipes.MbeTable.Core.Entities;

namespace NtoLib.Recipes.MbeTable.Config.Mapping;

/// <summary>
/// Maps YamlActionDefinition to ActionDefinition.
/// </summary>
public sealed class ActionDefinitionMapper : IEntityMapper<YamlActionDefinition, ActionDefinition>
{
    public ActionDefinition Map(YamlActionDefinition source)
    {
        if (!Enum.TryParse<DeployDuration>(source.DeployDuration, ignoreCase: true, out var deployDuration))
        {
            throw new NotSupportedException(
                $"Unsupported DeployDuration '{source.DeployDuration}' for action Id={source.Id}.");
        }

        var columns = source.Columns
            .Select(c => new PropertyConfig
            {
                Key = c.Key,
                PropertyTypeId = c.PropertyTypeId,
                DefaultValue = c.DefaultValue,
                GroupName = c.GroupName
            })
            .ToList();

        return new ActionDefinition
        {
            Id = source.Id,
            Name = source.Name,
            DeployDuration = deployDuration,
            Columns = columns
        };
    }

    public IReadOnlyList<ActionDefinition> MapMany(IEnumerable<YamlActionDefinition> sources)
    {
        return sources.Select(Map).ToList();
    }
}