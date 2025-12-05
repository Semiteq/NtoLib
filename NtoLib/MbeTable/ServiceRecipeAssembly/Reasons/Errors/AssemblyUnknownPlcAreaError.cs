using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyUnknownPlcAreaError : BilingualError
{
	public string Area { get; }

	public AssemblyUnknownPlcAreaError(string area)
		: base(
			$"Unknown PLC area: {area}",
			$"Неизвестная область PLC: {area}")
	{
		Area = area;
		Metadata["area"] = area;
	}
}
