using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceModbusTCP.Errors;

public sealed class ModbusTcpFailedError : BilingualError
{
	public int Address { get; }
	public int Length { get; }
	public string? Reason { get; }

	public ModbusTcpFailedError(int address, int length, string? reason = null)
		: base(
			$"Failed to write {length} registers to address {address}" + (reason != null ? $": {reason}" : ""),
			$"Не удалось записать {length} регистров по адресу {address}" + (reason != null ? $": {reason}" : ""))
	{
		Address = address;
		Length = length;
		Reason = reason;
	}
}
