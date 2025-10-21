using System.Threading;
using System.Threading.Tasks;

using FluentResults;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Protocol;

public interface IPlcProtocol
{
    Task<Result> WriteAllAreasAsync(int[] intData, int[] floatData, int rowCount, CancellationToken ct);

    Task<Result<int>> ReadRowCountAsync(CancellationToken ct);

    Task<Result<int[]>> ReadIntAreaAsync(int registers, CancellationToken ct);

    Task<Result<int[]>> ReadFloatAreaAsync(int registers, CancellationToken ct);
}