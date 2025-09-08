#nullable enable
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;

/// <summary>
/// Defines a contract for serializing a domain Recipe object to and from raw PLC register arrays.
/// </summary>
public interface IPlcRecipeSerializer
{
    (int[] IntArray, int[] FloatArray, int[] BoolArray) ToRegisters(IReadOnlyList<Step> steps);

    List<Step> FromRegisters(int[] intData, int[] floatData, int rowCount);
}