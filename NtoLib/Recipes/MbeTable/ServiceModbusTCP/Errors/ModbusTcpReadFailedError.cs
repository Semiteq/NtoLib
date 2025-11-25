using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;

public sealed class ModbusTcpReadFailedError : BilingualError
{
	public int Address { get; }
	public int Length { get; }
	public string? Reason { get; }

	public ModbusTcpReadFailedError(int address, int length, string? reason = null)
		: base(
			$"Failed to read {length} registers from address {address}" + (reason != null ? $": {reason}" : ""),
			$"Не удалось прочитать {length} регистров с адреса {address}" + (reason != null ? $": {reason}" : ""))
	{
		Address = address;
		Length = length;
		Reason = reason;
	}
}
