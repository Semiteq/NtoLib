using System.Collections.Generic;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ModuleConfig.Common;

/// <summary>
/// Contract for YAML deserialization.
/// </summary>
public interface IYamlDeserializer
{
    /// <summary>
    /// Deserializes YAML string to a collection of objects.
    /// </summary>
    /// <typeparam name="T">The target type.</typeparam>
    /// <param name="yaml">The YAML content.</param>
    /// <returns>Result containing the deserialized collection or errors.</returns>
    Result<IReadOnlyList<T>> Deserialize<T>(string yaml) where T : class;
}