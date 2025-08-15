#nullable enable
using System.Collections.Generic;
using NtoLib.Recipes.MbeTable.Core.Domain.Entities;

namespace NtoLib.Recipes.MbeTable.Infrastructure.PlcCommunication;

/// <summary>
/// Register mapper between domain steps and PLC registers.
/// </summary>
public interface IPlcRecipeMapper
{
    /// <summary>
    /// Converts a list of domain steps into PLC register arrays for integer, float, and boolean data types.
    /// </summary>
    /// <param name="steps">A list of <see cref="Step"/> objects representing the domain steps to be mapped to PLC registers.</param>
    /// <returns>
    /// A tuple containing three arrays:
    /// <c>IntArray</c> for integer-based registers,
    /// <c>FloatArray</c> for float-based registers,
    /// and <c>BoolArray</c> for boolean-based registers.
    /// </returns>
    (int[] IntArray, int[] FloatArray, int[] BoolArray) ToRegisters(List<Step> steps);

    /// <summary>
    /// Converts PLC register data arrays into a list of domain steps based on provided row count.
    /// </summary>
    /// <param name="intData">An array of integers representing integer-based PLC register data.</param>
    /// <param name="floatData">An array of integers representing float-based PLC register data in a specific encoding.</param>
    /// <param name="rowCount">The number of rows to map from PLC register data to domain steps.</param>
    /// <returns>
    /// A list of <see cref="Step"/> objects representing the domain steps, mapped from the provided PLC register data.
    /// </returns>
    List<Step> FromRegisters(int[] intData, int[] floatData, int rowCount);
}