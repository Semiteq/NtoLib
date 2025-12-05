using System.Collections.Generic;

using NtoLib.MbeTable.ResultsExtension;

namespace NtoLib.MbeTable.ServiceRecipeAssembly.Reasons.Errors;

public class AssemblyMissingOrInvalidTarget : BilingualError
{
	public AssemblyMissingOrInvalidTarget(List<string> errors)
		: base(
			$"Missing or invalid targets in current environment: " + string.Join("; ", errors),
			"Отсутствующие или недействительные цели в текущей среде: " + string.Join("; ", errors))
	{
	}
}
