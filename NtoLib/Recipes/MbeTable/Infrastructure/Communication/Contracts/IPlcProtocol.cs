#nullable enable

using FluentResults;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;

/// <summary>
/// Defines a contract for low-level communication with the PLC recipe memory areas.
/// This protocol is responsible for raw data transfer.
/// </summary>
public interface IPlcProtocol
{
    /// <summary>
    /// Writes recipe data arrays and the total row count to their respective memory areas in the PLC.
    /// </summary>
    Result WriteAllAreas(int[] intData, int[] floatData, int rowCount);

    /// <summary>
    /// Reads the total number of recipe rows from the PLC's control area.
    /// </summary>
    Result<int> ReadRowCount();
    
    /// <summary>
    /// Reads a specified number of registers from the integer data area.
    /// </summary>
    /// <param name="registerCount">The total number of integer registers to read.</param>
    Result<int[]> ReadIntArea(int registerCount);

    /// <summary>
    /// Reads a specified number of registers from the float data area.
    /// </summary>
    /// <param name="registerCount">The total number of float registers to read (note: 1 float = 2 registers).</param>
    Result<int[]> ReadFloatArea(int registerCount);
}