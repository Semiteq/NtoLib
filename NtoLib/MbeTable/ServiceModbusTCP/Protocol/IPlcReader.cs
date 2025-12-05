using System.Threading;
using System.Threading.Tasks;

using FluentResults;

namespace NtoLib.MbeTable.ServiceModbusTCP.Protocol;

public interface IPlcReader
{
	Task<Result<int>> ReadRowCountAsync(CancellationToken ct);
	Task<Result<int[]>> ReadIntAreaAsync(int registers, CancellationToken ct);
	Task<Result<int[]>> ReadFloatAreaAsync(int registers, CancellationToken ct);
}
