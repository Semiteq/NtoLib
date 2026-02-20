using NtoLib.Recipes.MbeTable.ResultsExtension;

namespace NtoLib.Recipes.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public sealed class AssemblyUnknownPlcAreaError : BilingualError
{
	public AssemblyUnknownPlcAreaError(string area)
		: base(
			$"Unknown PLC area: {area}",
			$"Неизвестная область PLC: {area}")
	{
		Area = area;
		Metadata["area"] = area;
	}

	public string Area { get; }
}
