using System;
using System.Collections.Generic;
using System.Linq;

using NtoLib.Recipes.MbeTable.ModuleConfig.Common;
using NtoLib.Recipes.MbeTable.ModuleConfig.Dto.Properties;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Contracts;
using NtoLib.Recipes.MbeTable.ModuleCore.Properties.Definitions;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Mapping;

/// <summary>
/// Maps YamlPropertyDefinition to IPropertyTypeDefinition.
/// Handles special types (Time, Enum) and standard numeric/string types.
/// </summary>
public sealed class PropertyDefinitionMapper : IEntityMapper<YamlPropertyDefinition, IPropertyTypeDefinition>
{
    public IPropertyTypeDefinition Map(YamlPropertyDefinition source)
    {
        if (string.Equals(source.PropertyTypeId, PropertyTypeIds.Time, StringComparison.OrdinalIgnoreCase))
            return new DynamicTimeDefinition(source);

        if (string.Equals(source.PropertyTypeId, PropertyTypeIds.Enum, StringComparison.OrdinalIgnoreCase))
            return new ConfigurableEnumDefinition(source);

        var systemType = Type.GetType(source.SystemType, throwOnError: true, ignoreCase: true)!;

        if (systemType == typeof(string))
            return new ConfigurableStringDefinition(source);

        if (systemType == typeof(short) || systemType == typeof(float))
            return new ConfigurableNumericDefinition(source);

        throw new NotSupportedException(
            $"Unsupported SystemType '{source.SystemType}' for PropertyTypeId '{source.PropertyTypeId}'.");
    }

    public IReadOnlyList<IPropertyTypeDefinition> MapMany(IEnumerable<YamlPropertyDefinition> sources)
    {
        return sources.Select(Map).ToList();
    }
}