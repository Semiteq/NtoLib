#nullable enable

using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;

/// <summary>
/// Defines a contract for serializing a domain Recipe object to and from raw PLC register arrays.
/// </summary>
public interface IPlcRecipeSerializer
{
    /// <summary>
    /// Converts a list of recipe steps into PLC register arrays based on column configuration.
    /// </summary>
    (int[] IntArray, int[] FloatArray) ToRegisters(IReadOnlyList<Step> steps);

    /// <summary>
    /// Reconstructs a list of recipe steps from PLC register arrays.
    /// </summary>
    List<Step> FromRegisters(int[] intData, int[] floatData, int rowCount);
}