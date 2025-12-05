using System.Threading;
using System.Threading.Tasks;

using FluentResults;

using NtoLib.MbeTable.ServiceModbusTCP.Transport;

namespace NtoLib.MbeTable.ServiceModbusTCP.Protocol;

/// <summary>
/// Handles chunked read/write operations for Modbus communication.
/// </summary>
public interface IModbusChunkHandler
{
	/// <summary>
	/// Writes data in chunks to specified address.
	/// </summary>
	Task<Result> WriteChunkedAsync(
		IModbusTransport transport,
		int baseAddress,
		int[] data,
		int chunkSize,
		CancellationToken ct);

	/// <summary>
	/// Reads data in chunks from specified address.
	/// </summary>
	Task<Result<int[]>> ReadChunkedAsync(
		IModbusTransport transport,
		int baseAddress,
		int totalRegisters,
		int chunkSize,
		CancellationToken ct);
}
