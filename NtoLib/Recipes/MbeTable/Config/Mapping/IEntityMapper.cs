

using System.Collections.Generic;

namespace NtoLib.Recipes.MbeTable.Config.Mapping;

/// <summary>
/// Contract for mapping DTOs to domain entities.
/// </summary>
/// <typeparam name="TSrc">Source DTO type.</typeparam>
/// <typeparam name="TDst">Destination domain entity type.</typeparam>
public interface IEntityMapper<in TSrc, out TDst>
    where TSrc : class
    where TDst : class
{
    /// <summary>
    /// Maps a single DTO to domain entity.
    /// </summary>
    /// <param name="source">The source DTO.</param>
    /// <returns>The mapped domain entity.</returns>
    TDst Map(TSrc source);

    /// <summary>
    /// Maps a collection of DTOs to domain entities.
    /// </summary>
    /// <param name="sources">The source DTOs.</param>
    /// <returns>The mapped domain entities.</returns>
    IReadOnlyList<TDst> MapMany(IEnumerable<TSrc> sources);
}