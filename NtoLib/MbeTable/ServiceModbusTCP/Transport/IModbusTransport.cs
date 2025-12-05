using System;
using System.Threading;
using System.Threading.Tasks;

using FluentResults;

namespace NtoLib.MbeTable.ServiceModbusTCP.Transport;

public interface IModbusTransport : IDisposable
{
	Task<Result> EnsureConnectedAsync(CancellationToken ct);

	Task<Result<int[]>> ReadHoldingAsync(int startAddress, int length, CancellationToken ct);

	Task<Result> WriteHoldingAsync(int startAddress, int[] data, CancellationToken ct);

	void Disconnect();
}
