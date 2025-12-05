using System.Threading;
using System.Threading.Tasks;

using FluentResults;

namespace NtoLib.MbeTable.ServiceModbusTCP.Protocol;

public interface IPlcWriter
{
	Task<Result> WriteAllAreasAsync(int[] intData, int[] floatData, int rowCount, CancellationToken ct);
}
