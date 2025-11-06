using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ModuleConfig.Domain.Actions;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Actions;
using NtoLib.Recipes.MbeTable.ModuleCore.Entities;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Mapping;

/// <summary>
/// Maps YamlActionDefinition to ActionDefinition.
/// </summary>
public sealed class ActionDefinitionMapper : IEntityMapper<YamlActionDefinition, ActionDefinition>
{
    public ActionDefinition Map(YamlActionDefinition source)
    {
        var deployDuration = Enum.TryParse<DeployDuration>(source.DeployDuration, ignoreCase: true, out var parsed)
            ? parsed
            : DeployDuration.Immediate;

        var columns = source.Columns
            .Select(c => new PropertyConfig
            {
                Key = c.Key,
                PropertyTypeId = c.PropertyTypeId,
                DefaultValue = c.DefaultValue,
                GroupName = c.GroupName
            })
            .ToList();

        FormulaDefinition? formula = null;
        if (source.Formula != null)
        {
            formula = new FormulaDefinition
            {
                Expression = source.Formula.Expression,
                RecalcOrder = source.Formula.RecalcOrder.AsReadOnly()
            };
        }

        return new ActionDefinition(
            Id: source.Id,
            Name: source.Name,
            Columns: columns,
            DeployDuration: deployDuration,
            Formula: formula
        );
    }

    public IReadOnlyList<ActionDefinition> MapMany(IEnumerable<YamlActionDefinition> sources)
    {
        return sources.Select(Map).ToList();
    }
}