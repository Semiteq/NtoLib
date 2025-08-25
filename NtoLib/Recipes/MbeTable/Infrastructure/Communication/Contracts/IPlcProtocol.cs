#nullable enable
using FluentResults;

namespace NtoLib.Recipes.MbeTable.Infrastructure.Communication.Contracts;

public interface IPlcProtocol
{
    Result WriteAllAreas(int[] intData, int[] floatData, int[] boolData, int rowCount);
    Result<(int[] IntData, int[] FloatData, int RowCount)> ReadAllAreas();
}